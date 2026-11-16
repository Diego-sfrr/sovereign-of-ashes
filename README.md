# Sovereign of Ashes (Ecos do Trono Vazio)

Roguelike deckbuilder de estratégia divina e fantasia sombria, feito em Unity (C#).

Arquitetura, diagramas e regras: [ARQUITETURA.md](ARQUITETURA.md).

## Como jogar (2 minutos de configuração)

1. Instale o [Unity Hub](https://unity.com/download) e, por ele, um Editor **LTS** (2022.3 ou Unity 6).
2. No Hub: **New project → 3D (Built-In Render Pipeline)**. "Universal 3D" também funciona.
3. Copie a pasta `Assets/_Project` deste repositório para dentro de `Assets/` do projeto.
4. Na cena aberta: **GameObject → Create Empty**, e no Inspector **Add Component → Sovereign Game View**.
   (O `SovereignBootstrapper` é adicionado junto, automaticamente.)
5. Aperte **Play**.

Nenhum asset é necessário: sem configuração, a partida usa o conteúdo padrão (baralho de 10 cartas,
receitas de alquimia, a civilização "Filhos da Cinza" e ondas de invasores).

### Controles

| Ação | Como |
|---|---|
| Jogar carta com alvo | Clique na carta, depois no setor destacado do tabuleiro |
| Jogar carta de uso imediato | Clique na carta ("Uso imediato") |
| Cancelar seleção | Botão direito do mouse |
| Ler uma carta em tamanho grande | Passe o mouse sobre ela na mão |
| Ver detalhes de um setor | Passe o mouse sobre ele (com uma carta de infusão selecionada, mostra o que ela faria ali; com invasor, mostra o retrato) |
| Abrir o Grimório (receitas de alquimia) | Botão **GRIMÓRIO** ou tecla **G**; **Esc** fecha |
| Pausar (continuar, configurações, reiniciar, menu principal) | **Esc** durante a partida |
| Encerrar o turno | Botão **ENCERRAR TURNO** |

### Menu e configurações

Ao abrir o jogo aparece o **menu principal** (Jogar agora, Como jogar, Configurações, Sair). Em **Configurações**, salvas automaticamente:
**Dificuldade** (Fácil / Normal / Difícil: vida do Núcleo, Fé inicial e crescimento das invasões), **Tela cheia**, **Animações** (normal/rápidas) e **Semente** (0 = aleatória; o mesmo número repete a partida). Dificuldade e semente valem a partir da próxima partida. **Música** e **Efeitos** têm volume próprio de 0 a 10.

**Painéis recolhíveis:** durante a partida cada seção dos painéis (Civilizações, Ao fim deste turno, Invasores, Mandamentos, Crônica e Intervenção) tem um botão `-`/`+` para abrir e fechar; com a Crônica fechada o tabuleiro ganha espaço. O jogo lembra o que você deixou fechado.

**História:** o jogo tem lore próprio (ver `HISTORIA.md`): o Primeiro Soberano que partiu, o Núcleo que é âncora e prisão, a Razão como despertar. A Crônica abre com o mito, cada civilização tem sua frase, os invasores falam na primeira vez que surgem, o Inquisidor tem fala própria no turno 10 e na morte, e a tela de fim de jogo mostra um epílogo (vitória por civilização; derrota pela causa).

**Áudio:** música (tema do menu e tema de batalha, em loop contínuo) e 21 efeitos sonoros (cartas, alquimia, dano no Núcleo, Fé, anéis girando, heresia, vitória/derrota...). Tudo é **sintetizado por código** ao abrir o jogo, sem arquivos de som.

**Civilizações:** antes do pacto você escolhe sua seita: **Filhos da Cinza** (equilibrada), **Forjadores do Abismo** (Poder 8 e um Golem inicial), **Flagelantes do Eclipse** (cada carta de Sangue ou Medo cura o Núcleo, mas a Razão sobe 2 por turno) ou **Eremitas do Tempo** (girar anel de graça, Razão só sobe nos turnos pares).

**Intervenção Divina:** o painel ao lado do tabuleiro gasta Fé para **girar um anel 1 casa** (1 Fé) para qualquer lado ou **congelá-lo** (2 Fé): ele ignora a próxima rotação programada. Só 1 intervenção por turno; a previsão de marcha já mostra o efeito.

**Pactos e Oferendas:** antes de cada partida você escolhe 1 de 3 **Pactos** (regras especiais com vantagem e preço, como "Mão Larga" ou "Mundo em Rotação"), ou segue sem pacto. A cada 3 turnos surge uma **Oferenda Divina**: escolha 1 de 3 cartas para levar ao baralho (ou recuse e ganhe +1 de Fé). O pool tem 10 cartas novas, entre elas **Giro Divino / Contragiro** (giram um anel inteiro), **Exílio** (empurra um invasor para fora) e **Dízimo de Sangue** (vida por Fé).

**Novos invasores:** o **Profanador** destrói o elemento do setor em que chega; o **Arauto Herege** e o **Inquisidor** (chefe do turno 10) somam Razão às civilizações a cada turno. No modo infinito eles escalam sem parar.

**Conquistas:** 21 conquistas ganhas jogando (primeiro sangue, mestre alquimista, vencer sem dano, chegar ao turno 50 no modo infinito...). Veja a lista e o progresso em **CONQUISTAS** no menu; um aviso desce do topo quando você ganha uma. O progresso fica salvo.

**Modo infinito (hardcore):** botão no menu principal. As ondas nunca acabam (Cultistas todo turno, Colossos de 2 em 2 a partir do turno 4, uma orda dobrada a cada 5 turnos, e todos ficam mais fortes a cada 10 turnos), com a dificuldade Difícil fixa. Não há vitória: o objetivo é bater o recorde de turnos, que fica salvo e aparece no botão.

### Barra do topo

- **Fé em cristais:** um por ponto de capacidade. Cristal aceso = Fé disponível; apagado = vazio. Ao ganhar Fé os
  cristais novos "estouram"; ao gastar, os gastados se dissolvem. O custo de cada carta é um cristal igual.
- **Vida do Núcleo em corações** (até 10; com vida alta cada coração vale vários pontos), com preenchimento parcial.
  Ao levar dano os corações atingidos tremem e deixam um rastro claro; com vida baixa (25% ou menos) pulsam em vermelho.
- **Trilha de turnos** na borda inferior: um segmento por turno até a vitória.

### Objetivo

Sobreviva **20 turnos** com o Núcleo Divino de pé.

- Invasores surgem nos **Ermos Externos** e marcham um anel por turno até o Núcleo, seguindo o setor
  **alinhado** naquele turno. A rotação dos anéis muda o caminho deles.
- Sua civilização devota combate os invasores, mas a **Razão** dela cresce todo turno.
  No máximo, ela vira **herege** e passa a atacar o Núcleo. Use **Sermão** para conter isso.
- O painel **"Ao fim deste turno"** mostra quais anéis vão girar e qual onda vai chegar.
- **Intenção dos invasores:** na sua vez, cada invasor mostra para onde vai marchar (pontos até o setor de
  destino, já contando a rotação prevista): seta dourada = avança, escudo = bloqueado, espada = ataca o Núcleo.
  Os **números nas bordas** do anel externo são as posições angulares: a marcha alinha invasor e destino pelo
  mesmo número.
- **Alquimia:** as cartas "Infundir" manifestam conceitos; jogar um conceito sobre outro os funde em um
  elemento que age sozinho (Forja queima invasores, Golem bloqueia a marcha, Santo acalma a Razão,
  Espectro drena vida, Cinzas e Altar rendem Fé). **A ordem importa**: veja as receitas no Grimório.

## Testes automáticos

No Unity: **Window → General → Test Runner → EditMode → Run All**.

## Personalizar

Tudo em **Create → Sovereign of Ashes** (cartas, efeitos, receitas, invasores, ondas, Mandamentos,
perfis de Fé, de turnos e de civilização). Arraste os assets nos campos do `SovereignBootstrapper`;
qualquer campo vazio continua usando o conteúdo padrão.

### Pinturas da interface

Pinturas geradas no Gemini, carregadas de `Assets/_Project/Resources` (sem elas a interface usa os desenhos
em pixel art gerados por código):

| Pasta | Arquivos | Uso |
|---|---|---|
| `UI/` | `backdrop`, `defeat`, `victory`, `cardback` | Fundo atrás do tabuleiro, telas de fim de jogo, verso das cartas nas pilhas |
| `Portraits/` | `ash_cultist`, `void_colossus`, `civ_devout`, `civ_heretic` | Painel de invasores, tooltip do setor e painel da civilização (o retrato muda se ela virar herege) |

Um invasor novo ganha retrato colocando `Portraits/<id do invasor>.jpg`. Os originais ficam em `ArtSource/`
(fora do build). A importação (sem compressão, bilinear) é configurada pelo `PaintingImporter`.

### Arte das cartas

Cada carta usa a ilustração `Assets/_Project/Resources/CardArt/<id da carta>.png`, se ela existir
(ex.: `fire_spear.png`). Sem imagem, a carta mostra o sprite em pixel art gerado por código.
A importação (sem compressão, com mipmaps, até 512px) é configurada sozinha pelo `CardArtImporter`.
Os originais em 2048px, gerados pelo Gemini, ficam em `ArtSource/Cards` (fora do build).

## Estrutura

```
Assets/_Project/
  Scripts/
    Core, Board, Alchemy, Cards, Civilization, Rules, Economy, Invaders, Turns   regras em C# puro
    Session        monta a partida (GameSession), conteúdo padrão, crônica e ações do jogador
    Presentation   bootstrapper, interface (tabuleiro gerado + HUD) e geometria de clique
  Tests/           testes EditMode (NUnit)
```
