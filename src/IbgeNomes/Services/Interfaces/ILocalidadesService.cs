using IbgeNomes.Models;

namespace IbgeNomes.Services.Interfaces;

/// <summary>Integração com a API de Localidades do IBGE (v1).</summary>
public interface ILocalidadesService
{
    /// <summary>Lista as 27 UFs em ordem alfabética.</summary>
    Task<IReadOnlyList<Estado>> ObterEstadosAsync(CancellationToken cancellationToken = default);

    /// <summary>Lista os municípios de uma UF em ordem alfabética.</summary>
    Task<IReadOnlyList<Municipio>> ObterMunicipiosAsync(int estadoId, CancellationToken cancellationToken = default);
}
