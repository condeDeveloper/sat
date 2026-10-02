# sat

Resolvedor SAT do zero em C# e .NET 8: DPLL de 1962 e CDCL com aprendizado de
cláusula, literais vigiados e reinício de Luby. O juiz é a força bruta, e a
comparação é no domínio inteiro.

```
$ dotnet medidor.dll pombos

  casas  variaveis  clausulas   conflitos   aprendidas       ms
  2              6          9           2            1        0
  5             30         81         151          150        0
  7             56        204       6.156        6.155      225
  8             72        297      66.413       66.412   58.290
  9             90        415   passou de 150.000 conflitos em 657.418 ms
```

## O juiz não conhece algoritmo nenhum

Um resolvedor errado é um resolvedor que **poda demais**: ele corta um ramo que
ainda tinha solução e responde "insatisfatível" com a mesma confiança com que
responderia a verdade. Um juiz que também poda não pega esse erro, porque pode
estar podando pelo mesmo motivo.

Então o juiz aqui é a força bruta: enumera as 2ⁿ atribuições e responde pela
definição. Sem propagação, sem heurística, sem corte. Ela é lenta por projeto, e
é essa lentidão que a torna confiável.

A comparação não é por amostra. Sobre 3 variáveis existem exatamente **26
cláusulas** não tautológicas de até 3 literais (6 de um literal, 12 de dois, 8 de
três), e o teste monta **toda** combinação de até 4 delas: **17.901 fórmulas**.
Cada uma passa pela força bruta, por três configurações de DPLL e pelo CDCL, e os
cinco têm de concordar — tanto na resposta quanto no modelo devolvido.

Isso importa porque instância sorteada quase nunca cai nos casos que quebram:
duas unitárias que se contradizem, literal puro em fórmula insatisfatível,
cláusula que vira vazia no meio da propagação, fórmula sem cláusula nenhuma. São
eventos raros no sorteio e são **todos** os casos aqui.

Fora do domínio exaustivo ficam as famílias de resposta conhecida por teorema:
casas de pombo (insatisfatível por contagem), grafo completo de n vértices
(precisa de exatamente n cores) e fórmula plantada (satisfatível por construção,
com o modelo escondido servindo de testemunha).

## Contar modelos é mais duro que responder sim ou não

SAT está em NP; contar quantas atribuições servem é completo para #P, uma classe
acima. E a diferença aparece no código, não só na teoria: a regra do **literal
puro**, que o DPLL usa à vontade, não vale no contador. Ela preserva a
satisfatibilidade e não o número de modelos, porque fixar o puro joga fora as
atribuições em que ele tem o outro valor e alguma outra cláusula segura a
fórmula.

Em `(x1 ou x2)` existem 3 modelos. Fixar x1, que é puro, deixa 2. A resposta "sim"
continua certa e a contagem passa a estar errada.

## A transição de fase, medida

```
$ dotnet medidor.dll transicao

  m/n      n=20     n=50    n=100   | conflitos medios do cdcl (n=100)
  3,00    100,0%   100,0%   100,0%  |     5,7
  3,50     96,5%   100,0%   100,0%  |    29,2
  4,00     82,0%    86,5%    94,0%  |   205,4
  4,50     54,5%    29,0%    19,0%  |   419,5
  5,00     30,0%     4,5%     0,5%  |   293,5
  5,50     14,0%     0,5%     0,0%  |   217,4
  6,00      5,5%     0,0%     0,0%  |   170,5
```

Duas coisas que a tabela mostra e que uma frase não mostraria:

**A transição fica mais abrupta com n.** Em m/n = 4,5, com 20 variáveis ainda são
54,5% satisfatíveis; com 100 variáveis caem para 19,0%. Na mesma razão. É esse
afunilamento que faz a literatura falar de 4,27 como um ponto e não como uma
faixa: o valor é o limite quando n cresce, e com n pequeno a travessia é suave.

**O pico de dificuldade não está onde a resposta vira.** Os conflitos sobem até
m/n = 4,5 e depois **caem**, mesmo com a fórmula continuando insatisfatível. Longe
do ponto, dos dois lados, a resposta aparece rápido: abaixo há solução demais para
tropeçar numa, acima há contradição demais para não achar uma logo. O caro é em
cima do ponto.

## O preço de não aprender com o conflito

```
$ dotnet medidor.dll aprender

  n     dpll decisoes   cdcl decisoes   razao   cdcl aprendeu
  20               14              11     1,2x          6
  30               26              22     1,2x         15
  40               58              37     1,5x         27
  50              121              72     1,7x         57
```

O DPLL desfaz a última decisão e tenta o outro sinal, o que repete o mesmo erro em
outro lugar da árvore quantas vezes for preciso. O CDCL pergunta **por que** deu
conflito, monta uma cláusula que proíbe aquela combinação, e volta direto para o
nível em que essa cláusula passa a mandar.

A razão cresce com n, que é o ponto: em instância pequena o aprendizado quase não
paga, e é em instância grande que ele vira a diferença entre responder e não
responder.

## Casas de pombo: fácil de entender, impossível para o método

n+1 pombos não cabem em n casas, e o argumento é de contagem, acessível a uma
criança. Para o resolvedor é outra história: Haken mostrou em 1985 que **toda**
prova por resolução dessa fórmula tem tamanho exponencial, e o CDCL produz provas
por resolução.

A tabela no topo não é uma implementação ruim, é o teorema aparecendo. Os
conflitos vão 2, 7, 28, 151, 829, 6.156, 66.413 — cada casa a mais multiplica por
uns dez. De 7 para 8 casas o tempo sai de 225 ms para 58 segundos, e com 9 casas
o orçamento de 150 mil conflitos estoura depois de 11 minutos. São 90 variáveis.

## Quantas variáveis cada decisão arrasta

```
$ dotnet medidor.dll propagar

  instancia                decisoes   propagacoes   por decisao
  3-SAT n=100 critico           646        16.000          24,8
  3-SAT n=200 critico        17.832       649.838          36,4
  plantada n=300              2.265        94.795          41,9
  pombos 7 casas              8.097        81.792          10,1
  completo 8 com 7 cores      7.843        92.510          11,8
```

Este é o número que explica por que a propagação unitária é a regra que faz um
resolvedor valer a pena. Numa instância de 200 variáveis, **cada decisão arrasta
36 variáveis de graça**: o literal obrigado não é uma escolha, ou ele vale ou a
cláusula morre, e atribuí-lo não perde solução nenhuma. Uma árvore de 2²⁰⁰ folhas
vira uma de 2¹⁸ decisões reais.

E a tabela também mostra o contrário: nas casas de pombo são **10,1**, a menor
razão da lista. A fórmula é toda de cláusulas curtas e simétricas, e quase nada é
forçado. É a mesma simetria que torna a prova por resolução exponencial: o
resolvedor precisa redescobrir o mesmo argumento uma vez para cada permutação de
pombos.

## As três coisas que as medidas me corrigiram

- **O `%` do DIMACS não é comentário, é o fim do arquivo.** Eu o tratei como
  comentário. Os benchmarks do SATLIB terminam com uma linha `%` e um `0` logo
  abaixo, e com isso aquele zero virava a **cláusula vazia**: toda instância do
  SATLIB passaria a ser insatisfatível, e o resolvedor responderia rápido, com
  confiança, e errado. Foi um teste que mostrou, porque a fórmula vinha com duas
  cláusulas onde havia uma.

- **A vigia precisa se mover.** Montei os dois literais vigiados nas posições 0 e
  1 e nunca os movi. Com a vigia parada a propagação perde cláusulas unitárias, o
  grafo de implicação nasce com buracos, e a análise do conflito cai numa variável
  que não tem razão nem é decisão. O defeito **não aparece em fórmula pequena**:
  as 17.901 do domínio exaustivo passavam, e casas de pombo com cinco casas
  quebrava. Testar só o pequeno teria deixado isso passar.

- **A soma dos blocos de Luby é k·2^(k−1).** Eu escrevi (k−1)·2^(k−1) + 1 de
  cabeça. Para k = 2 isso dá 3, e a soma de 1, 1 e 2 é 4. A identidade certa está
  conferida até k = 16, onde são 65.535 termos somando 524.288.

E um quarto, menor: duas cláusulas unitárias opostas na entrada eram **ignoradas**
em vez de encerrar a fórmula, porque unitária não recebe vigia e a segunda achava
a variável já atribuída. O caso mais simples de insatisfatibilidade que existe.

## As peças

| arquivo | o que faz |
|---|---|
| `Literal` | variável com sinal; negar é um XOR, não um desvio |
| `Clausula` | normalizada; tautologia numa passada só, porque x e ~x são vizinhos |
| `Formula` | CNF com as variáveis **declaradas**, não deduzidas do maior literal |
| `Atribuicao` | parcial, com trilha; desfazer custa o que foi desfeito, não o total |
| `Avaliacao` | os quatro estados de uma cláusula, que são o laço principal do DPLL |
| `ForcaBruta` | o juiz |
| `Propagacao` | unitária e literal puro |
| `Heuristica` | primeira livre, mais frequente e VSIDS com decaimento |
| `Dpll` | 1962 |
| `Cdcl` | 1UIP, volta não cronológica, literais vigiados |
| `Reinicio` | Luby e geométrico |
| `Dimacs` | o formato e as três armadilhas dele |
| `Geradores` | pombos, coloração, 3-SAT crítico, fórmula plantada |
| `Contador` | #SAT |

São 63 testes.

## Como rodar

```
dotnet test testes/Sat.Testes/Sat.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- transicao
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- aprender
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- pombos
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- propagar
```

## Licença

MIT.
