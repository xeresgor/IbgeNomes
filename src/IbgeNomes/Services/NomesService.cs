using System.Collections.Concurrent;
using IbgeNomes.Models;
using IbgeNomes.Services.Interfaces;

namespace IbgeNomes.Services;

/// <summary>
/// Cliente da API de Nomes do IBGE (Censo 2022). Cada localidade consultada fica em cache de memória:
/// voltar a um estado já visto não gera nova requisição.
/// </summary>
public class NomesService : INomesService
{
    private readonly HttpClient _http;
    private readonly ConcurrentDictionary<long, NomesLocalidade> _cache = new();

    public NomesService(HttpClient http)
    {
        _http = http;
    }

    public async Task<NomesLocalidade?> ObterNomesAsync(long localidadeId, CancellationToken cancellationToken = default)
    {
        if (localidadeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(localidadeId), "O código da localidade deve ser positivo.");

        if (_cache.TryGetValue(localidadeId, out var emCache))
            return emCache;

        // Para um código sem dados o IBGE responde HTTP 200 com o corpo "null".
        var nomes = await IbgeHttp.ObterJsonAsync<NomesLocalidade>(
            _http, $"v3/nomes/2022/localidade/{localidadeId}", cancellationToken);

        if (nomes is null)
            return null;

        // Garante a ordem do mais frequente para o menos frequente, independente do servidor.
        nomes.Mulheres = Ordenar(nomes.Mulheres);
        nomes.Homens = Ordenar(nomes.Homens);
        nomes.Sobrenomes = Ordenar(nomes.Sobrenomes);

        _cache[localidadeId] = nomes;
        return nomes;
    }

    private static List<NomeFrequencia> Ordenar(List<NomeFrequencia>? lista) =>
        (lista ?? new List<NomeFrequencia>()).OrderByDescending(n => n.Frequencia).ToList();
}
