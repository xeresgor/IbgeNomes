using System.Net;
using System.Text;

namespace IbgeNomes.Tests;

/// <summary>Substitui a rede: devolve respostas prontas e registra as URLs pedidas.</summary>
public class HttpFalso : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public HttpFalso(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

    public List<string> UrlsPedidas { get; } = new();

    public static HttpFalso ComJson(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(_ => new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    public HttpClient CriarCliente() => new(this) { BaseAddress = new Uri("https://servicodados.ibge.gov.br/api/") };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        UrlsPedidas.Add(request.RequestUri!.ToString());
        return Task.FromResult(_responder(request));
    }
}
