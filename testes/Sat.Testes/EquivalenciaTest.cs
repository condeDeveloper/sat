using Conde.Sat;
using Xunit;

namespace Conde.Sat.Testes;

/// <summary>
/// O teste principal: os dois resolvedores contra o juiz, no DOMINIO INTEIRO.
///
/// Instancia sorteada nao serve para testar correcao, porque ninguem sabe a
/// resposta sem resolver, e porque o sorteio quase nunca cai nos casos que
/// quebram: clausula unitaria que se contradiz, literal puro em formula
/// insatisfativel, clausula que vira vazia na propagacao. Esses casos sao raros
/// no sorteio e sao TODOS os casos aqui.
///
/// Sobre 3 variaveis existem 26 clausulas nao tautologicas de ate 3 literais, e
/// o teste monta toda combinacao de ate 4 delas: 17.901 formulas. Cada uma e
/// resolvida por forca bruta, por DPLL com tres configuracoes e por CDCL, e os
/// cinco tem de concordar.
/// </summary>
public class EquivalenciaTest
{
    private const int Variaveis = 3;

    /// <summary>Toda clausula nao tautologica de 1 a 3 literais sobre 3 variaveis.</summary>
    private static List<Clausula> TodasAsClausulas()
    {
        var literais = new List<Literal>();
        for (var v = 0; v < Variaveis; v++) { literais.Add(Literal.De(v, true)); literais.Add(Literal.De(v, false)); }
        var saida = new List<Clausula>();
        for (var mascara = 1; mascara < 1 << (Variaveis * 2); mascara++)
        {
            var escolhidos = new List<Literal>();
            for (var i = 0; i < Variaveis * 2; i++) if ((mascara & (1 << i)) != 0) escolhidos.Add(literais[i]);
            var c = Clausula.De([.. escolhidos]);
            if (c.EhTautologia()) continue;
            saida.Add(c);
        }
        return saida;
    }

    private static IEnumerable<Formula> TodasAsFormulas(int ateQuantasClausulas)
    {
        var clausulas = TodasAsClausulas();
        var escolha = new int[ateQuantasClausulas];
        for (var k = 1; k <= ateQuantasClausulas; k++)
            foreach (var f in Combinar(clausulas, escolha, k, 0, 0)) yield return f;
    }

    private static IEnumerable<Formula> Combinar(List<Clausula> todas, int[] escolha, int k, int profundidade, int inicio)
    {
        if (profundidade == k)
        {
            var c = new Clausula[k];
            for (var i = 0; i < k; i++) c[i] = todas[escolha[i]];
            yield return new Formula(Variaveis, c);
            yield break;
        }
        for (var i = inicio; i < todas.Count; i++)
        {
            escolha[profundidade] = i;
            foreach (var f in Combinar(todas, escolha, k, profundidade + 1, i + 1)) yield return f;
        }
    }

    /// <summary>
    /// Sao exatamente 26 clausulas: 6 de um literal, 12 de dois e 8 de tres. O
    /// numero entra no teste porque, se a geracao mudar sem querer, todo o
    /// dominio muda junto e o teste continuaria "passando" sobre outra coisa.
    /// </summary>
    [Fact]
    public void ODominioTemOTamanhoQueEuDigo()
    {
        var todas = TodasAsClausulas();
        Assert.Equal(26, todas.Count);
        Assert.Equal(6, todas.Count(c => c.Tamanho == 1));
        Assert.Equal(12, todas.Count(c => c.Tamanho == 2));
        Assert.Equal(8, todas.Count(c => c.Tamanho == 3));
        Assert.Equal(17901, TodasAsFormulas(4).Count());
    }

    /// <summary>
    /// Os quatro resolvedores contra o juiz nas 17.901 formulas. Qualquer
    /// discordancia derruba o teste com a formula impressa.
    /// </summary>
    [Fact]
    public void TodosConcordamComAForcaBruta()
    {
        var dpllSimples = new Dpll(new PrimeiraLivre(), usarLiteralPuro: false);
        var dpllPuro = new Dpll(new PrimeiraLivre(), usarLiteralPuro: true);
        var dpllFrequente = new Dpll(new MaisFrequente(), usarLiteralPuro: true);
        var conferidas = 0;

        foreach (var f in TodasAsFormulas(4))
        {
            var esperado = ForcaBruta.Satisfativel(f);

            foreach (var (nome, r) in new (string, Resultado)[]
                     {
                         ("dpll simples", dpllSimples.Resolver(f)),
                         ("dpll com puro", dpllPuro.Resolver(f)),
                         ("dpll frequente", dpllFrequente.Resolver(f)),
                         ("cdcl", new Cdcl().Resolver(f)),
                     })
            {
                Assert.True(esperado == r.Satisfativel, $"{nome} discordou em {f}: esperado {esperado}");
                Assert.True(r.Confere(f), $"{nome} devolveu modelo que nao satisfaz {f}");
            }
            conferidas++;
        }
        Assert.Equal(17901, conferidas);
    }

    /// <summary>
    /// Contar modelos e mais fino que responder sim ou nao: dois resolvedores
    /// podem concordar na satisfatibilidade e discordar em quais atribuicoes
    /// servem. O contador com poda tem de bater com a enumeracao nas 17.901.
    ///
    /// Este teste foi o que mostrou, na pratica, por que a regra do literal puro
    /// nao entra aqui: com ela, o contador erra a conta em formulas em que o
    /// puro tem os dois valores viaveis.
    /// </summary>
    [Fact]
    public void ContarModelosBateComAEnumeracao()
    {
        var conferidas = 0;
        foreach (var f in TodasAsFormulas(3))
        {
            Assert.Equal(ForcaBruta.ContarModelos(f), Contador.Contar(f));
            conferidas++;
        }
        Assert.Equal(2951, conferidas);
    }

    /// <summary>
    /// A formula sem clausula nenhuma e verdadeira em todo ponto: um E de nada
    /// e verdadeiro. Sao 2^n modelos e nao 1, e errar isso e classico.
    /// </summary>
    [Fact]
    public void FormulaVaziaEhVerdadeiraEmTodoPonto()
    {
        for (var n = 0; n <= 10; n++)
        {
            var f = Formula.Verdadeira(n);
            Assert.True(ForcaBruta.Satisfativel(f));
            Assert.Equal(1L << n, ForcaBruta.ContarModelos(f));
            Assert.Equal(1L << n, Contador.Contar(f));
            Assert.True(new Dpll().Resolver(f).Satisfativel);
            Assert.True(new Cdcl().Resolver(f).Satisfativel);
        }
    }

    /// <summary>
    /// A clausula vazia DENTRO da formula e o contrario: nenhum literal pode
    /// valer, entao nao ha modelo. E o caso que um leitor DIMACS desatento perde.
    /// </summary>
    [Fact]
    public void ClausulaVaziaMataAFormula()
    {
        for (var n = 0; n <= 8; n++)
        {
            var f = Formula.Falsa(n);
            Assert.False(ForcaBruta.Satisfativel(f));
            Assert.Equal(0, ForcaBruta.ContarModelos(f));
            Assert.Equal(0, Contador.Contar(f));
            Assert.False(new Dpll().Resolver(f).Satisfativel);
            Assert.False(new Cdcl().Resolver(f).Satisfativel);
        }
    }
}
