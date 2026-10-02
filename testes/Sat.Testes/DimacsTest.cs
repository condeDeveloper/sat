using Conde.Sat;
using Xunit;

namespace Conde.Sat.Testes;

public class DimacsTest
{
    /// <summary>
    /// Ida e volta pelo formato em 500 formulas sorteadas: escrever e ler de
    /// volta tem de dar a MESMA formula, conferida pelo numero de modelos e nao
    /// pela igualdade de texto. Comparar texto so diria que o escritor e
    /// determinista; comparar modelos diz que o significado sobreviveu.
    /// </summary>
    [Fact]
    public void IdaEVoltaPreservaOSignificado()
    {
        for (var semente = 1; semente <= 500; semente++)
        {
            var f = Geradores.TresSat(10, 20, semente);
            var devolta = Dimacs.Ler(Dimacs.Escrever(f));
            Assert.Equal(f.Variaveis, devolta.Variaveis);
            Assert.Equal(f.Quantas, devolta.Quantas);
            Assert.Equal(ForcaBruta.ContarModelos(f), ForcaBruta.ContarModelos(devolta));
        }
    }

    /// <summary>
    /// O zero TERMINA a clausula, nao separa linha. Uma clausula pode atravessar
    /// varias linhas, e arquivo de competicao faz isso o tempo todo. Quem quebra
    /// por linha le tres clausulas onde ha uma.
    /// </summary>
    [Fact]
    public void ClausulaPodeAtravessarLinhas()
    {
        var f = Dimacs.Ler("p cnf 3 1\n1\n2\n3\n0\n");
        Assert.Equal(1, f.Quantas);
        Assert.Equal(3, f[0].Tamanho);
    }

    /// <summary>
    /// A linha com um zero sozinho e a clausula VAZIA, que torna a formula
    /// insatisfativel. Tratar zero como separador em vez de terminador faz esse
    /// caso sumir, e a formula passa de insatisfativel a satisfativel em silencio.
    /// </summary>
    [Fact]
    public void ZeroSozinhoEhAClausulaVazia()
    {
        var f = Dimacs.Ler("p cnf 2 2\n1 2 0\n0\n");
        Assert.Equal(2, f.Quantas);
        Assert.True(f[1].EhVazia);
        Assert.False(ForcaBruta.Satisfativel(f));
        Assert.False(new Cdcl().Resolver(f).Satisfativel);
    }

    /// <summary>
    /// O cabecalho pode mentir no numero de clausulas, e arquivos reais mentem.
    /// O que vale e o que esta escrito depois dele.
    /// </summary>
    [Fact]
    public void CabecalhoMentirosoNaoManda()
    {
        var f = Dimacs.Ler("p cnf 2 99\n1 0\n-2 0\n");
        Assert.Equal(2, f.Quantas);
        Assert.Equal(2, f.Variaveis);
    }

    /// <summary>Linhas que comecam com 'c' sao comentario e somem.</summary>
    [Fact]
    public void ComentariosSaoIgnorados()
    {
        var f = Dimacs.Ler("c isto e um comentario\np cnf 1 1\nc outro\n1 0\n");
        Assert.Equal(1, f.Quantas);
    }

    /// <summary>
    /// O '%' TERMINA o arquivo, e isso nao e detalhe: os benchmarks do SATLIB
    /// fecham com uma linha '%' e um '0' embaixo. Tratando o '%' como comentario,
    /// aquele zero vira a clausula vazia e TODA instancia do SATLIB passa a ser
    /// insatisfativel. O resolvedor responde rapido, com confianca, e errado.
    ///
    /// Eu escrevi o leitor tratando '%' como comentario, e foi este teste que
    /// mostrou: a formula vinha com duas clausulas em vez de uma.
    /// </summary>
    [Fact]
    public void OPorcentoFechaOArquivoEmVezDeSerComentario()
    {
        const string comoOSatlibTermina = "p cnf 2 1\n1 2 0\n%\n0\n";
        var f = Dimacs.Ler(comoOSatlibTermina);
        Assert.Equal(1, f.Quantas);
        Assert.False(f[0].EhVazia);
        Assert.True(ForcaBruta.Satisfativel(f));
        Assert.True(new Cdcl().Resolver(f).Satisfativel);
    }

    /// <summary>
    /// O numero de variaveis vem do cabecalho e nao do maior literal: uma formula
    /// que declara 10 e usa 2 tem 2^10 modelos possiveis, nao 2^2. Deduzir
    /// mudaria a contagem sem mudar a resposta de sim ou nao, que e o tipo de
    /// erro que passa despercebido.
    /// </summary>
    [Fact]
    public void AsVariaveisVemDoCabecalho()
    {
        var f = Dimacs.Ler("p cnf 10 1\n1 0\n");
        Assert.Equal(10, f.Variaveis);
        Assert.Equal(512, ForcaBruta.ContarModelos(f));
    }

    /// <summary>Clausula antes do cabecalho e arquivo torto, e nao passa calado.</summary>
    [Fact]
    public void SemCabecalhoNaoLe()
    {
        Assert.Throws<FormatException>(() => Dimacs.Ler("1 2 0\n"));
    }
}
