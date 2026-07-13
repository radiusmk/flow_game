# Explicacao do codigo do Flow Game

Este projeto implementa um jogo desktop 2D para Windows, feito em C# com WPF. O objetivo do jogo e ligar pares de pontos da mesma cor em um tabuleiro, usando linhas ortogonais, ate preencher todo o tabuleiro.

## Estrutura geral

A solucao tem tres projetos principais:

- `FlowGame.Core`: contem as regras do jogo, os modelos do tabuleiro e o gerador de puzzles.
- `FlowGame.Wpf`: contem a interface grafica Windows.
- `FlowGame.Tests`: contem testes simples, executados por console, para validar regras e geracao.

Essa separacao deixa a regra do jogo independente da interface. A tela WPF desenha e captura eventos do mouse, mas as decisoes de validade de movimento ficam em `FlowGame.Core`.

## Modelos principais

`CellPosition` representa uma celula do tabuleiro usando linha e coluna. Ele tambem calcula distancia Manhattan, usada para garantir que o movimento seja sempre entre celulas vizinhas, sem diagonais.

`FlowPair` representa um par de pontos da mesma cor. Ele guarda:

- `Id`: identificador da cor/par;
- `ColorHex`: cor usada na tela;
- `Start` e `End`: as duas bolinhas que devem ser conectadas.

`PuzzleDefinition` representa um puzzle completo. Ele guarda o nivel, tamanho do tabuleiro, lista de pares e tambem `SolutionPaths`, que sao os caminhos validos gerados internamente. A tela normal mostra apenas os pontos iniciais e finais; a solucao fica disponivel para dicas e para a janela de diagnostico.

`GameSettings` guarda progresso local: nivel selecionado, ultimo nivel liberado, puzzles resolvidos, score e creditos de dica.

## Regras do tabuleiro

A classe mais importante das regras e `PlayerBoard`. Ela controla o estado atual do jogador:

- `Paths`: linhas desenhadas por cada cor;
- historico para desfazer;
- validacao de movimentos;
- deteccao de puzzle resolvido;
- aplicacao de dicas.

O metodo `StartPath` inicia uma linha a partir de uma bolinha da cor correspondente. O metodo `StartPathContinuation` permite continuar uma linha a partir da ponta solta, desde que ela ainda nao tenha chegado ao outro ponto da mesma cor.

O metodo `TryAppendDetailed` tenta adicionar uma celula ao caminho ativo. Ele retorna um `AppendResult`, que informa se o movimento foi aceito, bloqueado, completou um par, apagou parte da propria linha ou substituiu uma linha de outra cor.

As regras principais sao:

- so pode andar para celula vizinha ortogonal;
- nao pode continuar uma linha depois que ela ja ligou os dois pontos;
- nao pode atravessar bolinhas de outras cores;
- ao passar por cima de uma linha de outra cor, essa linha antiga e apagada;
- ao voltar sobre a propria linha, o trecho posterior e removido;
- o puzzle so e resolvido quando todos os pares estao conectados e todas as celulas do tabuleiro estao ocupadas.

## Geracao de puzzles

`PuzzleGenerator` cria os tabuleiros. Os tres primeiros niveis usam fases fixas, mais simples, para iniciar o jogador. A partir dai, os puzzles sao gerados automaticamente.

A progressao de tamanho usa `GetSizeForLevel`, partindo de 5x5 e chegando ate 15x15. A quantidade de cores vem de `GetPairCountForLevel`; em niveis altos ela e limitada para evitar muitas linhas curtas e faceis.

Para gerar puzzles automaticos, o codigo cria primeiro um caminho que cobre todo o tabuleiro. Esse caminho e embaralhado por uma tecnica de reconexao chamada no codigo de `TryBackbite`, que aumenta curvas e reduz o padrao obvio de serpentina. Depois esse caminho grande e dividido em varios caminhos menores, um para cada cor.

Por fim, `ValidateGeneratedPuzzle` garante que:

- todos os caminhos estao dentro do tabuleiro;
- nao ha sobreposicao na solucao;
- cada passo e adjacente;
- a solucao preenche todas as celulas.

## Interface do jogo

A tela principal fica em `MainWindow.xaml` e `MainWindow.xaml.cs`.

O XAML define a estrutura visual:

- painel lateral com nivel, botoes, score, dicas e status;
- area principal com o tabuleiro;
- botao `Ver solucao` no canto inferior direito.

O code-behind (`MainWindow.xaml.cs`) controla a interacao:

- carrega o nivel selecionado;
- desenha grade, linhas e pontos;
- converte posicao do mouse em celula do tabuleiro;
- inicia, continua e atualiza linhas durante o arraste;
- aplica penalidades e bonus;
- mostra janelas auxiliares.

O desenho usa um `Canvas`. Cada linha e desenhada como um `Path` com cantos retos entre centros de celulas. As bolinhas sao `Ellipse`.

## Score e dicas

Ao resolver um puzzle, o jogo calcula um bonus com base no tamanho do tabuleiro, quantidade de pares e nivel. Penalidades sao aplicadas quando o jogador:

- refaz uma ligacao ja desenhada;
- tenta movimento bloqueado;
- substitui linha de outra cor;
- desfaz jogada;
- reinicia tabuleiro com progresso.

As dicas usam `HintCredits`. O jogador recebe creditos iniciais e ganha novos creditos quando o score ultrapassa certos limites. O intervalo para ganhar novas dicas aumenta progressivamente, para permitir avancar sem tornar o jogo facil demais.

O botao `Usar dica` chama `ApplyNextMissingSolutionPath`, que escolhe uma cor ainda nao conectada e aplica o caminho correto dessa cor. Se esse caminho cruzar linhas atuais, as linhas conflitantes sao apagadas para manter o tabuleiro valido.

## Conclusao de fase

Quando `CheckSolved` detecta que o tabuleiro foi resolvido, ele:

1. calcula e adiciona o bonus;
2. atualiza progresso e score;
3. abre `LevelCompleteWindow`;
4. so carrega o proximo nivel depois que o jogador confirma.

Isso evita que o proximo puzzle apareca instantaneamente sem o jogador perceber a conclusao.

## Janela de solucao

O botao `Ver solucao` abre `SolutionWindow`. Essa janela recebe o `PuzzleDefinition` atual e desenha `SolutionPaths` em uma janela separada.

Ela e apenas diagnostica: nao altera o tabuleiro atual, nao consome dica, nao soma pontos e nao avanca nivel.

## Persistencia

`SettingsStore` salva e carrega `GameSettings` em JSON dentro de `%LocalAppData%\FlowGame\settings.json`.

Ele tambem normaliza dados antigos. Por exemplo, se um arquivo salvo foi criado antes da existencia de dicas, o jogo garante creditos iniciais e o proximo marco de score.

## Testes

`FlowGame.Tests` executa testes sem depender de framework externo. Os testes verificam, entre outros pontos:

- tamanho minimo e maximo de tabuleiro;
- solucao interna valida;
- bloqueio de movimentos invalidos;
- impossibilidade de continuar depois da bolinha final;
- continuacao a partir da ponta solta;
- substituicao de linha ao cruzar;
- aplicacao de dica;
- geracao valida em varios niveis;
- maior complexidade em niveis avancados.

Para executar:

```powershell
dotnet run --project .\FlowGame.Tests\FlowGame.Tests.csproj
```

Para compilar tudo:

```powershell
dotnet build .\FlowGame.sln
```
