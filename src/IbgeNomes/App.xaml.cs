namespace IbgeNomes;

public partial class App : Application
{
    private readonly AppShell _shell;

    public App(AppShell shell)
    {
        InitializeComponent();
        // Paleta pensada para fundo claro: fixa o tema para manter o contraste no modo escuro do aparelho.
        UserAppTheme = AppTheme.Light;
        _shell = shell;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_shell) { Title = "Nomes do Brasil" };
    }
}
