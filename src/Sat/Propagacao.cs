namespace Conde.Sat;

/// <summary>O que aconteceu numa rodada de propagacao.</summary>
public enum ResultadoDaPropagacao
{
    /// <summary>Nada mais e obrigado; a busca continua.</summary>
    Estavel,
    /// <summary>Alguma clausula ficou toda falsa.</summary>
    Conflito,
}

/// <summary>
/// Propagacao unitaria e a regra que faz um resolvedor valer a pena.
///
/// Se uma clausula tem todos os literais falsos menos um livre, esse literal NAO
/// E UMA ESCOLHA: ou ele vale, ou a clausula morre. Atribuir o obrigado nao perde
/// solucao nenhuma, e cada obrigado costuma criar outros. Uma decisao pode
/// arrastar dezenas de variaveis sem abrir ramo.
///
/// O laco roda ate nao haver mais nada obrigado, porque propagar uma vez so
/// deixa trabalho na mesa: uma clausula que nao era unitaria antes pode virar
/// depois da atribuicao anterior.
/// </summary>
public static class Propagacao
{
    /// <summary>
    /// Propaga ate estabilizar. Devolve conflito na primeira clausula que morrer,
    /// e nesse caso a atribuicao fica como estava no momento do conflito: quem
    /// chamou e que decide ate onde desfazer.
    /// </summary>
    public static ResultadoDaPropagacao Propagar(Formula f, Atribuicao a) => Propagar(f, a, out _);

    /// <summary>
    /// Igual, e ainda conta quantas variaveis foram atribuidas sem decisao. Esse
    /// numero e o que mostra o tamanho do ganho: numa formula com muita clausula
    /// curta ele passa de dez por decisao.
    /// </summary>
    public static ResultadoDaPropagacao Propagar(Formula f, Atribuicao a, out int obrigadas)
    {
        ArgumentNullException.ThrowIfNull(f);
        ArgumentNullException.ThrowIfNull(a);
        obrigadas = 0;
        bool mudou;
        do
        {
            mudou = false;
            foreach (var c in f.Clausulas)
            {
                var estado = Avaliacao.Estado(c, a, out var obrigado);
                if (estado == EstadoDaClausula.Conflitante) return ResultadoDaPropagacao.Conflito;
                if (estado != EstadoDaClausula.Unitaria) continue;
                a.Atribuir(obrigado);
                obrigadas++;
                mudou = true;
            }
        } while (mudou);
        return ResultadoDaPropagacao.Estavel;
    }

    /// <summary>
    /// Um literal e PURO quando a variavel dele so aparece com um sinal nas
    /// clausulas ainda nao satisfeitas. Atribuir o puro de modo a satisfazer
    /// nunca piora: ele so aparece em clausulas que passam a estar satisfeitas,
    /// e nao aparece em nenhuma que passe a sofrer.
    ///
    /// A regra nao preserva o NUMERO de modelos, so a satisfatibilidade. Por isso
    /// o DPLL daqui usa ela e o contador de modelos nao.
    /// </summary>
    public static IReadOnlyList<Literal> LiteraisPuros(Formula f, Atribuicao a)
    {
        ArgumentNullException.ThrowIfNull(f);
        ArgumentNullException.ThrowIfNull(a);
        var apareceuPositivo = new bool[f.Variaveis];
        var apareceuNegativo = new bool[f.Variaveis];
        foreach (var c in f.Clausulas)
        {
            if (Avaliacao.Estado(c, a) == EstadoDaClausula.Satisfeita) continue;
            foreach (var l in c.Literais)
            {
                if (a.ValorDoLiteral(l) != Valor.Livre) continue;
                if (l.Positivo) apareceuPositivo[l.Variavel] = true;
                else apareceuNegativo[l.Variavel] = true;
            }
        }
        var puros = new List<Literal>();
        for (var v = 0; v < f.Variaveis; v++)
        {
            if (a.ValorDe(v) != Valor.Livre) continue;
            if (apareceuPositivo[v] && !apareceuNegativo[v]) puros.Add(Literal.De(v, true));
            else if (apareceuNegativo[v] && !apareceuPositivo[v]) puros.Add(Literal.De(v, false));
        }
        return puros;
    }
}
