using Microsoft.Extensions.Logging;
using MauiApp1.Data;

namespace MauiApp1;

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
            });

        // Blazor WebView
        builder.Services.AddMauiBlazorWebView();

        // Register Journal Database (SQLite)
        builder.Services.AddSingleton<JournalDb>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
        builder.Services.AddSingleton<MauiApp1.Services.PdfExportService>();

    }
}
