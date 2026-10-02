namespace Conde.Sat;

/// <summary>
/// Uma formula na forma normal conjuntiva: um E de clausulas, cada clausula um
/// OU de literais. Toda formula proposicional tem uma equivalente nessa forma,
/// e e nela que os resolvedores trabalham.
///
/// O numero de variaveis e declarado e nao deduzido. A diferenca importa: uma
/// formula pode declarar dez variaveis e so usar tres, e nesse caso ela tem 2^10
/// atribuicoes e nao 2^3. Deduzir pelo maior literal mudaria a contagem de
/// modelos sem mudar a satisfatibilidade, o que e exatamente o tipo de bug que
/// passa despercebido.
/// </summary>
public sealed class Formula
{
    private readonly Clausula[] _clausulas;

    public Formula(int variaveis, IEnumerable<Clausula> clausulas)
    {
        ArgumentNullException.ThrowIfNull(clausulas);
        if (variaveis < 0) throw new ArgumentOutOfRangeException(nameof(variaveis));
        _clausulas = [.. clausulas];
        foreach (var c in _clausulas)
            if (c.MaiorVariavel() >= variaveis)
                throw new ArgumentException($"a clausula {c} usa variavel fora das {variaveis} declaradas", nameof(clausulas));
        Variaveis = variaveis;
    }

    /// <summary>Quantas variaveis a formula declara. O dominio tem 2^Variaveis pontos.</summary>
    public int Variaveis { get; }

    public int Quantas => _clausulas.Length;
    public Clausula this[int i] => _clausulas[i];
    public IReadOnlyList<Clausula> Clausulas => _clausulas;

    /// <summary>
    /// Formula sem clausula nenhuma. E verdadeira em toda atribuicao, porque um
    /// E de nada e verdadeiro. Vale a pena dizer isso em voz alta: e o caso base
    /// de toda recursao daqui.
    /// </summary>
    public static Formula Verdadeira(int variaveis) => new(variaveis, []);

    /// <summary>Formula com a clausula vazia dentro: falsa em toda atribuicao.</summary>
    public static Formula Falsa(int variaveis) => new(variaveis, [Clausula.Vazia]);

    /// <summary>Monta a partir de clausulas em notacao DIMACS.</summary>
    public static Formula DoDimacs(int variaveis, params int[][] clausulas)
    {
        ArgumentNullException.ThrowIfNull(clausulas);
        return new Formula(variaveis, Array.ConvertAll(clausulas, Clausula.DoDimacs));
    }

    /// <summary>A soma dos tamanhos das clausulas, que e o tamanho real da entrada.</summary>
    public int Literais
    {
        get
        {
            var soma = 0;
            foreach (var c in _clausulas) soma += c.Tamanho;
            return soma;
        }
    }

    /// <summary>Uma formula igual, sem as clausulas que sao tautologia.</summary>
    public Formula SemTautologias()
    {
        var ficam = new List<Clausula>(_clausulas.Length);
        foreach (var c in _clausulas) if (!c.EhTautologia()) ficam.Add(c);
        return new Formula(Variaveis, ficam);
    }

    public override string ToString() =>
        _clausulas.Length == 0 ? "(vazia, verdadeira)" : string.Join(" e ", (object[])_clausulas);
}
