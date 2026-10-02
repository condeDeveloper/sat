namespace Conde.Sat;

/// <summary>
/// DPLL: Davis, Putnam, Logemann e Loveland, 1962.
///
/// A forca bruta visita 2^n folhas. O DPLL visita as mesmas folhas em potencial,
/// mas corta subarvores inteiras assim que uma clausula morre, e antes de abrir
/// cada ramo ele esgota o que e obrigado. Sao tres regras, e nenhuma delas chuta:
///
///   1. clausula vazia  -> este ramo morreu, volta
///   2. clausula unitaria -> o literal e obrigado, atribui sem abrir ramo
///   3. literal puro -> atribui para satisfazer, nunca piora
///
/// Sessenta anos depois, isso ainda e o esqueleto de todo resolvedor usado em
/// producao. O que mudou foi o que se faz NO CONFLITO, e e dai que vem o CDCL.
///
/// A volta aqui e cronologica: desfaz a ultima decisao e tenta o outro sinal.
/// Essa e a parte que o CDCL joga fora.
/// </summary>
public sealed class Dpll
{
    private readonly IHeuristica _heuristica;
    private readonly bool _usarLiteralPuro;
    private long _decisoes, _propagacoes, _conflitos;

    public Dpll(IHeuristica? heuristica = null, bool usarLiteralPuro = true)
    {
        _heuristica = heuristica ?? new PrimeiraLivre();
        _usarLiteralPuro = usarLiteralPuro;
    }

    public Resultado Resolver(Formula f)
    {
        ArgumentNullException.ThrowIfNull(f);
        _decisoes = _propagacoes = _conflitos = 0;
        var a = new Atribuicao(f.Variaveis);
        var ok = Buscar(f, a);
        return ok
            ? Resultado.Sim(a.ComoModelo(), _decisoes, _propagacoes, _conflitos)
            : Resultado.Nao(_decisoes, _propagacoes, _conflitos);
    }

    private bool Buscar(Formula f, Atribuicao a)
    {
        if (Propagacao.Propagar(f, a, out var obrigadas) == ResultadoDaPropagacao.Conflito)
        {
            _propagacoes += obrigadas;
            _conflitos++;
            return false;
        }
        _propagacoes += obrigadas;

        if (_usarLiteralPuro)
        {
            foreach (var puro in Propagacao.LiteraisPuros(f, a))
            {
                if (a.ValorDe(puro.Variavel) != Valor.Livre) continue;
                a.Atribuir(puro);
                _propagacoes++;
            }
        }

        // Todas as clausulas satisfeitas? Entao acabou, mesmo que sobrem
        // variaveis livres: o modelo completa com qualquer valor.
        if (TodasSatisfeitas(f, a))
        {
            CompletarLivres(f, a);
            return true;
        }

        var escolha = _heuristica.Proxima(f, a);
        if (escolha is null)
        {
            // Sem variavel livre e com clausula nao satisfeita: este ramo morreu.
            _conflitos++;
            return false;
        }

        var literal = escolha.Value;
        var nivel = a.Nivel;
        foreach (var tentativa in new[] { literal, literal.Negado })
        {
            a.SobeNivel();
            _decisoes++;
            a.Atribuir(tentativa);
            if (Buscar(f, a)) return true;
            _heuristica.Culpou(tentativa.Variavel);
            a.DesfazerAte(nivel);
        }
        return false;
    }

    private static bool TodasSatisfeitas(Formula f, Atribuicao a)
    {
        foreach (var c in f.Clausulas)
            if (Avaliacao.Estado(c, a) != EstadoDaClausula.Satisfeita) return false;
        return true;
    }

    /// <summary>
    /// Fecha as variaveis que sobraram livres. Elas nao aparecem em clausula
    /// insatisfeita nenhuma, entao o valor nao muda a resposta; o que muda e o
    /// contrato do modelo, que precisa ter um valor por variavel declarada.
    /// </summary>
    private static void CompletarLivres(Formula f, Atribuicao a)
    {
        for (var v = 0; v < f.Variaveis; v++)
            if (a.ValorDe(v) == Valor.Livre) a.Atribuir(Literal.De(v, false));
    }
}
