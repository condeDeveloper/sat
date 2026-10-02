namespace Conde.Sat;

/// <summary>
/// Uma clausula e uma disjuncao de literais: vale quando pelo menos um deles vale.
///
/// A clausula nasce normalizada: literais repetidos somem (x ou x e x) e a ordem
/// e a do indice. Isso nao e arrumacao, e o que faz duas clausulas iguais serem
/// o MESMO objeto para efeito de comparacao, e o que deixa a deteccao de
/// tautologia ser uma passada so.
/// </summary>
public sealed class Clausula
{
    private readonly Literal[] _literais;

    private Clausula(Literal[] literais) => _literais = literais;

    /// <summary>A clausula vazia. Nenhum literal pode valer, entao ela e falsa sempre.</summary>
    public static readonly Clausula Vazia = new([]);

    /// <summary>
    /// Monta normalizando: tira repetido e ordena pelo indice do literal.
    /// </summary>
    public static Clausula De(params Literal[] literais)
    {
        ArgumentNullException.ThrowIfNull(literais);
        var vistos = new SortedSet<int>();
        foreach (var l in literais) vistos.Add(l.Indice);
        var saida = new Literal[vistos.Count];
        var i = 0;
        foreach (var indice in vistos) saida[i++] = Literal.DoIndice(indice);
        return new Clausula(saida);
    }

    /// <summary>
    /// Monta tirando repetido mas MANTENDO a ordem dada.
    ///
    /// A normalizacao por indice e o certo para comparar clausulas e o errado
    /// para a clausula aprendida do CDCL: ali a posicao 0 carrega informacao, e o
    /// literal assertivo, o unico que fica livre depois da volta. Ordenar por
    /// indice esconde isso e o resolvedor passa a propagar o literal errado.
    /// </summary>
    public static Clausula NaOrdem(IEnumerable<Literal> literais)
    {
        ArgumentNullException.ThrowIfNull(literais);
        var vistos = new HashSet<int>();
        var saida = new List<Literal>();
        foreach (var l in literais) if (vistos.Add(l.Indice)) saida.Add(l);
        return new Clausula([.. saida]);
    }

    /// <summary>Monta a partir da notacao do DIMACS.</summary>
    public static Clausula DoDimacs(params int[] dimacs)
    {
        ArgumentNullException.ThrowIfNull(dimacs);
        return De(Array.ConvertAll(dimacs, Literal.DoDimacs));
    }

    public int Tamanho => _literais.Length;
    public Literal this[int i] => _literais[i];
    public IReadOnlyList<Literal> Literais => _literais;

    /// <summary>Clausula sem literal nenhum: insatisfativel por si so.</summary>
    public bool EhVazia => _literais.Length == 0;

    /// <summary>Clausula de um literal so: ela obriga o valor dele.</summary>
    public bool EhUnitaria => _literais.Length == 1;

    /// <summary>
    /// Verdadeira quando a clausula contem um literal e a negacao dele, o que a
    /// torna verdadeira em qualquer atribuicao. Como os literais estao ordenados
    /// pelo indice e x e ~x sao vizinhos (2v e 2v+1), basta olhar os pares
    /// consecutivos: uma passada, sem conjunto auxiliar.
    /// </summary>
    public bool EhTautologia()
    {
        for (var i = 1; i < _literais.Length; i++)
            if (_literais[i].Indice == (_literais[i - 1].Indice ^ 1)) return true;
        return false;
    }

    /// <summary>A maior variavel que aparece, ou -1 na clausula vazia.</summary>
    public int MaiorVariavel()
    {
        var maior = -1;
        foreach (var l in _literais) if (l.Variavel > maior) maior = l.Variavel;
        return maior;
    }

    public bool Contem(Literal l) => Array.IndexOf(_literais, l) >= 0;

    /// <summary>
    /// Resolucao de duas clausulas sobre uma variavel: junta as duas tirando o
    /// literal e a negacao dele. Devolve nulo quando a variavel nao aparece com
    /// sinais opostos, e devolve tautologia quando sobra outro par oposto.
    /// </summary>
    public static Clausula? Resolver(Clausula a, Clausula b, int variavel)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        var positivo = Literal.De(variavel, true);
        var negativo = Literal.De(variavel, false);
        Clausula comPositivo, comNegativo;
        if (a.Contem(positivo) && b.Contem(negativo)) { comPositivo = a; comNegativo = b; }
        else if (b.Contem(positivo) && a.Contem(negativo)) { comPositivo = b; comNegativo = a; }
        else return null;

        var juntos = new List<Literal>(comPositivo.Tamanho + comNegativo.Tamanho);
        foreach (var l in comPositivo._literais) if (l != positivo) juntos.Add(l);
        foreach (var l in comNegativo._literais) if (l != negativo) juntos.Add(l);
        return De([.. juntos]);
    }

    public override string ToString() =>
        _literais.Length == 0 ? "()" : "(" + string.Join(" ou ", _literais) + ")";
}
