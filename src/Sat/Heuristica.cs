namespace Conde.Sat;

/// <summary>Como escolher a proxima variavel e com que valor comecar.</summary>
public interface IHeuristica
{
    /// <summary>
    /// Devolve o literal da proxima decisao, ou nulo quando nao ha variavel livre.
    /// O sinal faz parte da escolha: errar a variavel custa um ramo, errar o
    /// sinal custa uma descida inteira antes de voltar.
    /// </summary>
    Literal? Proxima(Formula f, Atribuicao a);

    /// <summary>Avisa que a variavel participou de um conflito.</summary>
    void Culpou(int variavel) { }
}

/// <summary>A primeira variavel livre, sempre positiva. Serve de linha de base.</summary>
public sealed class PrimeiraLivre : IHeuristica
{
    public Literal? Proxima(Formula f, Atribuicao a)
    {
        ArgumentNullException.ThrowIfNull(f);
        ArgumentNullException.ThrowIfNull(a);
        for (var v = 0; v < f.Variaveis; v++)
            if (a.ValorDe(v) == Valor.Livre) return Literal.De(v, true);
        return null;
    }
}

/// <summary>
/// DLIS: escolhe o literal que aparece em mais clausulas ainda nao satisfeitas.
///
/// A intuicao e direta: satisfazer o literal mais frequente resolve mais
/// clausulas de uma vez. O preco tambem e direto: a contagem e refeita a cada
/// decisao, varrendo a formula inteira. Fica caro antes de ficar bom, e e por
/// isso que os resolvedores modernos abandonaram esse caminho.
/// </summary>
public sealed class MaisFrequente : IHeuristica
{
    public Literal? Proxima(Formula f, Atribuicao a)
    {
        ArgumentNullException.ThrowIfNull(f);
        ArgumentNullException.ThrowIfNull(a);
        var conta = new int[f.Variaveis * 2];
        foreach (var c in f.Clausulas)
        {
            if (Avaliacao.Estado(c, a) == EstadoDaClausula.Satisfeita) continue;
            foreach (var l in c.Literais)
                if (a.ValorDoLiteral(l) == Valor.Livre) conta[l.Indice]++;
        }
        var melhor = -1;
        var melhorConta = 0;
        for (var i = 0; i < conta.Length; i++)
            if (conta[i] > melhorConta) { melhorConta = conta[i]; melhor = i; }
        if (melhor >= 0) return Literal.DoIndice(melhor);
        for (var v = 0; v < f.Variaveis; v++)
            if (a.ValorDe(v) == Valor.Livre) return Literal.De(v, true);
        return null;
    }
}

/// <summary>
/// VSIDS: cada variavel tem uma nota que sobe quando ela aparece num conflito, e
/// TODAS as notas decaem de tempos em tempos.
///
/// O decaimento e o que faz a heuristica funcionar, e e contraintuitivo: ele nao
/// serve para esquecer, serve para dar peso ao que e RECENTE. Uma variavel que
/// causou problema agora vale mais que uma que causou mil conflitos atras, porque
/// a busca ja esta em outra regiao do espaco. Sem decaimento a nota vira um
/// histograma do passado e para de guiar.
///
/// O custo por decisao nao depende da formula, so do numero de variaveis, e a
/// nota ja chega atualizada pelos conflitos. E a diferenca entre varrer a formula
/// a cada decisao e nao varrer.
/// </summary>
public sealed class Vsids : IHeuristica
{
    private readonly double[] _nota;
    private readonly double _decaimento;
    private double _incremento = 1.0;

    public Vsids(int variaveis, double decaimento = 0.95)
    {
        if (variaveis < 0) throw new ArgumentOutOfRangeException(nameof(variaveis));
        if (decaimento is <= 0 or >= 1) throw new ArgumentOutOfRangeException(nameof(decaimento), "fica entre 0 e 1");
        _nota = new double[variaveis];
        _decaimento = decaimento;
    }

    public void Culpou(int variavel)
    {
        _nota[variavel] += _incremento;
        // Em vez de multiplicar as n notas por decaimento, divide-se o incremento:
        // o efeito relativo e o mesmo e o custo cai de O(n) para O(1).
        if (_nota[variavel] > 1e100) Reescalar();
    }

    /// <summary>Fecha a rodada de conflito: o proximo incremento vale mais que este.</summary>
    public void Decair()
    {
        _incremento /= _decaimento;
        if (_incremento > 1e100) Reescalar();
    }

    private void Reescalar()
    {
        for (var i = 0; i < _nota.Length; i++) _nota[i] *= 1e-100;
        _incremento *= 1e-100;
    }

    public double NotaDe(int variavel) => _nota[variavel];

    public Literal? Proxima(Formula f, Atribuicao a)
    {
        ArgumentNullException.ThrowIfNull(f);
        ArgumentNullException.ThrowIfNull(a);
        var melhor = -1;
        var melhorNota = double.NegativeInfinity;
        for (var v = 0; v < f.Variaveis; v++)
        {
            if (a.ValorDe(v) != Valor.Livre) continue;
            if (_nota[v] > melhorNota) { melhorNota = _nota[v]; melhor = v; }
        }
        return melhor < 0 ? null : Literal.De(melhor, false);
    }
}
