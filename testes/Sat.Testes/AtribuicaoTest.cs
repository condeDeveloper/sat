using Conde.Sat;
using Xunit;

namespace Conde.Sat.Testes;

public class AtribuicaoTest
{
    /// <summary>
    /// Desfazer ate um nivel devolve o estado EXATO daquele nivel. E a operacao
    /// mais perigosa da estrutura: um resolvedor que desfaz errado continua
    /// respondendo, so que sobre uma formula que nao e a que foi perguntada.
    ///
    /// O teste grava uma foto do estado em cada nivel, desce ate o fim e volta,
    /// conferindo foto por foto.
    /// </summary>
    [Fact]
    public void DesfazerDevolveOEstadoExato()
    {
        const int n = 30;
        var a = new Atribuicao(n);
        var fotos = new List<Valor[]>();
        for (var nivel = 0; nivel < 10; nivel++)
        {
            fotos.Add(Foto(a, n));
            a.SobeNivel();
            for (var k = 0; k < 3; k++)
            {
                var v = nivel * 3 + k;
                a.Atribuir(Literal.De(v, (v & 1) == 0));
            }
        }
        for (var nivel = 9; nivel >= 0; nivel--)
        {
            a.DesfazerAte(nivel);
            Assert.Equal(fotos[nivel], Foto(a, n));
            Assert.Equal(nivel, a.Nivel);
        }
    }

    private static Valor[] Foto(Atribuicao a, int n)
    {
        var f = new Valor[n];
        for (var v = 0; v < n; v++) f[v] = a.ValorDe(v);
        return f;
    }

    /// <summary>
    /// A trilha guarda a ordem de atribuicao, e so as variaveis atribuidas. O
    /// tamanho dela e o contador de atribuidas: se os dois divergirem, desfazer
    /// passa a deixar lixo.
    /// </summary>
    [Fact]
    public void ATrilhaEhAOrdemDeAtribuicao()
    {
        var a = new Atribuicao(10);
        int[] ordem = [4, 1, 7, 0, 9];
        foreach (var v in ordem) a.Atribuir(Literal.De(v, true));
        Assert.Equal(ordem, a.Trilha);
        Assert.Equal(ordem.Length, a.Atribuidas);
    }

    /// <summary>
    /// Reatribuir por cima seria perder a trilha, e sem trilha nao ha como
    /// desfazer. Melhor estourar.
    /// </summary>
    [Fact]
    public void NaoDaParaReatribuirPorCima()
    {
        var a = new Atribuicao(3);
        a.Atribuir(Literal.De(0, true));
        Assert.Throws<InvalidOperationException>(() => a.Atribuir(Literal.De(0, false)));
        Assert.Throws<InvalidOperationException>(() => a.Atribuir(Literal.De(0, true)));
    }

    /// <summary>
    /// O valor do literal segue o sinal: com a variavel em verdadeiro, o literal
    /// positivo vale e o negado nao. Conferido nos dois sinais e nos tres estados.
    /// </summary>
    [Fact]
    public void OSinalInverteOValor()
    {
        var a = new Atribuicao(2);
        Assert.Equal(Valor.Livre, a.ValorDoLiteral(Literal.De(0, true)));
        Assert.Equal(Valor.Livre, a.ValorDoLiteral(Literal.De(0, false)));

        a.Atribuir(Literal.De(0, true));
        Assert.Equal(Valor.Verdadeiro, a.ValorDoLiteral(Literal.De(0, true)));
        Assert.Equal(Valor.Falso, a.ValorDoLiteral(Literal.De(0, false)));

        a.Atribuir(Literal.De(1, false));
        Assert.Equal(Valor.Falso, a.ValorDoLiteral(Literal.De(1, true)));
        Assert.Equal(Valor.Verdadeiro, a.ValorDoLiteral(Literal.De(1, false)));
    }

    /// <summary>
    /// O nivel de cada variavel e o nivel em que ela foi atribuida, e volta a -1
    /// quando ela e desfeita. A analise do conflito do CDCL se apoia nisso para
    /// separar o que e do nivel corrente e o que veio de antes.
    /// </summary>
    [Fact]
    public void ONivelAcompanhaAVariavel()
    {
        var a = new Atribuicao(4);
        a.Atribuir(Literal.De(0, true));
        Assert.Equal(0, a.NivelDe(0));
        a.SobeNivel();
        a.Atribuir(Literal.De(1, true));
        a.SobeNivel();
        a.Atribuir(Literal.De(2, true));
        Assert.Equal(1, a.NivelDe(1));
        Assert.Equal(2, a.NivelDe(2));
        Assert.Equal(-1, a.NivelDe(3));

        a.DesfazerAte(1);
        Assert.Equal(-1, a.NivelDe(2));
        Assert.Equal(1, a.NivelDe(1));
        Assert.Equal(0, a.NivelDe(0));
    }

    /// <summary>
    /// Desfazer ate zero devolve a atribuicao ao estado inicial, menos o que foi
    /// atribuido no proprio nivel zero, que e verdade sem condicao.
    /// </summary>
    [Fact]
    public void NivelZeroNaoSeDesfaz()
    {
        var a = new Atribuicao(5);
        a.Atribuir(Literal.De(0, true));
        a.SobeNivel();
        a.Atribuir(Literal.De(1, true));
        a.SobeNivel();
        a.Atribuir(Literal.De(2, true));

        a.DesfazerAte(0);
        Assert.Equal(1, a.Atribuidas);
        Assert.Equal(Valor.Verdadeiro, a.ValorDe(0));
        Assert.Equal(Valor.Livre, a.ValorDe(1));
        Assert.Equal(Valor.Livre, a.ValorDe(2));
    }
}
