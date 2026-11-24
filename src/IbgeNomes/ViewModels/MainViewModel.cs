using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IbgeNomes.Models;
using IbgeNomes.Services;
using IbgeNomes.Services.Interfaces;

namespace IbgeNomes.ViewModels;

public enum CategoriaNome
{
    Mulheres,
    Homens,
    Sobrenomes
}

/// <summary>
/// Estado da tela principal. Orquestra a consulta encadeada:
/// 1) ao abrir, busca os estados (API de Localidades);
/// 2) ao escolher um estado, busca os municípios dele E os nomes do estado (API de Nomes, id da UF);
/// 3) ao escolher um município, busca os nomes do município (API de Nomes, id de 7 dígitos).
/// Não conhece nenhum controle visual: só expõe propriedades e comandos para o binding da View.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ILocalidadesService _localidades;
    private readonly INomesService _nomes;

    /// <summary>Primeira opção do seletor de municípios: consultar o estado inteiro.</summary>
    public Municipio TodoOEstado { get; } = new() { Id = 0, Nome = "Todo o estado" };

    private CancellationTokenSource? _ctsNomes;
    private CancellationTokenSource? _ctsMunicipios;
    private bool _trocandoListaDeMunicipios;
    private NomesLocalidade? _resultado;
    private (long Id, string Titulo)? _ultimaConsulta;

    public MainViewModel(ILocalidadesService localidades, INomesService nomes)
    {
        _localidades = localidades;
        _nomes = nomes;
        Municipios = new List<Municipio> { TodoOEstado };
    }

    public ObservableCollection<Estado> Estados { get; } = new();

    public ObservableCollection<RankingItem> Ranking { get; } = new();

    // ---------- Seleção ----------

    [ObservableProperty]
    public partial Estado? EstadoSelecionado { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Municipio> Municipios { get; set; }

    [ObservableProperty]
    public partial Municipio? MunicipioSelecionado { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MulheresSelecionado), nameof(HomensSelecionado),
        nameof(SobrenomesSelecionado), nameof(TituloRanking))]
    public partial CategoriaNome Categoria { get; set; } = CategoriaNome.Mulheres;

    public bool MulheresSelecionado => Categoria == CategoriaNome.Mulheres;
    public bool HomensSelecionado => Categoria == CategoriaNome.Homens;
    public bool SobrenomesSelecionado => Categoria == CategoriaNome.Sobrenomes;

    public string TituloRanking => Categoria switch
    {
        CategoriaNome.Mulheres => "Nomes femininos mais comuns",
        CategoriaNome.Homens => "Nomes masculinos mais comuns",
        _ => "Sobrenomes mais comuns"
    };

    // ---------- Estado da tela ----------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Ocupado))]
    public partial bool CarregandoEstados { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MunicipiosHabilitado))]
    public partial bool CarregandoMunicipios { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Ocupado))]
    public partial bool CarregandoNomes { get; set; }

    public bool Ocupado => CarregandoEstados || CarregandoNomes;

    public bool MunicipiosHabilitado => EstadoSelecionado is not null && !CarregandoMunicipios;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemErro))]
    public partial string? MensagemErro { get; set; }

    public bool TemErro => !string.IsNullOrEmpty(MensagemErro);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemAviso))]
    public partial string? Aviso { get; set; }

    public bool TemAviso => !string.IsNullOrEmpty(Aviso);

    [ObservableProperty]
    public partial bool TemResultado { get; set; }

    [ObservableProperty]
    public partial string TituloLocalidade { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MaisComumMulher { get; set; } = "—";

    [ObservableProperty]
    public partial string MaisComumHomem { get; set; } = "—";

    [ObservableProperty]
    public partial string MaisComumSobrenome { get; set; } = "—";

    // ---------- Comandos ----------

    /// <summary>Etapa 1: popula o seletor de estados. Chamado quando a página aparece.</summary>
    [RelayCommand]
    public async Task CarregarEstadosAsync()
    {
        if (Estados.Count > 0 || CarregandoEstados)
            return;

        CarregandoEstados = true;
        MensagemErro = null;
        try
        {
            var estados = await _localidades.ObterEstadosAsync();
            foreach (var estado in estados)
                Estados.Add(estado);

            if (Estados.Count == 0)
                MensagemErro = "O IBGE não devolveu nenhum estado.";
        }
        catch (IbgeApiException ex)
        {
            MensagemErro = ex.Message;
        }
        finally
        {
            CarregandoEstados = false;
        }
    }

    [RelayCommand]
    private void SelecionarCategoria(string categoria)
    {
        if (Enum.TryParse<CategoriaNome>(categoria, out var valor))
            Categoria = valor;
    }

    /// <summary>Botão "Tentar novamente": repete o que falhou por último.</summary>
    [RelayCommand]
    private async Task TentarNovamenteAsync()
    {
        if (Estados.Count == 0)
        {
            await CarregarEstadosAsync();
            return;
        }

        if (EstadoSelecionado is not null && Municipios.Count <= 1)
            await CarregarMunicipiosAsync(EstadoSelecionado);

        if (_ultimaConsulta is { } ultima)
            await ConsultarNomesAsync(ultima.Id, ultima.Titulo);
    }

    // ---------- Encadeamento das chamadas ----------

    partial void OnEstadoSelecionadoChanged(Estado? value)
    {
        OnPropertyChanged(nameof(MunicipiosHabilitado));
        if (value is null)
            return;

        // O seletor de município volta para "Todo o estado" enquanto a lista nova chega.
        TrocarListaDeMunicipios(new List<Municipio> { TodoOEstado });

        _ = CarregarMunicipiosAsync(value);
        _ = ConsultarNomesAsync(value.Id, value.NomeExibicao);
    }

    partial void OnMunicipioSelecionadoChanged(Municipio? value)
    {
        if (_trocandoListaDeMunicipios || value is null || EstadoSelecionado is null)
            return;

        if (ReferenceEquals(value, TodoOEstado))
            _ = ConsultarNomesAsync(EstadoSelecionado.Id, EstadoSelecionado.NomeExibicao);
        else
            _ = ConsultarNomesAsync(value.Id, $"{value.Nome} – {EstadoSelecionado.Sigla}");
    }

    partial void OnCategoriaChanged(CategoriaNome value) => AtualizarRanking();

    private async Task CarregarMunicipiosAsync(Estado estado)
    {
        _ctsMunicipios?.Cancel();
        var cts = _ctsMunicipios = new CancellationTokenSource();

        CarregandoMunicipios = true;
        try
        {
            var municipios = await _localidades.ObterMunicipiosAsync(estado.Id, cts.Token);
            if (cts.IsCancellationRequested || !ReferenceEquals(EstadoSelecionado, estado))
                return;

            var lista = new List<Municipio>(municipios.Count + 1) { TodoOEstado };
            lista.AddRange(municipios);
            TrocarListaDeMunicipios(lista);
        }
        catch (OperationCanceledException)
        {
            // o usuário trocou de estado antes da resposta chegar
        }
        catch (IbgeApiException ex)
        {
            if (!cts.IsCancellationRequested)
                MensagemErro = ex.Message;
        }
        finally
        {
            if (ReferenceEquals(_ctsMunicipios, cts))
                CarregandoMunicipios = false;
        }
    }

    /// <summary>Etapas 2 e 3: chama a API de Nomes com o id do estado ou do município.</summary>
    private async Task ConsultarNomesAsync(long localidadeId, string titulo)
    {
        _ctsNomes?.Cancel();
        var cts = _ctsNomes = new CancellationTokenSource();
        _ultimaConsulta = (localidadeId, titulo);

        CarregandoNomes = true;
        MensagemErro = null;
        Aviso = null;
        try
        {
            var resultado = await _nomes.ObterNomesAsync(localidadeId, cts.Token);
            if (cts.IsCancellationRequested)
                return;

            AplicarResultado(titulo, resultado);
        }
        catch (OperationCanceledException)
        {
            // uma consulta mais nova já está em andamento
        }
        catch (IbgeApiException ex)
        {
            if (!cts.IsCancellationRequested)
                MensagemErro = ex.Message;
        }
        finally
        {
            if (ReferenceEquals(_ctsNomes, cts))
                CarregandoNomes = false;
        }
    }

    private void AplicarResultado(string titulo, NomesLocalidade? resultado)
    {
        _resultado = resultado;
        TituloLocalidade = titulo;

        if (resultado is null || resultado.EstaVazio)
        {
            TemResultado = false;
            Aviso = "O IBGE não tem dados de nomes para esta localidade.";
            MaisComumMulher = MaisComumHomem = MaisComumSobrenome = "—";
            Ranking.Clear();
            return;
        }

        TemResultado = true;
        MaisComumMulher = PrimeiroOuTraco(resultado.Mulheres);
        MaisComumHomem = PrimeiroOuTraco(resultado.Homens);
        MaisComumSobrenome = PrimeiroOuTraco(resultado.Sobrenomes);
        AtualizarRanking();
    }

    private void AtualizarRanking()
    {
        Ranking.Clear();
        if (_resultado is null)
            return;

        var lista = Categoria switch
        {
            CategoriaNome.Mulheres => _resultado.Mulheres,
            CategoriaNome.Homens => _resultado.Homens,
            _ => _resultado.Sobrenomes
        };

        var maior = lista.Count > 0 ? lista[0].Frequencia : 0;
        for (var i = 0; i < lista.Count; i++)
            Ranking.Add(new RankingItem(i + 1, lista[i].Nome, lista[i].Frequencia, maior));
    }

    private void TrocarListaDeMunicipios(IReadOnlyList<Municipio> lista)
    {
        // Trocar o ItemsSource do Picker zera a seleção; a trava evita disparar uma consulta nesse meio-tempo.
        _trocandoListaDeMunicipios = true;
        try
        {
            Municipios = lista;
            MunicipioSelecionado = TodoOEstado;
        }
        finally
        {
            _trocandoListaDeMunicipios = false;
        }
    }

    private static string PrimeiroOuTraco(List<NomeFrequencia> lista) =>
        lista.Count > 0 ? RankingItem.FormatarNome(lista[0].Nome) : "—";
}
