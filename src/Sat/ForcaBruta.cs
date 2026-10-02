namespace Conde.Sat;

/// <summary>
/// O juiz. Enumera as 2^n atribuicoes e responde sem algoritmo nenhum.
///
/// Nao ha poda, nao ha propagacao, nao ha heuristica. Isso e o ponto: um
/// resolvedor errado e um resolvedor que PODA DEMAIS, e um juiz que tambem poda
/// nao pega esse erro. A unica coisa que a forca bruta conhece e a definicao de
/// satisfatibilidade, e por isso ela responde certo por construcao.
///
/// Ela tambem conta modelos, que e uma pergunta mais fina que "sim ou nao": dois
/// resolvedores podem concordar na satisfatibilidade e discordar em quais
/// atribuicoes servem. Contar modelos pega qualquer formula transformada errado.
/// </summary>
public static class ForcaBruta
{
    /// <summary>Acima disso a enumeracao deixa de ser razoavel.</summary>
    public const int MaximoDeVariaveis = 26;

    private static void Conferir(Formula f)
    {
        ArgumentNullException.ThrowIfNull(f);
        if (f.Variaveis > MaximoDeVariaveis)
            throw new ArgumentException($"{f.Variaveis} variaveis sao 2^{f.Variaveis} pontos; o juiz vai ate {MaximoDeVariaveis}", nameof(f));
    }

    /// <summary>
    /// Todas as atribuicoes, em ordem crescente do inteiro que as codifica. O bit
    /// i do contador e o valor da variavel i.
    /// </summary>
    public static IEnumerable<bool[]> Atribuicoes(int variaveis)
    {
        if (variaveis < 0) throw new ArgumentOutOfRangeException(nameof(variaveis));
        if (variaveis > MaximoDeVariaveis) throw new ArgumentOutOfRangeException(nameof(variaveis));
        var total = 1L << variaveis;
        for (long n = 0; n < total; n++)
        {
            var m = new bool[variaveis];
            for (var i = 0; i < variaveis; i++) m[i] = ((n >> i) & 1) == 1;
            yield return m;
        }
    }

    /// <summary>O primeiro modelo em ordem crescente, ou nulo se nao ha nenhum.</summary>
    public static bool[]? PrimeiroModelo(Formula f)
    {
        Conferir(f);
        foreach (var m in Atribuicoes(f.Variaveis))
            if (Avaliacao.Satisfaz(f, m)) return m;
        return null;
    }

    /// <summary>Satisfativel ou nao, pela definicao.</summary>
    public static bool Satisfativel(Formula f) => PrimeiroModelo(f) is not null;

    /// <summary>
    /// Quantas atribuicoes satisfazem a formula. Uma formula sem clausula tem
    /// 2^n modelos, e nao 1: e o E de nada, verdadeiro em todo ponto.
    /// </summary>
    public static long ContarModelos(Formula f)
    {
        Conferir(f);
        var n = 0L;
        foreach (var m in Atribuicoes(f.Variaveis))
            if (Avaliacao.Satisfaz(f, m)) n++;
        return n;
    }

    /// <summary>Todos os modelos, em ordem crescente.</summary>
    public static IEnumerable<bool[]> Modelos(Formula f)
    {
        Conferir(f);
        foreach (var m in Atribuicoes(f.Variaveis))
            if (Avaliacao.Satisfaz(f, m)) yield return m;
    }

    /// <summary>
    /// O maior numero de clausulas que alguma atribuicao consegue satisfazer, que
    /// e o MAX-SAT. Serve de juiz para outra pergunta: uma formula insatisfativel
    /// ainda tem um teto, e saber o teto separa "nao da" de "quase da".
    /// </summary>
    public static int MaximoSatisfeitas(Formula f)
    {
        Conferir(f);
        var melhor = 0;
        foreach (var m in Atribuicoes(f.Variaveis))
        {
            var satisfeitas = f.Quantas - Avaliacao.Insatisfeitas(f, m);
            if (satisfeitas > melhor) melhor = satisfeitas;
        }
        return melhor;
    }
}
