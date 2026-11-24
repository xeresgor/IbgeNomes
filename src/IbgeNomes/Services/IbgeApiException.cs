namespace IbgeNomes.Services;

/// <summary>
/// Falha de comunicação com as APIs do IBGE, já com uma mensagem pronta para o usuário.
/// A camada de apresentação só precisa capturar este tipo.
/// </summary>
public class IbgeApiException : Exception
{
    public IbgeApiException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
