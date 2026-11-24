using System.Text.Json.Serialization;

namespace IbgeNomes.Models;

/// <summary>Grande região do Brasil (Norte, Nordeste, Sudeste, Sul, Centro-Oeste).</summary>
public class Regiao
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("sigla")]
    public string Sigla { get; set; } = string.Empty;

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;
}
