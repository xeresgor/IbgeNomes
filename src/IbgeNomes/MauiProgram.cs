using IbgeNomes.Services;
using IbgeNomes.Services.Interfaces;
using IbgeNomes.ViewModels;
using IbgeNomes.Views;
using Microsoft.Extensions.Logging;

namespace IbgeNomes;

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

        // Camada de Serviços: um único HttpClient para o app inteiro (reaproveita conexões).
        builder.Services.AddSingleton(_ => new HttpClient
        {
            BaseAddress = new Uri(IbgeHttp.UrlBase),
            Timeout = TimeSpan.FromSeconds(20)
        });
        builder.Services.AddSingleton<ILocalidadesService, LocalidadesService>();
        builder.Services.AddSingleton<INomesService, NomesService>();

        // Camada de Apresentação.
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
