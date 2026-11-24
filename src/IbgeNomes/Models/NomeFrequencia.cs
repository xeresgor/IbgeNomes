using System.Text.Json.Serialization;

namespace IbgeNomes.Models;

/// <summary>Um nome (ou sobrenome) e quantas pessoas o têm na localidade.</summary>
public class NomeFrequencia
{
    /// <summary>Vem da API em minúsculas e sem acento (ex.: "joao").</summary>
    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;

    [JsonPropertyName("frequencia")]
    public long Frequencia { get; set; }
}
