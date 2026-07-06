using Schedule.Infrastructure;
using System.Text;
using Schedule.Core.Interfaces;
using Schedule.Infrastructure.Repositories;

// Примусово вмикаємо підтримку українських літер (UTF-8) для консолі сервера
Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. Реєстрація сервісів у контейнері залежностей (Dependency Injection)
// =========================================================================

builder.Services.AddControllers();

// Реєструємо ініціалізатор бази даних як Transient сервіс
builder.Services.AddTransient<DatabaseInitializer>();

// (Сюди згодом ми будемо додавати реєстрацію твоїх репозиторіїв та сервісів,
// наприклад: builder.Services.AddScoped<IGroupRepository, GroupRepository>();)

// Реєструємо наші репозиторії
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<ITeacherRepository, TeacherRepository>();
builder.Services.AddScoped<IClassRoomRepository, ClassRoomRepository>();
builder.Services.AddScoped<IDisciplineRepository, DisciplineRepository>();
builder.Services.AddScoped<ISemesterRepository, SemesterRepository>();
builder.Services.AddScoped<IRealLessonRepository, RealLessonRepository>();

var app = builder.Build();

// =========================================================================
// 2. Автоматична ініціалізація бази даних MySQL при старті сервера
// =========================================================================

using (var scope = app.Services.CreateScope())
{
    try
    {
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        initializer.Initialize();

        // Виводимо повідомлення в консоль, щоб переконатися, що все ок
        app.Logger.LogInformation("База даних успішно ініціалізована, таблиці створено.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Помилка під час ініціалізації бази даних MySQL!");
        // Якщо база не підключилася (наприклад, сервер MySQL вимкнено), 
        // додаток зупиниться і покаже помилку в консолі
        throw;
    }
}

// =========================================================================
// 3. Налаштування HTTP-конвеєра (Middleware)
// =========================================================================

// Забезпечуємо перенаправлення на HTTPS
app.UseHttpsRedirection();

// Вмикаємо авторизацію (знадобиться для Диспетчера в Schedule.Web)
app.UseAuthorization();

// Мапимо контролери (щоб сервер знав, куди направляти маршрути api/...)
app.MapControllers();

// Запуск сервера
app.Run();