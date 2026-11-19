# Sovereign of Ashes (Ecos do Trono Vazio) — inventário completo

Roguelike deckbuilder de fantasia sombria, Unity 6 (C#), interface em IMGUI com pixel art e pinturas. Solo, offline, Windows. Você é um deus que protege o Núcleo Divino de ondas de invasores usando cartas, alquimia, intervenção nos anéis e uma civilização que pode virar herege.

*Estado em 2026-11-19 (commit 1121ca5). 200 testes automáticos passando.*

## 1. Visão geral e objetivo
- Modo **Padrão**: sobreviver 20 turnos com o Núcleo Divino (30 de vida no Normal) vivo. Derrota: Núcleo a 0.
- Modo **Infinito (Hardcore)**: sem vitória; ondas nunca acabam; dificuldade Difícil fixa; vale o recorde de turnos.
- Partida única de ~15–25 min. Sem metaprogressão além de conquistas, recorde e a escolha de civilização/pacto.
- Fluxo de uma partida nova: menu → **escolha de civilização** (4) → **escolha de pacto** (1 de 3 sorteados, ou sem pacto) → jogo.

## 2. O tabuleiro
- 3 anéis concêntricos × 8 setores = 24 placas: **Ermos Externos** (onde os invasores surgem), **Meio** e **Santuário Interno**, Núcleo Divino no centro.
- Anéis giram sozinhos: Ermos 1 casa horária todo turno; Santuário 1 casa anti-horária a cada 3 turnos. Painel "Ao fim deste turno" mostra os giros e invasões previstos.
- Cada setor guarda no máximo um elemento da alquimia. Números de posição (0–7) na borda.

## 3. Estrutura do turno
Renda de Fé → Compra de 5 cartas → **Jogador** (cartas + Intervenção Divina) → Rotação tectônica → Civilizações (combate e Razão) → Marcha dos invasores → Efeitos alquímicos → Surgimento de invasores → Oferenda Divina (a cada 3 turnos) → Julgamento.

## 4. Economia de Fé
- Fé inicial 3, capacidade 10, renda base +2/turno, +20% do rendimento das civilizações. Mostrada como cristais.
- Cada invasor destruído rende Fé (Cultista 1, Profanador 2, Arauto 2, Colosso 3, Inquisidor 5). Elementos alquímicos geram Fé por turno.
- Recusar uma Oferenda rende +1 Fé.

## 5. Intervenção Divina (controle direto dos anéis)
- 1 vez por turno: **girar** um anel 1 casa (1 Fé) ou **congelar** um anel (2 Fé; ele ignora a próxima rotação). Painel recolhível ao lado do tabuleiro.
- Eremitas do Tempo giram de graça.

## 6. Cartas
**Baralho inicial (15):** Lança de Fogo ×3 (1 Fé, 4 de dano), Tempestade de Cinzas (3, 3 de dano ao alvo e vizinhos), Sermão ×2 (1, −3 Razão), Milagre (2, cura 6), Recrutar Fiéis (2, +2 Poder e +1 Razão), e 7 cartas Infundir (Ferro, Fogo, Carne, Fé, Sangue, Medo, Tempo; 1 Fé cada).

**Oferendas Divinas (a cada 3 turnos, escolher 1 de 3 ou recusar):** pool de 23 cartas, cada uma com ilustração pintada. Uma das 3 opções é sempre do tema (conceito) da civilização escolhida.
- Básicas (10): Lança de Ferro, Brasa Sagrada, Pavor, Sangue Bento, Giro Divino, Contragiro, Exílio, Dízimo de Sangue, Penitência, Juramento de Ferro.
- **De nicho (13):**
  - Fogo: *Maré de Cinzas* (3 Fé; 3 de dano a todos os invasores do anel), *Muro de Chamas* (infunde Fogo no setor e vizinhos).
  - Ferro/Carne: *Fundição*, *Carne Florescente* (infundem o conceito no setor e vizinhos), *Cruzada* (+5 Poder, +2 Razão).
  - Sangue: *Sangria* (Núcleo perde 3, causa 7 de dano), *Golpe Sanguessuga* (3 de dano e cura 3).
  - Medo: *Veredito* (mata invasor com ≤6 de vida, senão 3 de dano), *Debandada* (2 de dano e empurra para fora).
  - Tempo: *Giro Duplo* (gira 2 casas), *Pêndulo* (2 de dano e gira o anel).
  - Fé: *Confissão* (−2 Razão, cura 2), *Colheita Sagrada* (+6 Fé nas civilizações).
- Mão de 5; descarte e reembaralho automáticos. Clique na carta + clique no setor; uso imediato ao clicar; botão direito cancela; carta ampliada ao passar o mouse.

## 7. Alquimia Divina (19 receitas)
Jogar um conceito sobre o elemento do setor, **em ordem**, transmuta. Elementos criados agem sozinhos no fim de cada turno.
- **6 iniciais:** Forja Sagrada (Ferro+Fogo: queima 3/1), Golem de Ferro (Carne+Ferro: barreira 3, contra-ataque 2), Santo Vivo (Carne+Fé: −1 Razão/turno), Espectro (Sangue+Medo: drena 2, cura 1), Cinzas (Fogo+Tempo: +1 Fé), Altar de Sacrifício (Fé+Sangue: +2 Fé, +1 Razão).
- **7 combinações novas:** Pira do Terror (Fogo+Medo: queima o anel inteiro, 1/turno), Carrasco Carmesim (Sangue+Ferro: drena 4, cura 2), Astrolábio (Tempo+Ferro: gira o próprio anel todo turno), Oráculo (Tempo+Fé: +1 Fé e +1 Poder), Aberração (Medo+Carne: barreira 2 que devolve 4), Relicário de Sangue (Sangue+Tempo: cura o Núcleo 2/turno), Névoa do Medo (Medo+Tempo: empurra invasores para fora).
- **6 evoluções (elemento + conceito):** Forja Eterna (Forja+Tempo: queima 5/2), Golem de Magma (Golem+Fogo: barreira 4 + queima 2 nos vizinhos), Mártir (Santo+Sangue: cura 2, −2 Razão), Fênix (Cinzas+Fé: +3 Fé e cura 1), Ceifador (Espectro+Medo: drena 5, cura 2), Altar Profano (Altar+Medo: +4 Fé, +2 Razão).
- **Grimório** (tecla G): lista rolável das 19 receitas e efeitos. Tooltip do setor mostra o que o elemento faz e a prévia da infusão.

## 8. Civilizações (4, escolhidas antes do pacto)
Cada uma tem retrato pintado próprio, frase própria, vantagem, preço e um tema de cartas (as Oferendas favorecem o tema).
- **Filhos da Cinza** (Poder 5; Razão sobe 1/turno; sem habilidade especial; tema Fogo e Fé).
- **Forjadores do Abismo** (Poder 8; começam com um Golem de Ferro e cartas de Ferro/Carne a mais; começam com 2 de Razão e 2 cristais de Fé a menos; tema Ferro e Carne).
- **Flagelantes do Eclipse** (Poder 3; cada carta de Sangue/Medo jogada cura 2 o Núcleo; Razão sobe 2/turno; tema Sangue e Medo).
- **Eremitas do Tempo** (Poder 4; girar anéis custa 0 Fé; Razão só sobe em turnos pares; 4 cartas de giro/Tempo extras; 1 Fé inicial a menos; tema Tempo).
- Mecânica comum: Razão sobe por turno; ao chegar ao máximo (10) a civilização vira **herege** (para de defender e ataca o Núcleo). Sermão, Santo Vivo, Confissão etc. reduzem a Razão; há redenção.

## 9. Pactos (7; escolher 1 de 3 sorteados antes da partida)
Decreto das Cinzas (+50% de ganho de Fé / Razão +1 por turno), Silêncio Sagrado (+2 Sermões / −2 cristais de Fé), Juramento de Aço (+3 Poder / Núcleo −6), Mão Larga (compra 6 / renda de Fé −1), Pacto Alquímico (+3 Infundir / −2 Fé inicial), Mundo em Rotação (+3 cartas de giro / Ermos giram 2 casas), Fome de Sangue (+2 Dízimos e Milagre / Núcleo −4).

## 10. Invasores (9 tipos comuns + 4 chefes)
- **Cultista Cinzento** (5 vida, 2 ataque): desde o turno 1, a cada 3 turnos, crescendo.
- **Colosso do Vazio** (12, 4): a partir do turno 5.
- **Profanador** (4, 1): destrói o elemento do setor em que chega (a partir do turno 6).
- **Arauto Herege** (6, 0): soma +1 Razão às civilizações por turno (a partir do turno 9).
- **Guardião Profano** (10, 1; turno 7): cada golpe contra ele perde 2 de dano (mínimo 1). *Só no Normal e no Difícil.*
- **Corredor Herege** (4, 2; turno 8): ignora barreiras (o Golem não o segura). *Normal e Difícil.*
- **Necroapóstolo** (8, 1; turno 11): cura 2 de vida de todos os outros invasores do anel por turno. *Normal e Difícil.*
- **Flagelo Explosivo** (6, 3; turno 12): ao morrer fere 2 os invasores vizinhos e destrói o elemento do próprio setor (explosões em cadeia). *Normal e Difícil.*
- **Chefe do turno 10 (um dos três, sorteado por partida; a mesma semente repete):** Inquisidor Cinzento (20 vida, 4 ataque, +1 Razão), **Profeta do Tempo** (26, 3; gira o anel do meio todo turno) ou **Colosso Carmesim** (32, 4; destrói o elemento onde está).
- **Chefe final, Rei Herege (turno 15, todas as dificuldades):** 30/40/46 de vida no Fácil/Normal/Difícil (mais o reforço por turno), 4–5 de ataque, +2 Razão por turno. Chega ao Núcleo antes do turno 20: é preciso retardá-lo (Golem, giros) e derrubá-lo.
- Marcham um anel para dentro por turno, atacam o Núcleo ao chegar ao Santuário; setas no tabuleiro mostram para onde vão (inclusive o Corredor, que ignora barreiras).
- **Modo infinito:** Cultistas todo turno, Colossos de 2 em 2 turnos, orda a cada 5, reforço a cada 10, e todos os invasores novos e chefes (Profeta no turno 20, Colosso Carmesim no 25, Rei Herege no 30 e a cada 30).
- *Retratos pintados dos 7 invasores novos ainda pendentes: usam um sprite com cor própria.*

## 11. Dificuldade
| | Núcleo | Fé inicial (máx.) | Ondas | Reforço dos invasores novos |
|---|---|---|---|---|
| Fácil | 36 | 4 (11) | crescimento 1, sem invasores novos | nenhum; Rei Herege com 30 de vida |
| Normal | 30 | 3 (10) | crescimento 3, Colossos a cada 2 turnos, Profanadores a cada 3 | +1 de vida a cada 4 turnos; +1 de ataque a cada 8; invasores novos; Rei Herege com 40 |
| Difícil | 21 | 2 (8) | crescimento 4, Colossos a cada 2, Profanadores a cada 2 | +2 de vida a cada 3 turnos; +1 de ataque a cada 6; invasores novos; Rei Herege com 46 |

## 12. História e apresentação
- Texto de abertura, frases por civilização, falas de invasores e do Inquisidor, crônicas a cada 3 turnos, fechos de ato nos turnos 8 e 15, epílogos de vitória por civilização e de derrota por causa.
- Painéis laterais recolhíveis (Civilizações, Ao fim deste turno, Invasores, Mandamentos, Crônica, Intervenção); o estado fica salvo.
- Pixel art por código + pinturas (fundo, retratos, ilustrações de carta, logo, telas de vitória/derrota). Brasas, partículas, números flutuantes.
- **Áudio sintetizado por código:** música de menu e de batalha, 21 efeitos, volumes separados.

## 13. Menus, configurações e conquistas
- Menu principal: Jogar Agora, Modo Infinito, Como Jogar, Conquistas (0/21), Configurações, Sair. Pausa (Esc) com Grimório, Configurações, Reiniciar, Menu.
- Configurações salvas: Dificuldade, Tela cheia, Animações, Volumes de Música/Efeitos, Semente (mesma semente repete a partida).
- **21 conquistas** salvas com aviso na tela: Primeiro Sangue, Exterminador (100), Ceifador de Hordas (500), Primeira Transmutação, Mestre Alquimista (19 receitas), Devoto (100 Fé), Oferenda Aceita, Baralho Consagrado (21 cartas), Fé Corrompida, O Trono Caiu, Veterano (10 partidas), O Trono Resiste, Pacto Cumprido, Mão Divina (10 intervenções), Seitas Unidas (vencer com as 4), Por um Fio, Intocável, Cinzas e Glória (vencer no Difícil), Resistência/Inabalável/Lenda das Cinzas (turnos 10/25/50 no infinito).

## 14. Arquitetura e qualidade
- Lógica em C# puro e testável; view IMGUI só lê e chama. Conteúdo padrão em código, substituível por ScriptableObjects.
- 183 testes EditMode (inclui bot de simulação). Modo `-sovereign-capture` joga sozinho e gera prints.
- Build Windows de ~115 MB (texturas comprimidas); zip de 56 MB para compartilhar.

## 15. Limitações conhecidas
- **Equilíbrio** medido só com um bot fraco (morre ~turno 12 em qualquer dificuldade); sem calibração com jogadores reais. Normal e Difícil foram endurecidos por estimativa.
- Sem tutorial interativo (só uma tela de texto); sem controle por gamepad/toque; sem localização (só português); sem atalhos de teclado para cartas.
- **Mandamentos** existem no código, mas só os pactos os usam. Sem loja, sem mapa de run, sem relíquias, sem remoção de cartas do baralho, sem upgrade de cartas.
- Falta o Inquisidor Ascético (reduz curas) e novos chefes alternativos para o turno 15.
- Ícones das conquistas reutilizam o emblema do Núcleo. Heresia tem retrato único para todas as seitas.
- Descrições das cartas "Infundir" citam só as receitas antigas.
- IMGUI limita polimento visual e acessibilidade.
