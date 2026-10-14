namespace LocationTrackerApp;

/// <summary>Configures MAUI, native Maps, fonts and dependency injection.</summary>
public static class MauiProgram
{
    /// <summary>Builds the shared application services used by each platform entry point.</summary>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        // Register the native MAUI Maps handler before any map page is created.
        builder.UseMauiApp<App>().UseMauiMaps().ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        });

        // Persistence and tracking state live for the app lifetime; pages can be recreated.
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddTransient<MainPage>();
        return builder.Build();
    }
}
