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
| Ver detalhes de um setor | Passe o mouse sobre ele (com uma carta de infusão selecionada, mostra o que ela faria ali) |
| Abrir o Grimório (receitas de alquimia) | Botão **GRIMÓRIO** ou tecla **G**; **Esc** fecha |
| Encerrar o turno | Botão **ENCERRAR TURNO** |

### Objetivo

Sobreviva **20 turnos** com o Núcleo Divino de pé.

- Invasores surgem nos **Ermos Externos** e marcham um anel por turno até o Núcleo, seguindo o setor
  **alinhado** naquele turno. A rotação dos anéis muda o caminho deles.
- Sua civilização devota combate os invasores, mas a **Razão** dela cresce todo turno.
  No máximo, ela vira **herege** e passa a atacar o Núcleo. Use **Sermão** para conter isso.
- O painel **"Ao fim deste turno"** mostra quais anéis vão girar e qual onda vai chegar.
- **Alquimia:** as cartas "Infundir" manifestam conceitos; jogar um conceito sobre outro os funde em um
  elemento que age sozinho (Forja queima invasores, Golem bloqueia a marcha, Santo acalma a Razão,
  Espectro drena vida, Cinzas e Altar rendem Fé). **A ordem importa**: veja as receitas no Grimório.

## Testes automáticos

No Unity: **Window → General → Test Runner → EditMode → Run All**.

## Personalizar

Tudo em **Create → Sovereign of Ashes** (cartas, efeitos, receitas, invasores, ondas, Mandamentos,
perfis de Fé, de turnos e de civilização). Arraste os assets nos campos do `SovereignBootstrapper`;
qualquer campo vazio continua usando o conteúdo padrão.

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
