namespace Conde.Sat;

/// <summary>O estado de uma clausula sob uma atribuicao parcial.</summary>
public enum EstadoDaClausula
{
    /// <summary>Algum literal ja vale: a clausula esta resolvida.</summary>
    Satisfeita,
    /// <summary>Todos os literais ja sao falsos: nao ha mais como salvar.</summary>
    Conflitante,
    /// <summary>Sobrou exatamente um literal livre e o resto e falso: ele e obrigado.</summary>
    Unitaria,
    /// <summary>Sobrou mais de um literal livre.</summary>
    Indefinida,
}

/// <summary>
/// Avaliacao de clausula e de formula sob atribuicao parcial.
///
/// Os quatro estados sao o coracao do DPLL. Conflitante manda voltar, Unitaria
/// manda propagar, Satisfeita manda ignorar e Indefinida manda continuar. Um
/// resolvedor e, em boa medida, um laco que pergunta isso.
/// </summary>
public static class Avaliacao
{
    /// <summary>
    /// Classifica a clausula. A ordem das perguntas importa: satisfeita vem antes
    /// de tudo, porque uma clausula com um literal verdadeiro nao e unitaria nem
    /// conflitante mesmo que todos os outros sejam falsos.
    /// </summary>
    public static EstadoDaClausula Estado(Clausula c, Atribuicao a, out Literal obrigado)
    {
        ArgumentNullException.ThrowIfNull(c);
        ArgumentNullException.ThrowIfNull(a);
        obrigado = default;
        var livres = 0;
        var ultimoLivre = default(Literal);
        foreach (var l in c.Literais)
        {
            var v = a.ValorDoLiteral(l);
            if (v == Valor.Verdadeiro) return EstadoDaClausula.Satisfeita;
            if (v == Valor.Livre) { livres++; ultimoLivre = l; if (livres > 1) break; }
        }
        if (livres == 0) return EstadoDaClausula.Conflitante;
        if (livres == 1) { obrigado = ultimoLivre; return EstadoDaClausula.Unitaria; }
        return EstadoDaClausula.Indefinida;
    }

    /// <summary>Classifica a clausula sem querer saber qual literal ficou obrigado.</summary>
    public static EstadoDaClausula Estado(Clausula c, Atribuicao a) => Estado(c, a, out _);

    /// <summary>
    /// Verdadeiro quando a atribuicao COMPLETA satisfaz a formula inteira.
    /// Exige completude de proposito: com atribuicao parcial a pergunta certa
    /// nao e "satisfaz" e sim "ainda pode satisfazer", que e outra funcao.
    /// </summary>
    public static bool Satisfaz(Formula f, bool[] modelo)
    {
        ArgumentNullException.ThrowIfNull(f);
        ArgumentNullException.ThrowIfNull(modelo);
        if (modelo.Length != f.Variaveis)
            throw new ArgumentException($"o modelo tem {modelo.Length} valores e a formula declara {f.Variaveis}", nameof(modelo));
        foreach (var c in f.Clausulas)
        {
            var alguem = false;
            foreach (var l in c.Literais)
            {
                if (modelo[l.Variavel] == l.Positivo) { alguem = true; break; }
            }
            if (!alguem) return false;
        }
        return true;
    }

    /// <summary>
    /// Verdadeiro quando nenhuma clausula ja esta conflitante. Nao quer dizer que
    /// a formula seja satisfativel dali para a frente, so que ainda nao morreu.
    /// </summary>
    public static bool AindaViva(Formula f, Atribuicao a)
    {
        ArgumentNullException.ThrowIfNull(f);
        foreach (var c in f.Clausulas)
            if (Estado(c, a) == EstadoDaClausula.Conflitante) return false;
        return true;
    }

    /// <summary>Quantas clausulas o modelo deixa insatisfeitas. Zero e um modelo.</summary>
    public static int Insatisfeitas(Formula f, bool[] modelo)
    {
        ArgumentNullException.ThrowIfNull(f);
        ArgumentNullException.ThrowIfNull(modelo);
        var n = 0;
        foreach (var c in f.Clausulas)
        {
            var alguem = false;
            foreach (var l in c.Literais)
            {
                if (modelo[l.Variavel] == l.Positivo) { alguem = true; break; }
            }
            if (!alguem) n++;
        }
        return n;
    }
}
