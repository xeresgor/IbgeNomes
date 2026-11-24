using System.Globalization;

namespace IbgeNomes.ViewModels;

/// <summary>Uma linha da lista exibida na tela (já formatada para leitura).</summary>
public class RankingItem
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public RankingItem(int posicao, string nome, long frequencia, long maiorFrequencia)
    {
        Posicao = posicao;
        Nome = FormatarNome(nome);
        Frequencia = frequencia;
        Proporcao = maiorFrequencia > 0 ? (double)frequencia / maiorFrequencia : 0;
    }

    public int Posicao { get; }

    /// <summary>"joao" → "Joao" (a API do IBGE devolve os nomes sem acento).</summary>
    public string Nome { get; }

    public long Frequencia { get; }

    /// <summary>"741.049 pessoas".</summary>
    public string FrequenciaTexto => $"{Frequencia.ToString("N0", PtBr)} {(Frequencia == 1 ? "pessoa" : "pessoas")}";

    /// <summary>Frequência relativa ao 1º colocado (0 a 1), usada na barra horizontal.</summary>
    public double Proporcao { get; }

    public static string FormatarNome(string? nome) =>
        string.IsNullOrWhiteSpace(nome) ? string.Empty : PtBr.TextInfo.ToTitleCase(nome.Trim().ToLower(PtBr));
}
