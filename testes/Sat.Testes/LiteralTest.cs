using Conde.Sat;
using Xunit;

namespace Conde.Sat.Testes;

public class LiteralTest
{
    /// <summary>
    /// A ida e volta entre a notacao do DIMACS e o indice interno, no dominio
    /// inteiro ate mil variaveis: sao 2000 literais, todos conferidos.
    /// Representacao com duas formas e onde mora o erro de sinal trocado, e ele
    /// nao da excecao, da resposta errada.
    /// </summary>
    [Fact]
    public void IdaEVoltaDoDimacs()
    {
        for (var v = 1; v <= 1000; v++)
        {
            foreach (var dimacs in new[] { v, -v })
            {
                var l = Literal.DoDimacs(dimacs);
                Assert.Equal(dimacs, l.Dimacs);
                Assert.Equal(v - 1, l.Variavel);
                Assert.Equal(dimacs > 0, l.Positivo);
            }
        }
    }

    /// <summary>
    /// Negar e uma involucao: negar duas vezes volta ao mesmo literal, e a
    /// variavel nunca muda. Conferido em todos os indices ate 4096.
    /// </summary>
    [Fact]
    public void NegarDuasVezesVolta()
    {
        for (var i = 0; i < 4096; i++)
        {
            var l = Literal.DoIndice(i);
            Assert.Equal(l, l.Negado.Negado);
            Assert.Equal(l.Variavel, l.Negado.Variavel);
            Assert.NotEqual(l.Positivo, l.Negado.Positivo);
        }
    }

    /// <summary>
    /// O indice e uma bijecao com o par (variavel, sinal): dois literais
    /// diferentes nunca compartilham indice. Sem isso, dois literais distintos
    /// ocupariam a mesma posicao de vetor e um sobrescreveria o outro em silencio.
    /// </summary>
    [Fact]
    public void IndiceEhBijecao()
    {
        var vistos = new HashSet<int>();
        for (var v = 0; v < 500; v++)
        {
            Assert.True(vistos.Add(Literal.De(v, true).Indice));
            Assert.True(vistos.Add(Literal.De(v, false).Indice));
        }
        Assert.Equal(1000, vistos.Count);
    }

    /// <summary>O zero nao e literal no DIMACS: ele fecha clausula.</summary>
    [Fact]
    public void ZeroNaoEhLiteral()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Literal.DoDimacs(0));
    }
}
