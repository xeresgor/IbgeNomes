using System.Text.Json.Serialization;

namespace IbgeNomes.Models;

/// <summary>
/// Resposta de GET https://servicodados.ibge.gov.br/api/v3/nomes/2022/localidade/{id_localidade}
/// (Censo 2022). Exemplo:
/// { "localidade": 33, "mulheres": [...], "homens": [...], "sobrenomes": [...] }
/// </summary>
public class NomesLocalidade
{
    [JsonPropertyName("localidade")]
    public long Localidade { get; set; }

    [JsonPropertyName("mulheres")]
    public List<NomeFrequencia> Mulheres { get; set; } = new();

    [JsonPropertyName("homens")]
    public List<NomeFrequencia> Homens { get; set; } = new();

    [JsonPropertyName("sobrenomes")]
    public List<NomeFrequencia> Sobrenomes { get; set; } = new();

    [JsonIgnore]
    public bool EstaVazio => Mulheres.Count == 0 && Homens.Count == 0 && Sobrenomes.Count == 0;
}
