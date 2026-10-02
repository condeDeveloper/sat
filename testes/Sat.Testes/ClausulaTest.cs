using Conde.Sat;
using Xunit;

namespace Conde.Sat.Testes;

public class ClausulaTest
{
    /// <summary>
    /// A deteccao de tautologia e conferida contra a definicao, em TODAS as
    /// clausulas sobre 4 variaveis: sao 8 literais, 256 subconjuntos. A versao
    /// rapida olha pares vizinhos; a definicao procura o par oposto em qualquer
    /// posicao. As duas tem de concordar nos 256.
    /// </summary>
    [Fact]
    public void TautologiaBateComADefinicao()
    {
        const int variaveis = 4;
        var literais = new List<Literal>();
        for (var v = 0; v < variaveis; v++) { literais.Add(Literal.De(v, true)); literais.Add(Literal.De(v, false)); }

        for (var mascara = 0; mascara < 1 << 8; mascara++)
        {
            var escolhidos = new List<Literal>();
            for (var i = 0; i < 8; i++) if ((mascara & (1 << i)) != 0) escolhidos.Add(literais[i]);
            var c = Clausula.De([.. escolhidos]);

            var pelaDefinicao = false;
            foreach (var a in escolhidos)
                foreach (var b in escolhidos)
                    if (a == b.Negado) pelaDefinicao = true;

            Assert.Equal(pelaDefinicao, c.EhTautologia());
        }
    }

    /// <summary>
    /// Tautologia e verdadeira em TODA atribuicao, e aqui isso e medido em vez de
    /// afirmado: cada clausula que o metodo chama de tautologia e avaliada nos
    /// 16 pontos do dominio de 4 variaveis.
    /// </summary>
    [Fact]
    public void TautologiaValeEmTodoPonto()
    {
        var tautologias = 0;
        for (var a = 0; a < 4; a++)
            for (var b = 0; b < 4; b++)
            {
                var c = Clausula.De(Literal.De(a, true), Literal.De(a, false), Literal.De(b, true));
                Assert.True(c.EhTautologia());
                tautologias++;
                var f = new Formula(4, [c]);
                Assert.Equal(16, ForcaBruta.ContarModelos(f));
            }
        Assert.Equal(16, tautologias);
    }

    /// <summary>
    /// Normalizar tira repetido: (x ou x) e (x). O tamanho cai e o significado
    /// nao muda, o que e conferido contando modelos nos dois.
    /// </summary>
    [Fact]
    public void RepetidoSomeSemMudarOSignificado()
    {
        var x = Literal.De(0, true);
        var comRepeticao = Clausula.De(x, x, x, Literal.De(1, false));
        Assert.Equal(2, comRepeticao.Tamanho);
        Assert.Equal(
            ForcaBruta.ContarModelos(new Formula(2, [Clausula.De(x, Literal.De(1, false))])),
            ForcaBruta.ContarModelos(new Formula(2, [comRepeticao])));
    }

    /// <summary>
    /// Resolucao: de (x ou A) e (~x ou B) sai (A ou B), e a clausula que sai e
    /// consequencia logica das duas. Isso e conferido pela definicao: todo modelo
    /// das duas originais satisfaz a resolvente, nos 32 pontos de 5 variaveis.
    /// </summary>
    [Fact]
    public void ResolventeEhConsequencia()
    {
        var a = Clausula.DoDimacs(1, 2, 3);
        var b = Clausula.DoDimacs(-1, 4, 5);
        var r = Clausula.Resolver(a, b, 0);
        Assert.NotNull(r);
        Assert.Equal(4, r!.Tamanho);

        foreach (var m in ForcaBruta.Atribuicoes(5))
        {
            var valeA = Avaliacao.Satisfaz(new Formula(5, [a]), m);
            var valeB = Avaliacao.Satisfaz(new Formula(5, [b]), m);
            if (!valeA || !valeB) continue;
            Assert.True(Avaliacao.Satisfaz(new Formula(5, [r]), m));
        }
    }

    /// <summary>Sem par oposto na variavel pedida, nao ha resolvente.</summary>
    [Fact]
    public void SemParOpostoNaoResolve()
    {
        Assert.Null(Clausula.Resolver(Clausula.DoDimacs(1, 2), Clausula.DoDimacs(3, 4), 0));
    }

    /// <summary>
    /// NaOrdem mantem a posicao, De ordena por indice. A diferenca existe para o
    /// CDCL, cuja clausula aprendida carrega informacao na posicao 0.
    /// </summary>
    [Fact]
    public void NaOrdemMantemAPosicao()
    {
        var literais = new[] { Literal.De(9, false), Literal.De(1, true), Literal.De(5, true) };
        var naOrdem = Clausula.NaOrdem(literais);
        Assert.Equal(literais[0], naOrdem[0]);

        var ordenada = Clausula.De(literais);
        Assert.Equal(Literal.De(1, true), ordenada[0]);
        Assert.Equal(naOrdem.Tamanho, ordenada.Tamanho);
    }
}
