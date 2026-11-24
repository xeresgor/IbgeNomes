using System.Collections.Concurrent;
using IbgeNomes.Models;
using IbgeNomes.Services.Interfaces;

namespace IbgeNomes.Services;

/// <summary>
/// Cliente da API de Localidades do IBGE. Estados e municípios quase nunca mudam, então ficam em
/// cache de memória durante a execução do app: cada UF é baixada no máximo uma vez.
/// </summary>
public class LocalidadesService : ILocalidadesService
{
    private readonly HttpClient _http;
    private IReadOnlyList<Estado>? _estados;
    private readonly ConcurrentDictionary<int, IReadOnlyList<Municipio>> _municipiosPorEstado = new();

    public LocalidadesService(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<Estado>> ObterEstadosAsync(CancellationToken cancellationToken = default)
    {
        if (_estados is not null)
            return _estados;

        var estados = await IbgeHttp.ObterJsonAsync<List<Estado>>(
            _http, "v1/localidades/estados?orderBy=nome", cancellationToken) ?? new List<Estado>();

        // Ordena aqui também: não dependemos do orderBy do servidor para a UI ficar organizada.
        _estados = estados
            .OrderBy(e => e.Nome, StringComparer.Create(new System.Globalization.CultureInfo("pt-BR"), true))
            .ToList();
        return _estados;
    }

    public async Task<IReadOnlyList<Municipio>> ObterMunicipiosAsync(int estadoId, CancellationToken cancellationToken = default)
    {
        if (_municipiosPorEstado.TryGetValue(estadoId, out var emCache))
            return emCache;

        var municipios = await IbgeHttp.ObterJsonAsync<List<Municipio>>(
            _http, $"v1/localidades/estados/{estadoId}/municipios?orderBy=nome", cancellationToken) ?? new List<Municipio>();

        IReadOnlyList<Municipio> ordenados = municipios
            .OrderBy(m => m.Nome, StringComparer.Create(new System.Globalization.CultureInfo("pt-BR"), true))
            .ToList();
        _municipiosPorEstado[estadoId] = ordenados;
        return ordenados;
    }
}
