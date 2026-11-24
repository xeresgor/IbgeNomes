using IbgeNomes.ViewModels;

namespace IbgeNomes.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Etapa 1 do fluxo: ao abrir, popula o seletor com os estados da API de Localidades.
        await _viewModel.CarregarEstadosAsync();
    }
}
