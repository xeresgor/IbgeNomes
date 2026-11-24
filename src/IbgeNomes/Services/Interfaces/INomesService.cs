using IbgeNomes.Models;

namespace IbgeNomes.Services.Interfaces;

/// <summary>Integração com a API de Nomes do IBGE (v3, Censo 2022).</summary>
public interface INomesService
{
    /// <summary>
    /// Nomes femininos, masculinos e sobrenomes mais frequentes de um estado (código de 2 dígitos)
    /// ou município (código de 7 dígitos). Devolve <c>null</c> quando o IBGE não tem dados para o código.
    /// </summary>
    Task<NomesLocalidade?> ObterNomesAsync(long localidadeId, CancellationToken cancellationToken = default);
}
