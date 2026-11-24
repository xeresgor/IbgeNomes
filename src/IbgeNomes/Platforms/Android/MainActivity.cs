using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace IbgeNomes;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // O fundo do app é claro: relógio e ícones da barra de status em cor escura para ficarem legíveis.
        if (Window is not null)
            WindowCompat.GetInsetsController(Window, Window.DecorView).AppearanceLightStatusBars = true;
    }
}
