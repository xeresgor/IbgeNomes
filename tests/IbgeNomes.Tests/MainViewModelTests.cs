using IbgeNomes.Models;
using IbgeNomes.Services;
using IbgeNomes.Services.Interfaces;
using IbgeNomes.ViewModels;

namespace IbgeNomes.Tests;

public class MainViewModelTests
{
    private static readonly Estado Rj = new() { Id = 33, Sigla = "RJ", Nome = "Rio de Janeiro" };
    private static readonly Estado Ac = new() { Id = 12, Sigla = "AC", Nome = "Acre" };
    private static readonly Municipio Niteroi = new() { Id = 3303302, Nome = "Niterói" };

    private class LocalidadesFalsas : ILocalidadesService
    {
        public bool Falhar { get; set; }

        public Task<IReadOnlyList<Estado>> ObterEstadosAsync(CancellationToken cancellationToken = default) =>
            Falhar
                ? Task.FromException<IReadOnlyList<Estado>>(new IbgeApiException("Sem conexão com o IBGE."))
                : Task.FromResult<IReadOnlyList<Estado>>(new[] { Ac, Rj });

        public Task<IReadOnlyList<Municipio>> ObterMunicipiosAsync(int estadoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Municipio>>(estadoId == 33 ? new[] { Niteroi } : Array.Empty<Municipio>());
    }

    private class NomesFalsos : INomesService
    {
        public List<long> Consultados { get; } = new();
        public bool Falhar { get; set; }

        public Task<NomesLocalidade?> ObterNomesAsync(long localidadeId, CancellationToken cancellationToken = default)
        {
            Consultados.Add(localidadeId);
            if (Falhar)
                return Task.FromException<NomesLocalidade?>(new IbgeApiException("Sem conexão com o IBGE."));
            if (localidadeId == 12)
                return Task.FromResult<NomesLocalidade?>(null);

            return Task.FromResult<NomesLocalidade?>(new NomesLocalidade
            {
                Localidade = localidadeId,
                Mulheres = { new() { Nome = "maria", Frequencia = 200 }, new() { Nome = "ana", Frequencia = 100 } },
                Homens = { new() { Nome = "joao", Frequencia = 150 } },
                Sobrenomes = { new() { Nome = "silva", Frequencia = 900 } }
            });
        }
    }

    [Fact]
    public async Task Ao_abrir_popula_os_estados()
    {
        var vm = new MainViewModel(new LocalidadesFalsas(), new NomesFalsos());

        await vm.CarregarEstadosAsync();

        Assert.Equal(2, vm.Estados.Count);
        Assert.False(vm.TemErro);
    }

    [Fact]
    public async Task Escolher_estado_consulta_nomes_com_o_id_do_estado_e_carrega_municipios()
    {
        var nomes = new NomesFalsos();
        var vm = new MainViewModel(new LocalidadesFalsas(), nomes);
        await vm.CarregarEstadosAsync();

        vm.EstadoSelecionado = Rj;

        Assert.Equal(new long[] { 33 }, nomes.Consultados);
        Assert.Equal("Rio de Janeiro (RJ)", vm.TituloLocalidade);
        Assert.Equal(new[] { "Todo o estado", "Niterói" }, vm.Municipios.Select(m => m.Nome));
        Assert.Same(vm.TodoOEstado, vm.MunicipioSelecionado);
        Assert.Equal("Maria", vm.MaisComumMulher);
        Assert.Equal("Joao", vm.MaisComumHomem);
        Assert.Equal("Silva", vm.MaisComumSobrenome);
        Assert.Equal(new[] { "Maria", "Ana" }, vm.Ranking.Select(r => r.Nome));
        Assert.Equal(0.5, vm.Ranking[1].Proporcao);
    }

    [Fact]
    public async Task Escolher_municipio_consulta_nomes_com_o_id_de_7_digitos()
    {
        var nomes = new NomesFalsos();
        var vm = new MainViewModel(new LocalidadesFalsas(), nomes);
        await vm.CarregarEstadosAsync();
        vm.EstadoSelecionado = Rj;

        vm.MunicipioSelecionado = vm.Municipios.Single(m => m.Id == 3303302);

        Assert.Equal(new long[] { 33, 3303302 }, nomes.Consultados);
        Assert.Equal("Niterói – RJ", vm.TituloLocalidade);
    }

    [Fact]
    public async Task Trocar_a_aba_mostra_a_lista_da_categoria()
    {
        var vm = new MainViewModel(new LocalidadesFalsas(), new NomesFalsos());
        await vm.CarregarEstadosAsync();
        vm.EstadoSelecionado = Rj;

        vm.SelecionarCategoriaCommand.Execute("Sobrenomes");

        Assert.True(vm.SobrenomesSelecionado);
        Assert.Equal("Sobrenomes mais comuns", vm.TituloRanking);
        Assert.Equal("Silva", Assert.Single(vm.Ranking).Nome);
    }

    [Fact]
    public async Task Localidade_sem_dados_mostra_aviso_e_esconde_o_resultado()
    {
        var vm = new MainViewModel(new LocalidadesFalsas(), new NomesFalsos());
        await vm.CarregarEstadosAsync();

        vm.EstadoSelecionado = Ac;

        Assert.False(vm.TemResultado);
        Assert.True(vm.TemAviso);
        Assert.Empty(vm.Ranking);
    }

    [Fact]
    public async Task Falha_ao_carregar_estados_vira_mensagem_na_tela()
    {
        var vm = new MainViewModel(new LocalidadesFalsas { Falhar = true }, new NomesFalsos());

        await vm.CarregarEstadosAsync();

        Assert.True(vm.TemErro);
        Assert.Equal("Sem conexão com o IBGE.", vm.MensagemErro);
    }

    [Fact]
    public async Task Falha_ao_trocar_de_estado_nao_mantem_os_nomes_do_estado_anterior()
    {
        var nomes = new NomesFalsos();
        var vm = new MainViewModel(new LocalidadesFalsas(), nomes);
        await vm.CarregarEstadosAsync();
        vm.EstadoSelecionado = Rj;

        nomes.Falhar = true;
        vm.EstadoSelecionado = Ac;

        Assert.True(vm.TemErro);
        Assert.False(vm.TemResultado);
        Assert.Empty(vm.Ranking);
        Assert.Equal("—", vm.MaisComumMulher);
    }

    [Theory]
    [InlineData("joao", "Joao")]
    [InlineData("MARIA", "Maria")]
    [InlineData("  silva ", "Silva")]
    public void Nome_da_api_e_formatado_com_inicial_maiuscula(string daApi, string esperado) =>
        Assert.Equal(esperado, RankingItem.FormatarNome(daApi));

    [Fact]
    public void Frequencia_usa_separador_de_milhar_brasileiro() =>
        Assert.Equal("741.049 pessoas", new RankingItem(1, "maria", 741049, 741049).FrequenciaTexto);
}
