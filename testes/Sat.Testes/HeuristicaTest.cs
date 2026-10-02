using Conde.Sat;
using Xunit;

namespace Conde.Sat.Testes;

public class HeuristicaTest
{
    /// <summary>
    /// Trocar a heuristica nao pode mudar a RESPOSTA, so o caminho. Conferido em
    /// 300 instancias contra a forca bruta, com as tres.
    /// </summary>
    [Fact]
    public void AHeuristicaMudaOCaminhoENaoARresposta()
    {
        for (var semente = 1; semente <= 300; semente++)
        {
            var f = Geradores.TresSat(14, 60, semente);
            var esperado = ForcaBruta.Satisfativel(f);
            Assert.Equal(esperado, new Dpll(new PrimeiraLivre()).Resolver(f).Satisfativel);
            Assert.Equal(esperado, new Dpll(new MaisFrequente()).Resolver(f).Satisfativel);
            Assert.Equal(esperado, new Dpll(new Vsids(f.Variaveis)).Resolver(f).Satisfativel);
        }
    }

    /// <summary>
    /// O decaimento do VSIDS nao esquece o passado, ele da peso ao RECENTE. Aqui
    /// isso vira numero: a variavel culpada uma vez agora passa na frente da que
    /// foi culpada vinte vezes ha cem rodadas.
    /// </summary>
    [Fact]
    public void ODecaimentoFazORecenteValerMais()
    {
        var v = new Vsids(2, decaimento: 0.95);
        for (var i = 0; i < 20; i++) { v.Culpou(0); v.Decair(); }
        for (var i = 0; i < 100; i++) v.Decair();
        v.Culpou(1);

        Assert.True(v.NotaDe(1) > v.NotaDe(0),
            $"a culpa recente deu {v.NotaDe(1)} e as vinte antigas deram {v.NotaDe(0)}");
    }

    /// <summary>
    /// Sem decaimento a nota vira um histograma do passado: a variavel com mais
    /// culpas acumuladas ganha sempre, mesmo que nao participe de um conflito ha
    /// muito tempo. E a mesma sequencia do teste anterior, com decaimento quase
    /// nulo, e o resultado se inverte.
    /// </summary>
    [Fact]
    public void SemDecaimentoOPassadoManda()
    {
        var v = new Vsids(2, decaimento: 0.999999);
        for (var i = 0; i < 20; i++) { v.Culpou(0); v.Decair(); }
        for (var i = 0; i < 100; i++) v.Decair();
        v.Culpou(1);

        Assert.True(v.NotaDe(0) > v.NotaDe(1),
            $"sem decaimento o acumulado deu {v.NotaDe(0)} e a culpa recente {v.NotaDe(1)}");
    }

    /// <summary>
    /// A nota so sobe para quem foi culpado, e nunca para quem nao foi. Parece
    /// obvio e e o que separa VSIDS de uma contagem de ocorrencias na formula.
    /// </summary>
    [Fact]
    public void SoSobeAHotaDeQuemFoiCulpado()
    {
        var v = new Vsids(5);
        v.Culpou(2);
        v.Culpou(2);
        v.Culpou(4);
        Assert.True(v.NotaDe(2) > v.NotaDe(4));
        Assert.Equal(0, v.NotaDe(0));
        Assert.Equal(0, v.NotaDe(1));
        Assert.Equal(0, v.NotaDe(3));
    }

    /// <summary>
    /// A escolha nunca devolve variavel ja atribuida, e devolve nulo quando nao
    /// sobra nenhuma. Devolver uma atribuida faria o resolvedor estourar na
    /// atribuicao em vez de terminar.
    /// </summary>
    [Fact]
    public void NuncaEscolheVariavelJaAtribuida()
    {
        var f = Geradores.TresSat(10, 30, 1);
        var a = new Atribuicao(10);
        IHeuristica[] todas = [new PrimeiraLivre(), new MaisFrequente(), new Vsids(10)];

        foreach (var h in todas)
        {
            var copia = new Atribuicao(10);
            for (var passo = 0; passo < 10; passo++)
            {
                var escolha = h.Proxima(f, copia);
                Assert.NotNull(escolha);
                Assert.Equal(Valor.Livre, copia.ValorDe(escolha!.Value.Variavel));
                copia.SobeNivel();
                copia.Atribuir(escolha.Value);
            }
            Assert.Null(h.Proxima(f, copia));
        }
        Assert.Equal(0, a.Atribuidas);
    }

    /// <summary>
    /// O decaimento precisa ficar entre zero e um: fora disso a nota ou explode
    /// ou congela, e nos dois casos a heuristica para de guiar.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-0.5)]
    [InlineData(1.5)]
    public void ODecaimentoTemFaixa(double decaimento)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Vsids(4, decaimento));
    }
}
