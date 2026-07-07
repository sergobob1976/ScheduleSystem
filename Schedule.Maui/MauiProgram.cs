using Microsoft.Extensions.Logging;
using Schedule.Maui.Services;
using Schedule.Maui.ViewModels;
using Schedule.Maui.Views;

namespace Schedule.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // ==========================================================
        // РЕЄСТРАЦІЯ СЕРВІСІВ ТА VIEWMODELS (DEPENDENCY INJECTION)
        // ==========================================================

        // 1. Мережевий клієнт та сервіс локальної бази даних SQLite
        // Замість звичайного AddSingleton<HttpClient> додаємо клієнт з ігноруванням сертифікатів для localhost:
        builder.Services.AddSingleton(sp =>
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            return new HttpClient(handler);
        });
        builder.Services.AddSingleton<DatabaseService>();

        // 2. Сервіс офлайн-синхронізації даних з MySQL сервера
        builder.Services.AddSingleton<SyncService>();

        // 3. Реєстрація архітектурних шарів сторінки студента
        // Використовуємо AddTransient, щоб при кожному переході на сторінку об'єкти оновлювалися
        builder.Services.AddTransient<StudentViewModel>();
        builder.Services.AddTransient<StudentPage>();

        // ==========================================================

        return builder.Build();
    }
}