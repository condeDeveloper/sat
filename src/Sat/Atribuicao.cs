namespace Conde.Sat;

/// <summary>O valor de uma variavel enquanto a busca anda.</summary>
public enum Valor : byte
{
    /// <summary>Ainda nao decidida.</summary>
    Livre = 0,
    Falso = 1,
    Verdadeiro = 2,
}

/// <summary>
/// Uma atribuicao PARCIAL: cada variavel esta em verdadeiro, falso ou livre.
///
/// O terceiro estado e o que separa um resolvedor de uma forca bruta. A forca
/// bruta so conhece atribuicoes totais e por isso precisa visitar 2^n pontos. O
/// resolvedor decide uma variavel de cada vez e pergunta, com o resto ainda
/// livre, se o que ja foi decidido JA e suficiente para matar a formula. Quando
/// e, ele corta 2^(n-k) pontos de uma vez.
///
/// A trilha guarda a ordem em que as variaveis foram atribuidas, e o nivel de
/// cada uma. Desfazer ate um nivel e andar a trilha de tras para frente: nao ha
/// copia de estado, e esse detalhe e a diferenca entre um resolvedor que roda e
/// um que engasga.
/// </summary>
public sealed class Atribuicao
{
    private readonly Valor[] _valores;
    private readonly int[] _nivel;
    private readonly List<int> _trilha;

    public Atribuicao(int variaveis)
    {
        if (variaveis < 0) throw new ArgumentOutOfRangeException(nameof(variaveis));
        _valores = new Valor[variaveis];
        _nivel = new int[variaveis];
        Array.Fill(_nivel, -1);
        _trilha = new List<int>(variaveis);
    }

    public int Variaveis => _valores.Length;

    /// <summary>Quantas variaveis ja foram atribuidas.</summary>
    public int Atribuidas => _trilha.Count;

    /// <summary>Nivel de decisao corrente: quantas decisoes livres foram tomadas.</summary>
    public int Nivel { get; private set; }

    public Valor ValorDe(int variavel) => _valores[variavel];

    /// <summary>Nivel em que a variavel foi atribuida, ou -1 se esta livre.</summary>
    public int NivelDe(int variavel) => _nivel[variavel];

    /// <summary>A ordem em que as variaveis foram atribuidas, da primeira a ultima.</summary>
    public IReadOnlyList<int> Trilha => _trilha;

    /// <summary>Verdadeiro quando todas as variaveis tem valor.</summary>
    public bool Completa => _trilha.Count == _valores.Length;

    /// <summary>O valor do literal sob esta atribuicao, com Livre quando a variavel nao foi decidida.</summary>
    public Valor ValorDoLiteral(Literal l)
    {
        var v = _valores[l.Variavel];
        if (v == Valor.Livre) return Valor.Livre;
        if (l.Positivo) return v;
        return v == Valor.Verdadeiro ? Valor.Falso : Valor.Verdadeiro;
    }

    /// <summary>Verdadeiro quando o literal ja esta satisfeito.</summary>
    public bool Satisfaz(Literal l) => ValorDoLiteral(l) == Valor.Verdadeiro;

    /// <summary>Verdadeiro quando o literal ja esta falso.</summary>
    public bool Falseia(Literal l) => ValorDoLiteral(l) == Valor.Falso;

    /// <summary>Abre um nivel de decisao novo.</summary>
    public void SobeNivel() => Nivel++;

    /// <summary>
    /// Atribui a variavel de modo a satisfazer o literal. A variavel precisa
    /// estar livre: reatribuir por cima seria perder a trilha, e a trilha e o
    /// unico jeito de desfazer.
    /// </summary>
    public void Atribuir(Literal l)
    {
        var v = l.Variavel;
        if (_valores[v] != Valor.Livre)
            throw new InvalidOperationException($"x{v} ja tem valor; desfaca antes de reatribuir");
        _valores[v] = l.Positivo ? Valor.Verdadeiro : Valor.Falso;
        _nivel[v] = Nivel;
        _trilha.Add(v);
    }

    /// <summary>
    /// Desfaz tudo que foi atribuido acima do nivel pedido, andando a trilha de
    /// tras para frente. Custa o numero de variaveis desfeitas, nao o total.
    /// </summary>
    public void DesfazerAte(int nivel)
    {
        if (nivel < 0) throw new ArgumentOutOfRangeException(nameof(nivel));
        while (_trilha.Count > 0)
        {
            var v = _trilha[^1];
            if (_nivel[v] <= nivel) break;
            _trilha.RemoveAt(_trilha.Count - 1);
            _valores[v] = Valor.Livre;
            _nivel[v] = -1;
        }
        Nivel = nivel;
    }

    /// <summary>Uma copia independente, para quem quiser guardar um modelo.</summary>
    public bool[] ComoModelo()
    {
        var m = new bool[_valores.Length];
        for (var i = 0; i < m.Length; i++) m[i] = _valores[i] == Valor.Verdadeiro;
        return m;
    }
}
