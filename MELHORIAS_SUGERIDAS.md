# Sovereign of Ashes — melhorias sugeridas (Gemini + ChatGPT)

Gerado em 2026-11-19. Base: `INVENTARIO_DO_JOGO.md` (estado do commit 1121ca5). O inventário foi enviado como resumo às duas IAs (Gemini "Neon", análise profunda, e ChatGPT), sem dados pessoais. As sugestões abaixo são delas; a coluna "Minha leitura" é a avaliação de quem mantém o código. Nada disso foi implementado ainda.

## O que as duas IAs disseram em comum (maior confiança)

| # | Problema | Proposta das IAs | Minha leitura |
|---|---|---|---|
| 1 | **O baralho não pode ser moldado.** Só se adiciona carta; 15 iniciais viram no máximo 21, e ~70% do baralho final é o inicial. A recusa "+1 Fé" não é escolha real. | Trocar/complementar a recusa por **remover 1 carta**; ocasionalmente **melhorar** uma carta (Lança 4→6 de dano, Milagre 6→9). ChatGPT: custo crescente (3/5/7 Fé) e "Oferendas especiais" nos turnos 6, 12 e 18. | Concordo e é a melhor relação impacto/esforço. `RewardSystem` já tem `Skip`; falta `CardDeck.Remove` e uma 4ª opção na tela. |
| 2 | **Razão é tudo ou nada.** 9 de Razão = 0 de Razão até virar herege no 10. | Faixas graduais (ex.: 0–3 devoção +25% Fé; 4–6 normal; 7–9 dúvida −25 a −50% de Fé e −2 Poder; 10 heresia). | Concordo. Cada ponto de Razão passa a importar todo turno. Mexe em `CivilizationEntity` e no rendimento de Fé. |
| 3 | **Eremitas do Tempo: giro grátis é forte demais** (economiza até ~20 Fé por partida, alinha invasores em Golem/Pira sem custo). | Só o 1º giro por turno grátis; o 2º custa 1 Fé; congelar continua 2. | Concordo. Mudança de 2 linhas no `RingControlService`/hook da civilização. |
| 4 | ✅ *Feito (parcial)* — **Falta clímax e estrutura na 2ª metade.** Inquisidor no turno 10 e depois só +1 de vida a cada 4 turnos. | **Chefe final no turno 20** (ex.: 45–50 de vida, 6 de ataque, habilidade que gira anéis ou sobe Razão), onda temática no 15; dividir em atos. **Chefes alternativos** sorteados (Anjo Devorador, Colosso Carmesim, Profeta do Tempo, Rei Herege). | Concordo. O jogo já tem fechos de ato nos turnos 8 e 15 (texto); falta o conteúdo mecânico. |
| 5 | ✅ *Feito (4 dos 5)* — **Pouca variedade de invasores** (só 3 de 5 têm habilidade). | Novos tipos: Guardião (reduz 2 do dano), Corredor/Fanático (avança 2 anéis), Necroapóstolo (cura o anel), Flagelo Explosivo (explode ao morrer), Inquisidor Ascético (reduz cura). Passiva simples no Colosso. | Concordo. Cada um testa uma parte diferente da estratégia (dano alto, posição, horda, alquimia, cura). |
| 6 | **Economia de Fé transborda / bola de neve.** Abates rendem 1–5 Fé e o teto é 10; a tensão some depois do turno ~6. | Reduzir recompensa por abate (1/1/1/2/4); criar sumidouro de Fé (3 Fé = +1 carta 1×/turno). | Precisa de dados reais antes (ver nº 10). Pode ser verdade ou não. |
| 7 | **Cura demais para 30 de vida** (Milagre 6 + Relicário + Flagelantes). | Milagre 6→4 ou custo 3; teto de cura por turno (Gemini). ChatGPT prefere **medir cura líquida** antes de limitar. | Concordo com medir primeiro; Milagre a 4–5 é ajuste seguro. |
| 8 | **Sem tutorial interativo**; muita coisa de uma vez (3 anéis, 19 receitas, Razão, pactos). | Turno 1 roteirizado e opcional (mão fixa: Lança + 2 Infundir que fazem a Forja). Prévia da receita ao passar a carta sobre o setor. | Concordo. A prévia de infusão já existe no tooltip do setor; falta destacar o nome da receita resultante. |

## Só uma das IAs (avaliar caso a caso)

**Gemini**
- **Receitas comutativas** (A+B = B+A) e/ou Infundir duplas para o baralho inicial deixar de ter 7 cartas de conceito. *Cuidado:* a ordem é parte da identidade da alquimia; talvez melhor manter a ordem só nas 6 iniciais e tornar as combinações novas comutativas.
- **Ordem do turno:** mover a fase de Alquimia para antes da marcha dos invasores (ou mostrar prévia).
- **Cartas fracas:** Tempestade de Cinzas (3 Fé, 3 de dano a até 3 alvos = quase sempre pior que 2 Lanças) → 2 Fé; Veredito vira carta morta depois do turno ~6 (≤6 de vida) → ≤8; Maré de Cinzas tem o mesmo problema de custo.
- **Combos a vigiar:** Flagelantes + Sangria + Relicário de Sangue (cura anula o custo de vida); Eremitas + Pira do Terror/Névoa/Golem.
- **Rejogabilidade:** baralho inicial diferente por civilização; segundo pacto após o Inquisidor; níveis de "Ascensão/Heresia" empilháveis; setas de "próxima rotação" na borda dos anéis.

**ChatGPT**
- **Telemetria e testes** antes de tudo: registrar vitória, turno de derrota, Fé desperdiçada, cartas e receitas usadas, por civilização; metas iniciais (Normal 35–50%, Difícil 10–25%, cada civilização 30–55%); **3 bots** (conservador, agressivo, alquimista) e ≥100 partidas por combinação.
- **Alquimia:** efeitos condicionais pela posição (Forja Eterna no Santuário gera Fé), **sinergias entre vizinhos** (Golem junto de Forja +2 de vida; Fênix junto de Fogo +2 de dano), **livro de receitas descobertas** que persiste entre partidas.
- **Oferendas:** classificar por categoria (ofensiva, defensiva, alquímica, econômica, estratégica) e garantir variedade (1 utilidade geral, no máx. 2 da mesma categoria).
- **Flagelantes:** Razão +1 por turno (em vez de +2) com outra forma de custo, para não ser punição quase inevitável.
- **Pactos:** sorteio que evite 3 pactos da mesma estratégia.
- **Eventos narrativos** (12, três por partida) com escolhas de risco; cartas lendárias por civilização; **Modo Infinito em ciclos de 5 turnos** com novas habilidades; recordes por estilo (maior dano alquímico etc.).

## Ordem que eu seguiria

1. **Medir de verdade** (bots com 3 estilos + telemetria): sem isso, os números de Fé, cura e dificuldade são palpite. Dá para evoluir o `BalanceBot` que já existe.
2. **Remover/melhorar cartas** nas Oferendas (nº 1).
3. **Eremitas: só o 1º giro grátis** (nº 3) e **Milagre 4–5** (nº 7), ajustes de uma linha.
4. **Razão em faixas** (nº 2).
5. ~~Chefe final + invasores novos~~ **feito em 2026-11-19**: Rei Herege no turno 15, chefe do turno 10 sorteado entre 3, e 4 invasores novos (Guardião, Corredor, Necroapóstolo, Flagelo). Falta o Inquisidor Ascético, mais chefes finais e os retratos pintados.
6. **Cartas fracas:** Tempestade 2 Fé, Veredito ≤8.
7. **Tutorial do turno 1** e destaque da receita resultante (nº 8).
8. Depois: eventos, chefes alternativos, Infinito em ciclos, ícones próprios das conquistas.

## Ressalvas
- São opiniões de duas IAs lendo um resumo, não de jogadores; vários números (45–50 de vida do chefe, custos 3/5/7) são ponto de partida.
- Duas afirmações não conferem com o jogo: o ChatGPT supôs que Razão +2 chega a 10 "em cinco turnos" sem considerar Sermão e que giros custam sempre 1 Fé; o Gemini assumiu que Veredito/Tempestade nunca combinam com dano prévio. Vale testar antes de mudar.
- O Gemini atingiu o limite de uso ao fim da resposta (reinicia às 16:17).
