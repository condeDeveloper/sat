namespace Conde.Sat;

/// <summary>
/// CDCL: busca dirigida por conflito, de Marques-Silva e Sakallah (1996) e
/// Bayardo e Schrag (1997). E o que roda em producao hoje.
///
/// A diferenca para o DPLL esta inteira no que acontece NO CONFLITO. O DPLL
/// desfaz a ultima decisao e tenta o outro sinal, o que repete o mesmo erro em
/// outro lugar da arvore quantas vezes for preciso. O CDCL pergunta POR QUE deu
/// conflito, monta uma clausula nova que proibe aquela combinacao, e volta direto
/// para o nivel em que essa clausula passa a mandar.
///
/// As quatro pecas:
///
/// - **Razao.** Toda variavel propagada guarda a clausula que a obrigou. Isso
///   forma o grafo de implicacao, e e nele que a analise anda. Decisao nao tem
///   razao, e e esse buraco no grafo que termina a analise.
/// - **Primeiro ponto de implicacao unica (1UIP).** Caminhando de tras para
///   frente pelo nivel do conflito, para-se quando sobra UMA variavel daquele
///   nivel. Todo caminho da decisao ate o conflito passa por ela.
/// - **Volta nao cronologica.** Em vez de um nivel, volta-se para o segundo maior
///   nivel da clausula aprendida, onde ela fica unitaria e propaga sozinha.
/// - **Literais vigiados.** Cada clausula observa so DOIS literais. Enquanto um
///   dos dois nao for falso, a clausula nao pode ser unitaria nem conflitante.
///   A vigia PRECISA se mover: deixar as duas paradas nas posicoes iniciais faz a
///   propagacao perder clausulas unitarias, o grafo de implicacao nascer com
///   buracos e a analise cair numa variavel sem razao. Foi exatamente esse o
///   defeito da primeira versao daqui, e ele nao aparece em formula pequena:
///   as 17.901 do dominio exaustivo passavam, e casas de pombo com cinco casas
///   quebrava.
/// </summary>
public sealed class Cdcl
{
    private readonly IReinicio _reinicio;
    private readonly double _decaimento;
    private readonly int _tetoDeConflitos;

    // As clausulas viram vetores mutaveis: a vigia se move trocando literais de
    // posicao, e as duas vigiadas ficam sempre nas posicoes 0 e 1.
    private readonly List<Literal[]> _clausulas = [];
    private List<int>[] _vigias = [];
    private Formula _formula = Formula.Verdadeira(0);
    private Atribuicao _a = new(0);
    private int[] _razao = [];
    private Vsids _vsids = new(0);
    private bool[] _visto = [];
    private long _decisoes, _propagacoes, _conflitos;
    private int _lido;

    private const int SemRazao = -1;

    public Cdcl(IReinicio? reinicio = null, double decaimento = 0.95, int tetoDeConflitos = 20_000_000)
    {
        _reinicio = reinicio ?? new Luby();
        _decaimento = decaimento;
        _tetoDeConflitos = tetoDeConflitos;
    }

    /// <summary>Quantas clausulas foram aprendidas na ultima execucao.</summary>
    public int Aprendidas { get; private set; }

    public Resultado Resolver(Formula f)
    {
        ArgumentNullException.ThrowIfNull(f);

        // A clausula vazia encerra antes de preparar: ela nao tem literal para
        // vigiar, e nenhuma atribuicao a salva.
        foreach (var c in f.Clausulas) if (c.EhVazia) return Resultado.Nao();

        // Duas unitarias opostas na entrada ja encerram. Elas nao tem vigia, e
        // sem esta conferencia a segunda era simplesmente ignorada por achar a
        // variavel ja atribuida, e o resolvedor respondia que sim.
        if (!Preparar(f)) return Resultado.Nao();
        if (Propagar() >= 0) return Resultado.Nao(_decisoes, _propagacoes, _conflitos);

        var limite = _reinicio.ProximoLimite();
        var desdeOReinicio = 0L;

        while (true)
        {
            var escolha = _vsids.Proxima(_formula, _a);
            if (escolha is null) return Resultado.Sim(_a.ComoModelo(), _decisoes, _propagacoes, _conflitos);

            _a.SobeNivel();
            _decisoes++;
            // Decisao nao tem razao, e limpar importa: sem isso a variavel
            // herdaria a clausula que a propagou num ramo ja desfeito, e a
            // analise andaria por um grafo que nao existe mais.
            _razao[escolha.Value.Variavel] = SemRazao;
            _a.Atribuir(escolha.Value);

            var conflito = Propagar();
            while (conflito >= 0)
            {
                _conflitos++;
                desdeOReinicio++;
                if (_conflitos > _tetoDeConflitos)
                    throw new InvalidOperationException($"passou de {_tetoDeConflitos} conflitos");
                if (_a.Nivel == 0) return Resultado.Nao(_decisoes, _propagacoes, _conflitos);

                var (aprendida, destino) = Analisar(conflito);
                _vsids.Decair();
                _a.DesfazerAte(destino);
                _lido = _a.Trilha.Count;
                Aprender(aprendida);
                conflito = Propagar();
            }

            if (desdeOReinicio >= limite)
            {
                _a.DesfazerAte(0);
                _lido = _a.Trilha.Count;
                desdeOReinicio = 0;
                limite = _reinicio.ProximoLimite();
                if (Propagar() >= 0) return Resultado.Nao(_decisoes, _propagacoes, _conflitos);
            }
        }
    }

    /// <summary>Monta o estado. Devolve falso quando a entrada ja se contradiz.</summary>
    private bool Preparar(Formula f)
    {
        _formula = f;
        _clausulas.Clear();
        _a = new Atribuicao(f.Variaveis);
        _razao = new int[f.Variaveis];
        Array.Fill(_razao, SemRazao);
        // Alocado uma vez: um vetor por conflito, com milhoes de conflitos,
        // passa a ser a maior parte do trabalho.
        _visto = new bool[f.Variaveis];
        _vsids = new Vsids(f.Variaveis, _decaimento);
        _decisoes = _propagacoes = _conflitos = 0;
        Aprendidas = 0;
        _lido = 0;
        _vigias = new List<int>[f.Variaveis * 2];
        for (var i = 0; i < _vigias.Length; i++) _vigias[i] = [];

        foreach (var c in f.Clausulas)
        {
            var literais = c.Literais.ToArray();
            var i = _clausulas.Count;
            _clausulas.Add(literais);
            if (literais.Length == 1)
            {
                // Unitaria da entrada: vale no nivel zero e nao precisa de vigia.
                var valor = _a.ValorDoLiteral(literais[0]);
                if (valor == Valor.Falso) return false;   // ja contradiz outra unitaria
                if (valor == Valor.Livre)
                {
                    _razao[literais[0].Variavel] = i;
                    _a.Atribuir(literais[0]);
                }
                continue;
            }
            Vigiar(i);
        }
        return true;
    }

    /// <summary>
    /// Registra as posicoes 0 e 1 da clausula nas listas de vigia. A clausula
    /// que vigia o literal L fica na lista indexada por L: assim, quando L vira
    /// VERDADEIRO, a lista de ~L traz exatamente as clausulas em que ~L acabou
    /// de ficar falso.
    /// </summary>
    private void Vigiar(int i)
    {
        var c = _clausulas[i];
        _vigias[c[0].Indice].Add(i);
        _vigias[c[1].Indice].Add(i);
    }

    private void Aprender(Literal[] aprendida)
    {
        var i = _clausulas.Count;
        _clausulas.Add(aprendida);
        Aprendidas++;
        if (aprendida.Length == 1)
        {
            // Unitaria aprendida: vale no nivel zero para sempre.
            if (_a.ValorDoLiteral(aprendida[0]) == Valor.Livre)
            {
                _razao[aprendida[0].Variavel] = i;
                _a.Atribuir(aprendida[0]);
                _propagacoes++;
            }
            return;
        }
        Vigiar(i);
        // Assertiva por construcao: a posicao 0 e o unico literal livre no nivel
        // de destino, entao ela propaga aqui mesmo.
        if (_a.ValorDoLiteral(aprendida[0]) == Valor.Livre)
        {
            _razao[aprendida[0].Variavel] = i;
            _a.Atribuir(aprendida[0]);
            _propagacoes++;
        }
    }

    /// <summary>
    /// Propaga ate estabilizar. Devolve o indice da clausula do conflito, ou -1.
    ///
    /// So visita as clausulas que vigiam o literal que acabou de ficar falso, e
    /// para cada uma tenta MOVER a vigia para outro literal que ainda nao seja
    /// falso. So quando nao ha para onde mover e que a clausula e unitaria ou
    /// conflitante.
    /// </summary>
    private int Propagar()
    {
        while (_lido < _a.Trilha.Count)
        {
            var v = _a.Trilha[_lido++];
            // O literal que acabou de virar verdadeiro.
            var verdadeiro = Literal.De(v, _a.ValorDe(v) == Valor.Verdadeiro);
            var lista = _vigias[verdadeiro.Negado.Indice];

            for (var k = lista.Count - 1; k >= 0; k--)
            {
                var i = lista[k];
                var c = _clausulas[i];

                // O literal que ficou falso precisa estar na posicao 1.
                if (c[0] == verdadeiro.Negado) (c[0], c[1]) = (c[1], c[0]);

                // A outra vigia ja satisfaz a clausula: nao ha o que fazer.
                if (_a.ValorDoLiteral(c[0]) == Valor.Verdadeiro) continue;

                // Procura um literal que ainda nao seja falso para vigiar no
                // lugar. Achou, a clausula sai desta lista e entra na de la.
                var mudou = false;
                for (var p = 2; p < c.Length; p++)
                {
                    if (_a.ValorDoLiteral(c[p]) == Valor.Falso) continue;
                    (c[1], c[p]) = (c[p], c[1]);
                    lista.RemoveAt(k);
                    _vigias[c[1].Indice].Add(i);
                    mudou = true;
                    break;
                }
                if (mudou) continue;

                // Nao ha para onde mover: ou a posicao 0 e obrigada, ou morreu.
                if (_a.ValorDoLiteral(c[0]) == Valor.Falso) return i;
                _razao[c[0].Variavel] = i;
                _a.Atribuir(c[0]);
                _propagacoes++;
            }
        }
        return -1;
    }

    /// <summary>
    /// Analise do conflito ate o primeiro ponto de implicacao unica, e o nivel
    /// para onde voltar.
    ///
    /// Anda a trilha de tras para frente marcando as variaveis do nivel corrente
    /// e abrindo a razao de cada uma, ate sobrar uma so. O literal dela, negado,
    /// abre a clausula aprendida; o resto vem dos niveis anteriores.
    /// </summary>
    private (Literal[] Aprendida, int Destino) Analisar(int conflito)
    {
        var nivel = _a.Nivel;
        var visto = _visto;
        Array.Clear(visto);
        var deFora = new List<Literal>();
        var porResolver = 0;
        var i = _a.Trilha.Count - 1;
        var atual = conflito;
        var expandido = -1;
        Literal pivo;

        while (true)
        {
            foreach (var l in _clausulas[atual])
            {
                var v = l.Variavel;
                // A variavel expandida nesta rodada e a conclusao da razao que
                // acabou de ser aberta: ela ja esta explicada.
                if (v == expandido || visto[v]) continue;
                // Nivel zero e verdade sem condicao: nao entra na explicacao.
                if (_a.NivelDe(v) <= 0) continue;
                visto[v] = true;
                _vsids.Culpou(v);
                if (_a.NivelDe(v) == nivel) porResolver++;
                else deFora.Add(l);
            }

            while (i >= 0 && !visto[_a.Trilha[i]]) i--;
            var vp = _a.Trilha[i];
            porResolver--;
            if (porResolver == 0)
            {
                pivo = Literal.De(vp, _a.ValorDe(vp) != Valor.Verdadeiro);
                break;
            }
            // Com mais de uma variavel do nivel em jogo, vp nao e a decisao: foi
            // propagada, e portanto tem razao.
            atual = _razao[vp];
            if (atual == SemRazao) throw new InvalidOperationException($"x{vp} sem razao no meio da analise");
            expandido = vp;
            i--;
        }

        // O assertivo fica na posicao 0: e ele que vai propagar depois da volta.
        var aprendida = new Literal[deFora.Count + 1];
        aprendida[0] = pivo;
        for (var k = 0; k < deFora.Count; k++) aprendida[k + 1] = deFora[k];

        var destino = 0;
        foreach (var l in deFora)
        {
            var n = _a.NivelDe(l.Variavel);
            if (n > destino) destino = n;
        }
        return (aprendida, destino);
    }
}
