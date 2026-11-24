using System.Text.Json.Serialization;

namespace IbgeNomes.Models;

/// <summary>
/// Município devolvido por
/// GET https://servicodados.ibge.gov.br/api/v1/localidades/estados/{UF}/municipios
/// (mesmo formato de /api/v1/localidades/municipios). Só os campos usados pelo app são mapeados.
/// </summary>
public class Municipio
{
    /// <summary>Código IBGE de 7 dígitos (ex.: 3304557 = Rio de Janeiro).</summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;

    public override string ToString() => Nome;
}
