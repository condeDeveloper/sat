using System.Text;

namespace Conde.Sat;

/// <summary>
/// O formato DIMACS CNF, que e o que todo resolvedor e toda competicao usam
/// desde o Desafio DIMACS de 1992.
///
/// O formato e de uma simplicidade que engana: `p cnf V C` no cabecalho, depois
/// uma clausula por linha terminada em zero. As armadilhas estao nas bordas, e
/// sao todas reais:
///
/// - a clausula pode atravessar varias linhas, e o que fecha e o zero, nao a
///   quebra de linha;
/// - o cabecalho pode mentir no numero de clausulas, e arquivos de competicao
///   mentem mesmo; o que vale e o que esta escrito;
/// - a linha `0` sozinha e a clausula VAZIA, que torna a formula insatisfativel.
///   Quem trata zero como separador e nao como terminador perde isso em silencio.
/// </summary>
public static class Dimacs
{
    public static Formula Ler(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        var variaveis = -1;
        var clausulas = new List<Clausula>();
        var atual = new List<int>();
        var comecou = false;

        foreach (var linhaBruta in texto.Split('\n'))
        {
            var linha = linhaBruta.Trim();
            if (linha.Length == 0) continue;
            if (linha[0] == 'c') continue;
            // O '%' TERMINA o arquivo. Os benchmarks do SATLIB fecham com uma
            // linha '%' e um '0' embaixo; ler esse zero como clausula vazia
            // tornaria TODA instancia do SATLIB insatisfativel, e o resolvedor
            // responderia rapido e errado em cima de um arquivo valido.
            if (linha[0] == '%') break;
            if (linha[0] == 'p')
            {
                var partes = linha.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (partes.Length < 4 || partes[1] != "cnf")
                    throw new FormatException($"cabecalho invalido: {linha}");
                variaveis = int.Parse(partes[2]);
                comecou = true;
                continue;
            }
            if (!comecou) throw new FormatException("clausula antes do cabecalho 'p cnf'");
            foreach (var t in linha.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                var n = int.Parse(t);
                if (n == 0) { clausulas.Add(Clausula.De(Array.ConvertAll(atual.ToArray(), Literal.DoDimacs))); atual.Clear(); }
                else atual.Add(n);
            }
        }
        if (variaveis < 0) throw new FormatException("faltou o cabecalho 'p cnf'");
        // Literais soltos no fim, sem o zero: aceita, mas e arquivo torto.
        if (atual.Count > 0) clausulas.Add(Clausula.De(Array.ConvertAll(atual.ToArray(), Literal.DoDimacs)));
        return new Formula(variaveis, clausulas);
    }

    public static string Escrever(Formula f)
    {
        ArgumentNullException.ThrowIfNull(f);
        var sb = new StringBuilder();
        sb.Append("p cnf ").Append(f.Variaveis).Append(' ').Append(f.Quantas).Append('\n');
        foreach (var c in f.Clausulas)
        {
            foreach (var l in c.Literais) sb.Append(l.Dimacs).Append(' ');
            sb.Append("0\n");
        }
        return sb.ToString();
    }
}
