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

        // ==========================================
        // РЕЄСТРАЦІЯ СЕРВІСІВ (Dependency Injection)
        // ==========================================

        // Базовий HttpClient для системних потреб додатку
        builder.Services.AddSingleton<HttpClient>();

        // Локальна база SQLite та сервіс кросплатформної синхронізації
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<SyncService>();

        // Забезпечуємо створення сторінок відображення розкладу через фабрику DI
        builder.Services.AddTransient<StudentViewModel>();
        builder.Services.AddTransient<StudentPage>();

        return builder.Build();
    }
}