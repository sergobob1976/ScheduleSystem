using Microsoft.Extensions.Logging;
using Schedule.Maui.Services;
using Schedule.Maui.ViewModels;
using Schedule.Maui.Views;

namespace Schedule.Maui
{
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
            // Реєструємо MainPage та MainViewModel як сінглтони в контейнері залежностей
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<StudentPage>();
            builder.Services.AddSingleton<StudentViewModel>();
            return builder.Build();
        }
    }
}
