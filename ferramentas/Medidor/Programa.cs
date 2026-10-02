using System.Diagnostics;
using System.Globalization;
using Conde.Sat;

// A ferramenta RODA para registrar numeros, e nao tem asseveracao nenhuma: os
// numeros que entram no README saem daqui, nunca da minha cabeca.

// Com InvariantGlobalization a cultura pt-BR nao existe, e ela lancaria. O
// formato vai montado na mao, que e o padrao ja adotado nos outros projetos.
var br = new NumberFormatInfo { NumberDecimalSeparator = ",", NumberGroupSeparator = ".", PercentDecimalSeparator = "," };
string N(long x) => x.ToString("N0", br);
string D(double x, int casas = 1) => x.ToString("N" + casas, br);

var comando = args.Length > 0 ? args[0] : "tudo";
switch (comando)
{
    case "transicao": Transicao(); break;
    case "aprender": Aprender(); break;
    case "pombos": Pombos(); break;
    case "propagar": Propagar(); break;
    default: Transicao(); Console.WriteLine(); Aprender(); Console.WriteLine(); Pombos(); Console.WriteLine(); Propagar(); break;
}

return;

// A transicao de fase: a fracao de instancias satisfativeis despenca em torno de
// m/n = 4,27, e a queda fica mais abrupta quanto maior o n. Esse e o motivo de
// as instancias dificeis estarem EM CIMA do ponto e nao longe dele.
void Transicao()
{
    Console.WriteLine("transicao de fase do 3-SAT sorteado (200 instancias por ponto)");
    Console.WriteLine();
    Console.WriteLine("  m/n      n=20     n=50    n=100   | conflitos medios do cdcl (n=100)");
    const int amostras = 200;
    for (var razaoDez = 30; razaoDez <= 60; razaoDez += 5)
    {
        var razao = razaoDez / 10.0;
        var linha = "  " + D(razao, 2).PadRight(7);
        var conflitos = 0.0;
        foreach (var n in new[] { 20, 50, 100 })
        {
            var m = (int)Math.Round(n * razao);
            var sim = 0;
            var somaConflitos = 0L;
            for (var s = 1; s <= amostras; s++)
            {
                var r = new Cdcl().Resolver(Geradores.TresSat(n, m, s * 7919 + n));
                if (r.Satisfativel) sim++;
                somaConflitos += r.Conflitos;
            }
            linha += (D(100.0 * sim / amostras) + "%").PadLeft(9);
            if (n == 100) conflitos = (double)somaConflitos / amostras;
        }
        Console.WriteLine(linha + "   | " + D(conflitos));
    }
}

// O que a clausula aprendida compra. A mesma instancia, com e sem aprendizado.
void Aprender()
{
    Console.WriteLine("o preco de nao aprender: 3-SAT em cima da razao critica");
    Console.WriteLine();
    Console.WriteLine("  n     dpll decisoes   cdcl decisoes   razao   cdcl aprendeu");
    foreach (var n in new[] { 20, 30, 40, 50 })
    {
        var m = (int)Math.Round(n * Geradores.RazaoCritica);
        long dpll = 0, cdcl = 0, aprendidas = 0;
        const int amostras = 40;
        for (var s = 1; s <= amostras; s++)
        {
            var f = Geradores.TresSat(n, m, s);
            dpll += new Dpll(new MaisFrequente()).Resolver(f).Decisoes;
            var motor = new Cdcl();
            cdcl += motor.Resolver(f).Decisoes;
            aprendidas += motor.Aprendidas;
        }
        var razao = cdcl == 0 ? 0 : (double)dpll / cdcl;
        Console.WriteLine($"  {n,-4}  {N(dpll / amostras),13}   {N(cdcl / amostras),13}   {D(razao),5}x   {N(aprendidas / amostras),8}");
    }
}

// Casas de pombo: facil de entender, dificil para o metodo. Haken mostrou em
// 1985 que toda prova por resolucao e exponencial, e o CDCL produz provas por
// resolucao. O tempo aqui nao e falha de implementacao, e limite do metodo.
void Pombos()
{
    Console.WriteLine("casas de pombo: insatisfativel por contagem, exponencial por resolucao");
    Console.WriteLine();
    Console.WriteLine("  casas  variaveis  clausulas   conflitos   aprendidas       ms");
    // O orcamento de conflitos nao e cautela: e a medida. A linha em que ele
    // estoura e onde o crescimento exponencial encontra a paciencia de quem roda.
    const int orcamento = 150_000;
    for (var casas = 2; casas <= 10; casas++)
    {
        var f = Geradores.Pombos(casas);
        var motor = new Cdcl(tetoDeConflitos: orcamento);
        var relogio = Stopwatch.StartNew();
        try
        {
            var r = motor.Resolver(f);
            relogio.Stop();
            if (r.Satisfativel) throw new InvalidOperationException("pombos nao cabem; algo esta errado");
            Console.WriteLine($"  {casas,-5}  {N(f.Variaveis),9}  {N(f.Quantas),9}   {N(r.Conflitos),9}   {N(motor.Aprendidas),10}  {N(relogio.ElapsedMilliseconds),7}");
        }
        catch (InvalidOperationException)
        {
            relogio.Stop();
            Console.WriteLine($"  {casas,-5}  {N(f.Variaveis),9}  {N(f.Quantas),9}   passou de {N(orcamento)} conflitos em {N(relogio.ElapsedMilliseconds)} ms");
            break;
        }
    }
}

// Quantas variaveis uma decisao arrasta. E o numero que mostra por que a
// propagacao unitaria e a regra que faz o resolvedor valer a pena.
void Propagar()
{
    Console.WriteLine("quantas variaveis cada decisao arrasta");
    Console.WriteLine();
    Console.WriteLine("  instancia                decisoes   propagacoes   por decisao");
    var casos = new (string Nome, Formula F)[]
    {
        ("3-SAT n=100 critico", Geradores.TresSat(100, (int)Math.Round(100 * Geradores.RazaoCritica), 1)),
        ("3-SAT n=200 critico", Geradores.TresSat(200, (int)Math.Round(200 * Geradores.RazaoCritica), 1)),
        ("plantada n=300", Geradores.Plantada(300, 1200, 1).Formula),
        ("pombos 7 casas", Geradores.Pombos(7)),
        ("completo 8 com 7 cores", Geradores.Coloracao(8, Geradores.Completo(8).ToList(), 7)),
    };
    foreach (var (nome, f) in casos)
    {
        var r = new Cdcl().Resolver(f);
        var por = r.Decisoes == 0 ? 0 : (double)r.Propagacoes / r.Decisoes;
        Console.WriteLine($"  {nome,-22}  {N(r.Decisoes),9}   {N(r.Propagacoes),11}   {D(por),11}");
    }
}
