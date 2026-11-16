# Sovereign of Ashes (Ecos do Trono Vazio) — inventário completo

Roguelike deckbuilder de fantasia sombria, Unity 6 (C#), interface em IMGUI com pixel art e pinturas. Solo, offline, Windows. Você é um deus que protege o Núcleo Divino de ondas de invasores usando cartas, alquimia e uma civilização que pode virar herege.

## 1. Visão geral e objetivo
- Modo **Padrão**: sobreviver 20 turnos com o Núcleo Divino (30 de vida) vivo. Derrota: Núcleo a 0.
- Modo **Infinito (Hardcore)**: sem vitória; ondas nunca acabam; dificuldade Difícil fixa; vale o recorde de turnos.
- Partida única de ~15–25 min. Não há metaprogressão além de conquistas e recorde.

## 2. O tabuleiro
- 3 anéis concêntricos × 8 setores = 24 placas: **Ermos Externos** (onde os invasores surgem), **Meio** e **Santuário Interno**, com o **Núcleo Divino** no centro.
- Anéis giram sozinhos: Ermos 1 casa horária todo turno; Santuário 1 casa anti-horária a cada 3 turnos (planejador tectônico configurável).
- Cada setor guarda no máximo um elemento da alquimia. Números de posição (0–7) na borda, decoração (raios, aros, faixa de runas, halo).

## 3. Estrutura do turno
Renda de Fé → Compra → **Jogador** → Rotação tectônica → Civilizações (combate e Razão) → Marcha dos invasores → Efeitos alquímicos → Surgimento de invasores → Julgamento (vitória/derrota).

## 4. Economia de Fé
- Fé inicial 3, capacidade 10, renda base +2/turno, +20% do rendimento das civilizações. Mostrada como **cristais** na barra do topo (com animações de ganho, gasto e dissolução).
- Cada invasor destruído rende Fé (Cultista 1, Colosso 3). Cinzas e Altar de Sacrifício geram Fé por turno.

## 5. Cartas (baralho inicial de 15)
Compra de 5 cartas por turno; descarte e reembaralho automáticos.
| Carta | Custo | Efeito |
|---|---|---|
| Lança de Fogo (×3) | 1 | 4 de dano a um invasor |
| Tempestade de Cinzas | 3 | 3 de dano ao alvo e aos invasores dos setores vizinhos |
| Sermão (×2) | 1 | Todas as civilizações: −3 de Razão |
| Milagre | 2 | Cura 6 de vida do Núcleo |
| Recrutar Fiéis | 2 | Todas as civilizações: +2 Poder Militar, +1 Razão |
| Infundir Ferro / Fogo / Carne / Fé / Sangue / Medo / Tempo (1 cada) | 1 | Manifesta o conceito no setor (alvo) e dispara receitas |
- Cada carta tem ilustração pintada (Gemini) 512 px. Interação: clicar na carta e depois no setor destacado; cartas de uso imediato jogam ao clicar; botão direito cancela; carta ampliada ao passar o mouse; pilhas de compra/descarte visíveis.

## 6. Alquimia Divina
- 7 conceitos base: Carne, Fogo, Ferro, Tempo, Fé, Medo, Sangue. Jogar um conceito sobre outro, **em ordem**, transmuta.
- 6 receitas e seus efeitos em jogo (a cada turno, fase de efeitos):
  - Ferro + Fogo → **Forja Sagrada**: queima o invasor no setor (3) e os vizinhos (1).
  - Carne + Ferro → **Golem de Ferro**: barreira (durabilidade 3) que bloqueia a marcha e contra-ataca (2).
  - Carne + Fé → **Santo Vivo**: −1 Razão nas civilizações por turno.
  - Sangue + Medo → **Espectro**: drena 2 de dano e cura 1 por golpe.
  - Fogo + Tempo → **Cinzas**: +1 Fé por turno.
  - Fé + Sangue → **Altar de Sacrifício**: +2 Fé por turno e +1 Razão.
- **Grimório** (botão ou tecla G): lista receitas e efeitos. Tooltip do setor mostra o que o elemento faz e a prévia da infusão.

## 7. Civilização "Filhos da Cinza"
- Poder Militar 5, Fé 10/20, Razão 0/10. Ataca invasores sozinha no fim do turno.
- Razão sobe 1 por turno; ao chegar a 10 a civilização vira **herege** (para de defender e ataca o Núcleo). Sermão e Santo Vivo reduzem a Razão; há estado de redenção.
- Suporte no código a várias civilizações e **Mandamentos** (regras modificadoras), mas o conteúdo padrão traz só uma civilização e nenhum mandamento.

## 8. Invasores
- **Cultista Cinzento** (5 vida, 2 ataque, 1 Fé): desde o turno 1, crescendo a cada 3 turnos.
- **Colosso do Vazio** (12 vida, 4 ataque, 3 Fé): a partir do turno 5, a cada 3 turnos.
- Marcham um anel para dentro por turno, entram pelo setor de mesmo número, atacam o Núcleo ao chegar ao Santuário; são bloqueados por Golem.
- **MarchForecaster**: setas no tabuleiro mostram para onde cada invasor vai (testado contra a fase real).
- Modo infinito: Cultistas todo turno em número crescente, Colossos de 2 em 2 turnos desde o 4, orda dobrada a cada 5 turnos, +força a cada 10 turnos. O limite é o anel externo lotar.

## 9. Menus e configurações
- **Menu principal** com logo pintado: Jogar Agora, Modo Infinito (mostra o recorde), Como Jogar, Conquistas, Configurações, Sair.
- **Configurações** (salvas): Dificuldade (Fácil 36 vida/4 Fé; Normal 30/3; Difícil 21/2 com ondas mais rápidas), Tela cheia, Animações (normal/rápidas), Semente (0 aleatória; mesma semente repete a partida). Restaurar padrões.
- **Pausa** (Esc): Continuar, Grimório, Configurações, Reiniciar, Menu principal (com confirmação).
- Telas de **vitória** e **derrota** pintadas, com Jogar novamente e Menu principal.

## 10. Conquistas (16, salvas, com aviso na tela)
Primeiro Sangue, Exterminador (100 invasores), Ceifador de Hordas (500), Primeira Transmutação, Mestre Alquimista (6 elementos), Devoto (100 Fé gastas), Fé Corrompida, O Trono Caiu, Veterano (10 partidas), O Trono Resiste, Por um Fio, Intocável, Cinzas e Glória (vencer no Difícil), Resistência / Inabalável / Lenda das Cinzas (turnos 10/25/50 no infinito).

## 11. Interface e apresentação
- Barra do topo: logo, turno X/20 (ou ∞) e estado, cristais de Fé, corações de vida do Núcleo, botão do Grimório; trilha de turnos.
- Painel esquerdo: civilizações (Poder, Fé, Razão), previsão "ao fim deste turno" (giros e invasão), lista de invasores com retratos e intenção, mandamentos.
- Painel direito: Crônica (log narrado em português). Mão de cartas ampliável; botão ENCERRAR TURNO.
- Pixel art por código + pinturas: fundo, retratos (cultista, colosso, civilizações), versos de carta, ilustrações das cartas, logo. Brasas, partículas, números flutuantes de dano, rastros e animações.
- **Sem áudio** (nem música nem efeitos).

## 12. Arquitetura e qualidade
- Lógica em C# puro e testável (Core, Board, Alchemy, Cards, Civilization, Rules, Economy, Invaders, Turns, Session, Achievements); view IMGUI só lê e chama.
- Conteúdo padrão em código (`DefaultContent`), substituível por ScriptableObjects (cartas, receitas, civilizações, mandamentos, ondas, economia, turnos).
- 87 testes automáticos EditMode; modo `-sovereign-capture` joga sozinho e gera prints; README e ARQUITETURA.md.

## 13. Limitações conhecidas
- Sem áudio; sem tutorial interativo (só uma tela de texto); sem controle por gamepad/toque; sem localização (só português).
- Só 1 civilização, 0 mandamentos ativos, 2 tipos de invasor, 15 cartas fixas (sem recompensas de cartas, sem loja, sem mapa de run, sem relíquias).
- Equilíbrio medido só com um jogador automático: 18% de vitória no Normal, 87% no Fácil, 14% no Difícil.
- Painel esquerdo ainda mostra a Fé da civilização como barra azul (não cristais). Ícones de conquista reutilizam o emblema do Núcleo.
- IMGUI limita polimento visual e acessibilidade; sem atalhos de teclado para cartas.
