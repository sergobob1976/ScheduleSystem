using System.Text;
using Schedule.Core.Interfaces;
using Schedule.Infrastructure;
using Schedule.Infrastructure.Repositories;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddControllers();

builder.Services.AddTransient<DatabaseInitializer>();

builder.Services.AddScoped<
    IGroupRepository,
    GroupRepository>();

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
    IBaseLessonRepository,
    BaseLessonRepository>();

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
            "База даних успішно ініціалізована, таблиці створено.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(
            ex,
            "Помилка під час ініціалізації бази даних MySQL!");

        throw;
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();