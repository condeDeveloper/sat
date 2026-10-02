namespace Conde.Sat;

/// <summary>
/// Familias de formulas com resposta conhecida de antemao.
///
/// Instancia sorteada serve para medir tempo medio e nao serve para testar
/// correcao: ninguem sabe a resposta sem resolver. Estas familias sabem, por
/// construcao ou por teorema, e por isso valem como juiz em tamanhos onde a
/// forca bruta ja nao alcanca.
/// </summary>
public static class Geradores
{
    /// <summary>
    /// Princípio das casas de pombo: n+1 pombos em n casas.
    ///
    /// E insatisfativel por um argumento de contagem que uma crianca entende, e
    /// e o exemplo mais famoso de formula FACIL de entender e DIFICIL de provar
    /// para o metodo. Haken mostrou em 1985 que toda prova por resolucao dela
    /// tem tamanho exponencial, e o CDCL produz provas por resolucao. Nao e
    /// limitacao de implementacao: e limite do metodo.
    ///
    /// Variavel x(p,c) quer dizer "o pombo p esta na casa c".
    /// </summary>
    public static Formula Pombos(int casas)
    {
        if (casas < 0) throw new ArgumentOutOfRangeException(nameof(casas));
        var pombos = casas + 1;
        int X(int p, int c) => p * casas + c;
        var clausulas = new List<Clausula>();
        // todo pombo esta em alguma casa
        for (var p = 0; p < pombos; p++)
        {
            var literais = new Literal[casas];
            for (var c = 0; c < casas; c++) literais[c] = Literal.De(X(p, c), true);
            clausulas.Add(Clausula.De(literais));
        }
        // nenhuma casa tem dois pombos
        for (var c = 0; c < casas; c++)
            for (var p = 0; p < pombos; p++)
                for (var q = p + 1; q < pombos; q++)
                    clausulas.Add(Clausula.De(Literal.De(X(p, c), false), Literal.De(X(q, c), false)));
        return new Formula(pombos * casas, clausulas);
    }

    /// <summary>
    /// Coloracao de grafo com k cores, em CNF. Satisfativel exatamente quando o
    /// grafo e k-colorivel, o que da um juiz externo: um grafo completo de n
    /// vertices precisa de n cores, nunca menos.
    /// </summary>
    public static Formula Coloracao(int vertices, IEnumerable<(int A, int B)> arestas, int cores)
    {
        ArgumentNullException.ThrowIfNull(arestas);
        if (vertices < 0) throw new ArgumentOutOfRangeException(nameof(vertices));
        if (cores < 1) throw new ArgumentOutOfRangeException(nameof(cores));
        int X(int v, int c) => v * cores + c;
        var clausulas = new List<Clausula>();
        for (var v = 0; v < vertices; v++)
        {
            var literais = new Literal[cores];
            for (var c = 0; c < cores; c++) literais[c] = Literal.De(X(v, c), true);
            clausulas.Add(Clausula.De(literais));
            for (var c = 0; c < cores; c++)
                for (var d = c + 1; d < cores; d++)
                    clausulas.Add(Clausula.De(Literal.De(X(v, c), false), Literal.De(X(v, d), false)));
        }
        foreach (var (a, b) in arestas)
            for (var c = 0; c < cores; c++)
                clausulas.Add(Clausula.De(Literal.De(X(a, c), false), Literal.De(X(b, c), false)));
        return new Formula(vertices * cores, clausulas);
    }

    /// <summary>Grafo completo: toda aresta presente.</summary>
    public static IEnumerable<(int A, int B)> Completo(int vertices)
    {
        for (var a = 0; a < vertices; a++)
            for (var b = a + 1; b < vertices; b++)
                yield return (a, b);
    }

    /// <summary>
    /// 3-SAT sorteado com m clausulas e n variaveis.
    ///
    /// A razao m/n e o parametro que importa: abaixo de 4,27 quase toda instancia
    /// e satisfativel, acima quase nenhuma, e a transicao fica mais abrupta
    /// quanto maior o n. As instancias dificeis estao EM CIMA do ponto, e nao
    /// longe dele: longe, a resposta aparece rapido dos dois lados.
    /// </summary>
    public static Formula TresSat(int variaveis, int clausulas, int semente)
    {
        if (variaveis < 3) throw new ArgumentOutOfRangeException(nameof(variaveis), "3-SAT precisa de ao menos 3");
        if (clausulas < 0) throw new ArgumentOutOfRangeException(nameof(clausulas));
        var r = new Random(semente);
        var saida = new List<Clausula>(clausulas);
        while (saida.Count < clausulas)
        {
            var a = r.Next(variaveis);
            int b; do { b = r.Next(variaveis); } while (b == a);
            int c; do { c = r.Next(variaveis); } while (c == a || c == b);
            var cl = Clausula.De(
                Literal.De(a, r.Next(2) == 0),
                Literal.De(b, r.Next(2) == 0),
                Literal.De(c, r.Next(2) == 0));
            if (cl.EhTautologia()) continue;
            saida.Add(cl);
        }
        return new Formula(variaveis, saida);
    }

    /// <summary>
    /// A razao em que metade das instancias de 3-SAT sorteadas e satisfativel.
    /// Valor numerico de Crawford e Auton (1996) e Mitchell, Selman e Levesque.
    /// </summary>
    public const double RazaoCritica = 4.27;

    /// <summary>
    /// Uma formula satisfativel POR CONSTRUCAO: sorteia um modelo primeiro e so
    /// aceita clausula que ele satisfaz.
    ///
    /// Serve para testar o caminho do "sim" em tamanho grande, onde a forca bruta
    /// nao alcanca: a resposta e conhecida e o modelo escondido e uma testemunha.
    /// Note que a instancia fica MAIS FACIL que uma sorteada da mesma razao, e e
    /// por isso que ela nao serve para medir tempo.
    /// </summary>
    public static (Formula Formula, bool[] Escondido) Plantada(int variaveis, int clausulas, int semente)
    {
        if (variaveis < 3) throw new ArgumentOutOfRangeException(nameof(variaveis));
        var r = new Random(semente);
        var escondido = new bool[variaveis];
        for (var i = 0; i < variaveis; i++) escondido[i] = r.Next(2) == 0;
        var saida = new List<Clausula>(clausulas);
        while (saida.Count < clausulas)
        {
            var a = r.Next(variaveis);
            int b; do { b = r.Next(variaveis); } while (b == a);
            int c; do { c = r.Next(variaveis); } while (c == a || c == b);
            var cl = Clausula.De(
                Literal.De(a, r.Next(2) == 0),
                Literal.De(b, r.Next(2) == 0),
                Literal.De(c, r.Next(2) == 0));
            if (cl.EhTautologia()) continue;
            var serve = false;
            foreach (var l in cl.Literais) if (escondido[l.Variavel] == l.Positivo) { serve = true; break; }
            if (!serve) continue;
            saida.Add(cl);
        }
        return (new Formula(variaveis, saida), escondido);
    }
}
