using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.WebUtilities;
using Schedule.Api.Authentication;
using Schedule.Core.Interfaces;
using Schedule.Core.Services;
using Schedule.Infrastructure;
using Schedule.Infrastructure.Repositories;
using Schedule.Api.Services;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("ScheduleWeb", policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            return;
        }

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;
});

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    builder.Services
        .AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
        .SetApplicationName("ScheduleSystem");
}

builder.Services.AddControllers();
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var googleHostedDomain = builder.Configuration["Authentication:Google:HostedDomain"] ?? "college.cv.ua";
var webBaseUrl = builder.Configuration["ClientApplications:WebBaseUrl"];
var googleConfigurationSpecified =
    !string.IsNullOrWhiteSpace(googleClientId) ||
    !string.IsNullOrWhiteSpace(googleClientSecret);
var googleIsConfigured = !string.IsNullOrWhiteSpace(googleClientId) &&
                         !string.IsNullOrWhiteSpace(googleClientSecret);

if (googleConfigurationSpecified && !googleIsConfigured)
{
    throw new InvalidOperationException(
        "Для Google OAuth потрібно одночасно задати ClientId і ClientSecret.");
}

if (googleIsConfigured && string.IsNullOrWhiteSpace(webBaseUrl))
{
    throw new InvalidOperationException(
        "Для Google OAuth потрібно задати адресу Web-застосунку " +
        "ClientApplications__WebBaseUrl.");
}

builder.Services.AddSingleton(new GoogleAuthenticationSettings
{
    IsConfigured = googleIsConfigured,
    HostedDomain = googleHostedDomain,
    WebBaseUrl = webBaseUrl ?? string.Empty
});

var authentication = builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);

authentication.AddCookie(options =>
{
    options.Cookie.Name = "ScheduleSystem.Authentication";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
    options.Events.OnValidatePrincipal = async context =>
    {
        var role = context.Principal?.FindFirstValue(ClaimTypes.Role);
        if (role is not ("Administrator" or "Dispatcher"))
            return;

        var userName = context.Principal?.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(userName))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        var userRepository = context.HttpContext.RequestServices
            .GetRequiredService<IApplicationUserRepository>();
        var user = await userRepository.GetByUserNameAsync(userName);
        if (user is null || !user.IsActive || user.Role != role)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    };
});

if (googleIsConfigured)
{
    authentication.AddGoogle(options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.SaveTokens = false;
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            context.Response.Redirect(QueryHelpers.AddQueryString(
                context.RedirectUri,
                "hd",
                googleHostedDomain));
            return Task.CompletedTask;
        };
        options.Events.OnCreatingTicket = async context =>
        {
            var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
            var hostedDomain = context.User.TryGetProperty("hd", out var hd)
                ? hd.GetString()
                : null;
            var emailVerified =
                (context.User.TryGetProperty("verified_email", out var verifiedEmail) &&
                 verifiedEmail.ValueKind == System.Text.Json.JsonValueKind.True) ||
                (context.User.TryGetProperty("email_verified", out var emailVerifiedClaim) &&
                 emailVerifiedClaim.ValueKind == System.Text.Json.JsonValueKind.True);

            if (!emailVerified ||
                !string.Equals(hostedDomain, googleHostedDomain, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(email))
            {
                context.Fail("Доступ дозволено лише підтвердженим корпоративним обліковим записам Google.");
                return;
            }

            var teacherRepository = context.HttpContext.RequestServices
                .GetRequiredService<ITeacherRepository>();
            var teacher = await teacherRepository.GetByEmailAsync(email);
            if (teacher is null)
            {
                context.Fail("Корпоративну пошту не знайдено в довіднику викладачів.");
                return;
            }

            var identity = (ClaimsIdentity)context.Principal!.Identity!;
            foreach (var claimType in new[]
                     {
                         ClaimTypes.NameIdentifier,
                         ClaimTypes.Name,
                         ClaimTypes.GivenName,
                         ClaimTypes.Role
                     })
            {
                foreach (var claim in identity.FindAll(claimType).ToList())
                    identity.RemoveClaim(claim);
            }

            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, teacher.Id.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Name, email.ToLowerInvariant()));
            identity.AddClaim(new Claim(ClaimTypes.GivenName, TeacherNameFormatter.ToNameSurname(teacher.Name)));
            identity.AddClaim(new Claim(ClaimTypes.Role, "Teacher"));
            identity.AddClaim(new Claim("teacher_id", teacher.Id.ToString()));
        };
        options.Events.OnRemoteFailure = context =>
        {
            context.HandleResponse();
            context.Response.Redirect($"{webBaseUrl!.TrimEnd('/')}/teacher-schedule?googleLoginError=1");
            return Task.CompletedTask;
        };
    });
}
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole("Administrator", "Dispatcher")
        .Build();
});

builder.Services.AddTransient<DatabaseInitializer>();
builder.Services.AddSingleton<DayScheduleDocxGenerator>();
builder.Services.AddSingleton<AuditoriumFundDocxGenerator>();
builder.Services.AddSingleton<TeacherReadingDocxGenerator>();
builder.Services.AddSingleton<PasswordHashService>();
builder.Services.AddTransient<InitialAdminBootstrapper>();

builder.Services.AddScoped<
    IGroupRepository,
    GroupRepository>();

builder.Services.AddScoped<
    IApplicationUserRepository,
    ApplicationUserRepository>();

builder.Services.AddScoped<
    ITeacherRepository,
    TeacherRepository>();

builder.Services.AddScoped<
    IClassRoomRepository,
    ClassRoomRepository>();

builder.Services.AddScoped<
    IDisciplineRepository,
    DisciplineRepository>();

builder.Services.AddScoped<
    ISemesterRepository,
    SemesterRepository>();

builder.Services.AddScoped<
    IRealLessonRepository,
    RealLessonRepository>();

builder.Services.AddScoped<
    IRealLessonReportRepository,
    RealLessonReportRepository>();

builder.Services.AddScoped<
    IBaseLessonRepository,
    BaseLessonRepository>();

builder.Services.AddScoped<
    IBaseScheduleReportRepository,
    BaseScheduleReportRepository>();

builder.Services.AddScoped<
    ISpecialtyRepository,
    SpecialtyRepository>();

builder.Services.AddScoped<
    ITeacherSemesterLoadRepository,
    TeacherSemesterLoadRepository>();

builder.Services.AddScoped<
    ITeacherDisciplineLoadRepository,
    TeacherDisciplineLoadRepository>();

builder.Services.AddScoped<
    IGroupDisciplineRepository,
    GroupDisciplineRepository>();

builder.Services.AddScoped<
    ITeachingAssignmentRepository,
    TeachingAssignmentRepository>();

builder.Services.AddScoped<
    IGroupSpecialtyRepository,
    GroupSpecialtyRepository>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var initializer =
            scope.ServiceProvider
                .GetRequiredService<DatabaseInitializer>();

        var seedTestData = builder.Configuration
            .GetValue<bool>("Database:SeedTestData");

        initializer.Initialize(seedTestData);

        if (!seedTestData)
        {
            var adminBootstrapper = scope.ServiceProvider
                .GetRequiredService<InitialAdminBootstrapper>();
            await adminBootstrapper.EnsureCreatedAsync();
        }

        app.Logger.LogInformation(
            "Базу даних успішно ініціалізовано. Тестові дані: {SeedTestData}.",
            seedTestData ? "увімкнено" : "вимкнено");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(
            ex,
            "Помилка під час ініціалізації " +
            "бази даних MySQL.");

        throw;
    }
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
var staticFileContentTypeProvider = new FileExtensionContentTypeProvider();
staticFileContentTypeProvider.Mappings[".dat"] = "application/octet-stream";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = staticFileContentTypeProvider
});

app.UseCors("ScheduleWeb");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { Status = "Працює" }))
    .AllowAnonymous();
app.MapFallbackToFile("index.html")
    .AllowAnonymous();

app.Run();
