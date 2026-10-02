using Conde.Sat;
using Xunit;

namespace Conde.Sat.Testes;

public class ReinicioTest
{
    /// <summary>
    /// Os primeiros termos da sequencia de Luby estao publicados e nao dependem
    /// de implementacao nenhuma: 1 1 2 1 1 2 4 1 1 2 1 1 2 4 8.
    /// </summary>
    [Fact]
    public void OsPrimeirosTermosSaoOsPublicados()
    {
        int[] esperado = [1, 1, 2, 1, 1, 2, 4, 1, 1, 2, 1, 1, 2, 4, 8, 1, 1, 2, 1, 1, 2, 4, 1, 1, 2, 1, 1, 2, 4, 8, 16];
        for (var i = 0; i < esperado.Length; i++)
            Assert.Equal(esperado[i], Luby.Termo(i + 1));
    }

    /// <summary>
    /// A sequencia e feita de blocos: o termo de indice 2^k - 1 e 2^(k-1), e os
    /// 2^k - 1 primeiros termos somam exatamente k * 2^(k-1).
    ///
    /// A soma importa porque ela E o trabalho total ate o fim do bloco, que e o
    /// que o teorema de Luby limita. E foi aqui que eu me corrigi: escrevi
    /// (k-1) * 2^(k-1) + 1 de cabeca, que da 3 para k=2 quando a soma de 1, 1 e 2
    /// e 4. A identidade certa e k * 2^(k-1), conferida ate k = 16, onde sao
    /// 65.535 termos somando 524.288.
    /// </summary>
    [Fact]
    public void AEstruturaDeBlocosFecha()
    {
        for (var k = 1; k <= 16; k++)
        {
            var fim = (1 << k) - 1;
            Assert.Equal(1L << (k - 1), Luby.Termo(fim));
            long soma = 0;
            for (var i = 1; i <= fim; i++) soma += Luby.Termo(i);
            Assert.Equal((long)k * (1 << (k - 1)), soma);
        }
    }

    /// <summary>
    /// Todo termo e potencia de dois. Nao e enfeite: e consequencia direta da
    /// definicao, e serve de rede contra uma recursao escrita errado.
    /// </summary>
    [Fact]
    public void TodoTermoEhPotenciaDeDois()
    {
        for (var i = 1; i <= 5000; i++)
        {
            var t = Luby.Termo(i);
            Assert.True(t > 0 && (t & (t - 1)) == 0, $"o termo {i} deu {t}, que nao e potencia de dois");
        }
    }

    /// <summary>
    /// A sequencia e base UM. Pedir o termo zero e erro de quem chama, e e
    /// melhor estourar do que devolver um numero plausivel.
    /// </summary>
    [Fact]
    public void NaoExisteTermoZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Luby.Termo(0));
    }

    /// <summary>
    /// O limite devolvido e a unidade vezes o termo, e cresce sem limite: um
    /// cronograma que estagnasse nunca deixaria a busca ir fundo.
    /// </summary>
    [Fact]
    public void OLimiteCresceSemTeto()
    {
        var luby = new Luby(100);
        var maior = 0L;
        for (var i = 0; i < 2000; i++) maior = Math.Max(maior, luby.ProximoLimite());
        Assert.True(maior >= 100 * 256, $"o maior limite em 2000 reinicios foi {maior}");
    }

    /// <summary>
    /// Com e sem reinicio a RESPOSTA e a mesma. Reinicio e politica de busca, nao
    /// pode mudar o que e verdade. Conferido em 200 instancias contra a forca
    /// bruta, com os tres cronogramas.
    /// </summary>
    [Fact]
    public void ReiniciarNaoMudaAResposta()
    {
        for (var semente = 1; semente <= 200; semente++)
        {
            var f = Geradores.TresSat(16, 68, semente);
            var esperado = ForcaBruta.Satisfativel(f);
            Assert.Equal(esperado, new Cdcl(new SemReinicio()).Resolver(f).Satisfativel);
            Assert.Equal(esperado, new Cdcl(new Luby(10)).Resolver(f).Satisfativel);
            Assert.Equal(esperado, new Cdcl(new Geometrico(10, 1.3)).Resolver(f).Satisfativel);
        }
    }
}
