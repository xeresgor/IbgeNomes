using System.Text.Json.Serialization;

namespace IbgeNomes.Models;

/// <summary>
/// Unidade da Federação devolvida por
/// GET https://servicodados.ibge.gov.br/api/v1/localidades/estados
/// </summary>
public class Estado
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("sigla")]
    public string Sigla { get; set; } = string.Empty;

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;

    [JsonPropertyName("regiao")]
    public Regiao? Regiao { get; set; }

    /// <summary>Texto mostrado no seletor: "Rio de Janeiro (RJ)".</summary>
    [JsonIgnore]
    public string NomeExibicao => $"{Nome} ({Sigla})";

    public override string ToString() => NomeExibicao;
}
