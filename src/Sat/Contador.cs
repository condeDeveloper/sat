namespace Conde.Sat;

/// <summary>
/// Contagem de modelos (#SAT): quantas atribuicoes satisfazem a formula.
///
/// E uma pergunta mais dura que satisfatibilidade, e nao por pouco: SAT esta em
/// NP e #SAT e completo para #P, uma classe acima. Saber que existe solucao nao
/// ajuda a contar quantas sao, porque parar na primeira e exatamente o que o
/// resolvedor faz de melhor.
///
/// Isso aparece no codigo de um jeito concreto: a regra do literal puro, que o
/// DPLL usa a vontade, NAO VALE aqui. Ela preserva a satisfatibilidade e nao o
/// numero de modelos, porque fixar o puro joga fora as atribuicoes em que ele
/// tem o outro valor e ainda assim alguma outra clausula segura a formula.
///
/// O que vale e o resto: quando todas as clausulas estao satisfeitas e sobram k
/// variaveis livres, sao 2^k modelos de uma vez, sem visitar nenhum. E dai que
/// vem todo o ganho sobre a enumeracao.
/// </summary>
public static class Contador
{
    /// <summary>Conta os modelos com busca e corte, sem enumerar 2^n.</summary>
    public static long Contar(Formula f)
    {
        ArgumentNullException.ThrowIfNull(f);
        if (f.Variaveis > 62) throw new ArgumentOutOfRangeException(nameof(f), "2^n nao caberia em long");
        var a = new Atribuicao(f.Variaveis);
        return Contar(f, a);
    }

    private static long Contar(Formula f, Atribuicao a)
    {
        // Propagacao unitaria PODE ser usada: ela nao descarta modelo nenhum,
        // porque o literal obrigado e obrigado em todos eles.
        var nivel = a.Nivel;
        if (Propagacao.Propagar(f, a) == ResultadoDaPropagacao.Conflito)
        {
            a.DesfazerAte(nivel);
            return 0;
        }

        var faltam = 0;
        var pendente = false;
        foreach (var c in f.Clausulas)
        {
            if (Avaliacao.Estado(c, a) != EstadoDaClausula.Satisfeita) { pendente = true; break; }
        }
        if (!pendente)
        {
            for (var v = 0; v < f.Variaveis; v++) if (a.ValorDe(v) == Valor.Livre) faltam++;
            a.DesfazerAte(nivel);
            return 1L << faltam;
        }

        var escolhida = -1;
        for (var v = 0; v < f.Variaveis; v++) if (a.ValorDe(v) == Valor.Livre) { escolhida = v; break; }
        if (escolhida < 0) { a.DesfazerAte(nivel); return 0; }

        long total = 0;
        foreach (var positivo in new[] { true, false })
        {
            a.SobeNivel();
            a.Atribuir(Literal.De(escolhida, positivo));
            total += Contar(f, a);
            a.DesfazerAte(a.Nivel - 1);
        }
        a.DesfazerAte(nivel);
        return total;
    }
}
