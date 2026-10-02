using Conde.Sat;
using Xunit;

namespace Conde.Sat.Testes;

public class PropagacaoTest
{
    /// <summary>
    /// Uma cadeia de implicacoes: x1, e x1 implica x2, que implica x3... Uma
    /// unica clausula unitaria no comeco arrasta a cadeia inteira sem decisao
    /// nenhuma. Com 50 elos sao 50 variaveis atribuidas de graca.
    /// </summary>
    [Fact]
    public void UmaUnitariaArrastaACadeiaInteira()
    {
        const int n = 50;
        var clausulas = new List<Clausula> { Clausula.De(Literal.De(0, true)) };
        for (var i = 0; i + 1 < n; i++)
            clausulas.Add(Clausula.De(Literal.De(i, false), Literal.De(i + 1, true)));

        var f = new Formula(n, clausulas);
        var a = new Atribuicao(n);
        Assert.Equal(ResultadoDaPropagacao.Estavel, Propagacao.Propagar(f, a, out var obrigadas));
        Assert.Equal(n, obrigadas);
        Assert.True(a.Completa);
        for (var i = 0; i < n; i++) Assert.Equal(Valor.Verdadeiro, a.ValorDe(i));
    }

    /// <summary>
    /// Duas unitarias que se contradizem dao conflito sem decisao nenhuma. E o
    /// caso mais simples de insatisfatibilidade e o que um resolvedor tem de
    /// pegar antes de abrir o primeiro ramo.
    /// </summary>
    [Fact]
    public void UnitariasOpostasDaoConflitoDeCara()
    {
        var f = Formula.DoDimacs(1, [1], [-1]);
        Assert.Equal(ResultadoDaPropagacao.Conflito, Propagacao.Propagar(f, new Atribuicao(1)));
        Assert.False(ForcaBruta.Satisfativel(f));
    }

    /// <summary>
    /// Propagar nao pode inventar nem perder modelo: o conjunto de modelos da
    /// formula antes e depois da propagacao e o MESMO. Conferido em mil formulas
    /// sorteadas de 8 variaveis, contra a enumeracao dos 256 pontos.
    /// </summary>
    [Fact]
    public void PropagarPreservaOsModelos()
    {
        var r = new Random(12345);
        var conferidas = 0;
        for (var caso = 0; caso < 1000; caso++)
        {
            const int n = 8;
            var clausulas = new List<Clausula>();
            var quantas = 1 + r.Next(12);
            for (var i = 0; i < quantas; i++)
            {
                var tamanho = 1 + r.Next(3);
                var literais = new Literal[tamanho];
                for (var k = 0; k < tamanho; k++) literais[k] = Literal.De(r.Next(n), r.Next(2) == 0);
                clausulas.Add(Clausula.De(literais));
            }
            var f = new Formula(n, clausulas);

            var a = new Atribuicao(n);
            if (Propagacao.Propagar(f, a) == ResultadoDaPropagacao.Conflito)
            {
                // Conflito na propagacao significa insatisfativel de verdade.
                Assert.False(ForcaBruta.Satisfativel(f));
                conferidas++;
                continue;
            }
            // Sem conflito: todo modelo da formula concorda com o que foi obrigado.
            foreach (var m in ForcaBruta.Modelos(f))
                for (var v = 0; v < n; v++)
                {
                    if (a.ValorDe(v) == Valor.Livre) continue;
                    Assert.Equal(a.ValorDe(v) == Valor.Verdadeiro, m[v]);
                }
            conferidas++;
        }
        Assert.Equal(1000, conferidas);
    }

    /// <summary>
    /// O literal puro preserva a SATISFATIBILIDADE e nao o numero de modelos.
    /// Aqui isso vira numero em vez de nota de rodape: em (x1 ou x2), a variavel
    /// x1 e pura, fixa-la deixa 2 modelos dos 3 que existem, e a resposta sim
    /// continua certa.
    /// </summary>
    [Fact]
    public void LiteralPuroGuardaARespostaENaoAContagem()
    {
        var f = Formula.DoDimacs(2, [1, 2]);
        Assert.Equal(3, ForcaBruta.ContarModelos(f));

        var puros = Propagacao.LiteraisPuros(f, new Atribuicao(2));
        Assert.Equal(2, puros.Count);

        var comPuroFixo = new Formula(2, [.. f.Clausulas, Clausula.De(puros[0])]);
        Assert.True(ForcaBruta.Satisfativel(comPuroFixo));
        Assert.Equal(2, ForcaBruta.ContarModelos(comPuroFixo));
    }

    /// <summary>
    /// Clausula ja satisfeita nao gera literal puro: a variavel dela nao precisa
    /// de mais nada. Esquecer de pular as satisfeitas faz a regra fixar valores
    /// sem motivo, e em formula insatisfativel isso pode mudar a resposta.
    /// </summary>
    [Fact]
    public void SatisfeitaNaoContaParaOPuro()
    {
        var f = Formula.DoDimacs(3, [1, 2], [-2, 3]);
        var a = new Atribuicao(3);
        a.Atribuir(Literal.De(1, true));   // x2 verdadeiro satisfaz a primeira
        var puros = Propagacao.LiteraisPuros(f, a);
        // x1 so aparecia na clausula ja satisfeita: nao e mais puro relevante.
        Assert.DoesNotContain(puros, l => l.Variavel == 0);
        Assert.Contains(puros, l => l.Variavel == 2 && l.Positivo);
    }
}
