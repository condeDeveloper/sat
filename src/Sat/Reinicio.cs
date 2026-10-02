namespace Conde.Sat;

/// <summary>
/// Quando jogar fora as decisoes e recomecar do zero.
///
/// Reiniciar parece desperdicio e nao e, por um motivo que vale entender: o
/// tempo de um resolvedor numa instancia tem cauda PESADA. Duas execucoes da
/// mesma busca com escolhas iniciais diferentes podem levar segundos e horas. Uma
/// escolha ruim no comeco empurra a busca para uma regiao enorme e sem solucao, e
/// nada dentro da busca percebe isso.
///
/// Recomecar abandona as decisoes mas NAO abandona o que foi aprendido: as
/// clausulas aprendidas ficam, e elas sao justamente o resumo do que deu errado.
/// Entao cada reinicio comeca melhor informado que o anterior.
/// </summary>
public interface IReinicio
{
    /// <summary>Quantos conflitos ate o proximo reinicio.</summary>
    long ProximoLimite();
}

/// <summary>Nunca reinicia. Serve de linha de base para medir o ganho.</summary>
public sealed class SemReinicio : IReinicio
{
    public long ProximoLimite() => long.MaxValue;
}

/// <summary>
/// Sequencia de Luby, de Luby, Sinclair e Zuckerman (1993):
/// 1 1 2 1 1 2 4 1 1 2 1 1 2 4 8 ...
///
/// O teorema e forte: para um processo de tempo desconhecido que pode ser
/// reiniciado, essa sequencia chega dentro de um fator logaritmico do melhor
/// cronograma possivel, e isso SEM saber nada sobre a distribuicao. Nao e um
/// ajuste de parametro que alguem achou medindo; e otima no pior caso.
///
/// A forma da sequencia tem uma logica visivel: muitos reinicios curtos, para o
/// caso de a instancia ser facil a partir de outra escolha, e de vez em quando um
/// bem longo, para o caso de ela exigir mesmo uma busca funda.
/// </summary>
public sealed class Luby : IReinicio
{
    private readonly long _unidade;
    private int _i;

    public Luby(long unidade = 100)
    {
        if (unidade <= 0) throw new ArgumentOutOfRangeException(nameof(unidade));
        _unidade = unidade;
    }

    /// <summary>
    /// O i-esimo termo (base 1). A definicao fechada e de Knuth: se i+1 e uma
    /// potencia de dois, o termo e (i+1)/2; senao, o termo e o de i menos a maior
    /// potencia de dois que cabe, mais um.
    /// </summary>
    public static long Termo(int i)
    {
        if (i < 1) throw new ArgumentOutOfRangeException(nameof(i), "a sequencia e base um");
        long k = 1;
        while (true)
        {
            var potencia = (1L << (int)k) - 1;
            if (potencia == i) return 1L << (int)(k - 1);
            if (potencia > i)
            {
                i -= (int)((1L << (int)(k - 1)) - 1);
                k = 1;
                continue;
            }
            k++;
        }
    }

    public long ProximoLimite() => _unidade * Termo(++_i);
}

/// <summary>Limite que cresce por um fator fixo. Simples, e sem a garantia do Luby.</summary>
public sealed class Geometrico : IReinicio
{
    private readonly double _fator;
    private double _limite;

    public Geometrico(long inicial = 100, double fator = 1.5)
    {
        if (inicial <= 0) throw new ArgumentOutOfRangeException(nameof(inicial));
        if (fator <= 1) throw new ArgumentOutOfRangeException(nameof(fator), "precisa crescer");
        _limite = inicial;
        _fator = fator;
    }

    public long ProximoLimite()
    {
        var atual = (long)_limite;
        _limite *= _fator;
        return atual;
    }
}
