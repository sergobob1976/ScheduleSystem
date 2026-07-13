using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Schedule.Core.Interfaces;
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
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
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
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddTransient<DatabaseInitializer>();
builder.Services.AddSingleton<DayScheduleDocxGenerator>();
builder.Services.AddSingleton<TeacherReadingDocxGenerator>();
builder.Services.AddSingleton<PasswordHashService>();

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

        initializer.Initialize();

        app.Logger.LogInformation(
            "Базу даних успішно ініціалізовано, " +
            "таблиці створено.");
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

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("ScheduleWeb");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
