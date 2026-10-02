namespace Conde.Sat;

/// <summary>
/// A resposta de um resolvedor, com o modelo quando ha um.
///
/// Satisfativel vem com testemunha, e isso nao e enfeite: a resposta "sim" de um
/// resolvedor e verificavel em tempo linear por quem nao confia nele, bastando
/// avaliar a formula no modelo. A resposta "nao" nao vem com testemunha curta, e
/// e essa assimetria que coloca SAT em NP e deixa a outra ponta em aberto.
///
/// Os numeros de decisoes, propagacoes e conflitos ficam aqui porque sao eles
/// que mostram o trabalho: duas respostas iguais com contagens muito diferentes
/// dizem que as heuristicas estao fazendo efeito.
/// </summary>
public sealed class Resultado
{
    private Resultado(bool satisfativel, bool[]? modelo, long decisoes, long propagacoes, long conflitos)
    {
        Satisfativel = satisfativel;
        Modelo = modelo;
        Decisoes = decisoes;
        Propagacoes = propagacoes;
        Conflitos = conflitos;
    }

    public bool Satisfativel { get; }

    /// <summary>A testemunha do "sim". Nulo quando a resposta e "nao".</summary>
    public bool[]? Modelo { get; }

    public long Decisoes { get; }
    public long Propagacoes { get; }
    public long Conflitos { get; }

    public static Resultado Sim(bool[] modelo, long decisoes = 0, long propagacoes = 0, long conflitos = 0)
    {
        ArgumentNullException.ThrowIfNull(modelo);
        return new Resultado(true, modelo, decisoes, propagacoes, conflitos);
    }

    public static Resultado Nao(long decisoes = 0, long propagacoes = 0, long conflitos = 0) =>
        new(false, null, decisoes, propagacoes, conflitos);

    /// <summary>
    /// Confere a propria resposta contra a formula. So o "sim" e conferivel aqui,
    /// e isso ja pega o erro mais comum: devolver um modelo montado a partir de
    /// uma atribuicao parcial, com as variaveis livres saindo como falso sem que
    /// ninguem tenha olhado se isso serve.
    /// </summary>
    public bool Confere(Formula f)
    {
        ArgumentNullException.ThrowIfNull(f);
        if (!Satisfativel) return true;
        return Modelo is not null && Avaliacao.Satisfaz(f, Modelo);
    }

    public override string ToString() =>
        (Satisfativel ? "SATISFATIVEL" : "INSATISFATIVEL") +
        $" (decisoes {Decisoes}, propagacoes {Propagacoes}, conflitos {Conflitos})";
}
