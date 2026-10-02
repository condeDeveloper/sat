namespace Conde.Sat;

/// <summary>
/// Um literal e uma variavel com sinal: x ou nao-x.
///
/// A representacao e a do DIMACS por fora (1, -1, 2, -2...) e a de indice por
/// dentro (0, 1, 2, 3...), porque o indice e o que serve de posicao de vetor.
/// A conta e a classica: variavel v positiva vira 2v, negada vira 2v+1, e negar
/// e inverter o bit de baixo. Isso faz a negacao custar um XOR em vez de um
/// desvio, e e o motivo de todo resolvedor serio guardar literal assim.
/// </summary>
public readonly struct Literal : IEquatable<Literal>
{
    /// <summary>Indice interno: 2*variavel para positivo, 2*variavel+1 para negado.</summary>
    public int Indice { get; }

    private Literal(int indice) => Indice = indice;

    /// <summary>Monta a partir da variavel (base zero) e do sinal.</summary>
    public static Literal De(int variavel, bool positivo)
    {
        if (variavel < 0) throw new ArgumentOutOfRangeException(nameof(variavel), "variavel e base zero");
        return new Literal((variavel << 1) | (positivo ? 0 : 1));
    }

    /// <summary>Monta a partir do indice interno.</summary>
    public static Literal DoIndice(int indice)
    {
        if (indice < 0) throw new ArgumentOutOfRangeException(nameof(indice));
        return new Literal(indice);
    }

    /// <summary>
    /// Monta a partir da notacao do DIMACS, em que a variavel e base UM e o
    /// sinal e o do numero. O zero nao e literal: no formato ele fecha a clausula.
    /// </summary>
    public static Literal DoDimacs(int dimacs)
    {
        if (dimacs == 0) throw new ArgumentOutOfRangeException(nameof(dimacs), "0 fecha clausula, nao e literal");
        return De(Math.Abs(dimacs) - 1, dimacs > 0);
    }

    /// <summary>A variavel, base zero.</summary>
    public int Variavel => Indice >> 1;

    /// <summary>Verdadeiro quando o literal e a variavel sem negacao.</summary>
    public bool Positivo => (Indice & 1) == 0;

    /// <summary>O mesmo literal com o sinal trocado. Um XOR, sem desvio.</summary>
    public Literal Negado => new(Indice ^ 1);

    /// <summary>De volta para a notacao do DIMACS, base um e com sinal.</summary>
    public int Dimacs => Positivo ? Variavel + 1 : -(Variavel + 1);

    public bool Equals(Literal outro) => Indice == outro.Indice;
    public override bool Equals(object? o) => o is Literal l && Equals(l);
    public override int GetHashCode() => Indice;
    public static bool operator ==(Literal a, Literal b) => a.Indice == b.Indice;
    public static bool operator !=(Literal a, Literal b) => a.Indice != b.Indice;

    public override string ToString() => (Positivo ? "x" : "~x") + Variavel;
}
