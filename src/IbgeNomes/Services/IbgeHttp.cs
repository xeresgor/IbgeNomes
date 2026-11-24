using System.Net.Http.Json;
using System.Text.Json;

namespace IbgeNomes.Services;

/// <summary>
/// Ponto único de comunicação HTTP com o IBGE: faz o GET, desserializa o JSON e traduz
/// qualquer falha de rede/servidor/formato em <see cref="IbgeApiException"/>.
/// </summary>
internal static class IbgeHttp
{
    public const string UrlBase = "https://servicodados.ibge.gov.br/api/";

    public static async Task<T?> ObterJsonAsync<T>(HttpClient http, string caminho, CancellationToken cancellationToken)
    {
        HttpResponseMessage resposta;
        try
        {
            resposta = await http.GetAsync(caminho, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // cancelado pelo próprio app (o usuário trocou a seleção): não é erro
        }
        catch (OperationCanceledException ex)
        {
            throw new IbgeApiException("O IBGE demorou demais para responder. Tente novamente.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new IbgeApiException("Sem conexão com o IBGE. Verifique a internet e tente novamente.", ex);
        }

        using (resposta)
        {
            if (!resposta.IsSuccessStatusCode)
            {
                throw new IbgeApiException(
                    $"O IBGE respondeu com erro {(int)resposta.StatusCode}. Tente novamente em instantes.");
            }

            try
            {
                return await resposta.Content.ReadFromJsonAsync<T>(cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                throw new IbgeApiException("O IBGE devolveu dados num formato inesperado.", ex);
            }
        }
    }
}
