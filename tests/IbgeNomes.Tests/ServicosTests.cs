using System.Net;
using IbgeNomes.Services;

namespace IbgeNomes.Tests;

public class ServicosTests
{
    // Trechos reais das respostas do IBGE (conferidas em 24/11/2026).
    private const string JsonEstados =
        "[{\"id\":33,\"sigla\":\"RJ\",\"nome\":\"Rio de Janeiro\",\"regiao\":{\"id\":3,\"sigla\":\"SE\",\"nome\":\"Sudeste\"}}," +
        "{\"id\":12,\"sigla\":\"AC\",\"nome\":\"Acre\",\"regiao\":{\"id\":1,\"sigla\":\"N\",\"nome\":\"Norte\"}}]";

    private const string JsonNomesRj =
        "{\"localidade\":33,\"mulheres\":[{\"nome\":\"ana\",\"frequencia\":345138},{\"nome\":\"maria\",\"frequencia\":741049}]," +
        "\"homens\":[{\"nome\":\"jose\",\"frequencia\":250172}],\"sobrenomes\":[{\"nome\":\"silva\",\"frequencia\":2515895}]}";

    [Fact]
    public async Task Estados_sao_desserializados_e_ordenados_por_nome()
    {
        var http = HttpFalso.ComJson(JsonEstados);
        var servico = new LocalidadesService(http.CriarCliente());

        var estados = await servico.ObterEstadosAsync();

        Assert.Equal(new[] { "Acre", "Rio de Janeiro" }, estados.Select(e => e.Nome));
        Assert.Equal("Rio de Janeiro (RJ)", estados[1].NomeExibicao);
        Assert.Equal("Sudeste", estados[1].Regiao!.Nome);
        Assert.Contains("v1/localidades/estados", http.UrlsPedidas.Single());
    }

    [Fact]
    public async Task Municipios_usam_o_id_do_estado_e_ficam_em_cache()
    {
        var http = HttpFalso.ComJson("[{\"id\":3304557,\"nome\":\"Rio de Janeiro\"},{\"id\":3303302,\"nome\":\"Niterói\"}]");
        var servico = new LocalidadesService(http.CriarCliente());

        var primeira = await servico.ObterMunicipiosAsync(33);
        var segunda = await servico.ObterMunicipiosAsync(33);

        Assert.Equal("Niterói", primeira[0].Nome);
        Assert.Equal(3304557, primeira[1].Id);
        Assert.Same(primeira, segunda);
        Assert.Contains("v1/localidades/estados/33/municipios", http.UrlsPedidas.Single());
    }

    [Fact]
    public async Task Nomes_chamam_a_api_v3_com_o_id_da_localidade_e_ordenam_por_frequencia()
    {
        var http = HttpFalso.ComJson(JsonNomesRj);
        var servico = new NomesService(http.CriarCliente());

        var nomes = await servico.ObterNomesAsync(33);

        Assert.NotNull(nomes);
        Assert.Equal("maria", nomes!.Mulheres[0].Nome); // a resposta veio fora de ordem de propósito
        Assert.Equal("jose", nomes.Homens[0].Nome);
        Assert.Equal(2515895, nomes.Sobrenomes[0].Frequencia);
        Assert.EndsWith("v3/nomes/2022/localidade/33", http.UrlsPedidas.Single());
    }

    [Fact]
    public async Task Nomes_ficam_em_cache_por_localidade()
    {
        var http = HttpFalso.ComJson(JsonNomesRj);
        var servico = new NomesService(http.CriarCliente());

        await servico.ObterNomesAsync(33);
        await servico.ObterNomesAsync(33);

        Assert.Single(http.UrlsPedidas);
    }

    [Fact]
    public async Task Localidade_sem_dados_devolve_null()
    {
        // O IBGE responde HTTP 200 com o corpo "null" para códigos sem dados.
        var servico = new NomesService(HttpFalso.ComJson("null").CriarCliente());

        Assert.Null(await servico.ObterNomesAsync(9999999));
    }

    [Fact]
    public async Task Erro_do_servidor_vira_IbgeApiException_com_mensagem_amigavel()
    {
        var servico = new NomesService(HttpFalso.ComJson("{}", HttpStatusCode.InternalServerError).CriarCliente());

        var erro = await Assert.ThrowsAsync<IbgeApiException>(() => servico.ObterNomesAsync(33));
        Assert.Contains("500", erro.Message);
    }

    [Fact]
    public async Task Falha_de_rede_vira_IbgeApiException()
    {
        var http = new HttpFalso(_ => throw new HttpRequestException("sem rede"));
        var servico = new LocalidadesService(http.CriarCliente());

        var erro = await Assert.ThrowsAsync<IbgeApiException>(() => servico.ObterEstadosAsync());
        Assert.Contains("internet", erro.Message);
    }

    [Fact]
    public async Task Json_invalido_vira_IbgeApiException()
    {
        var servico = new NomesService(HttpFalso.ComJson("<html>manutenção</html>").CriarCliente());

        await Assert.ThrowsAsync<IbgeApiException>(() => servico.ObterNomesAsync(33));
    }
}
