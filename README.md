# Sovereign of Ashes (Ecos do Trono Vazio)

Roguelike deckbuilder de estratégia divina e fantasia sombria, feito em Unity (C#).

Arquitetura, diagramas e regras: [ARQUITETURA.md](ARQUITETURA.md).

## Como testar

### Opção 1: testes automáticos (sem abrir o jogo)
1. Instale o [Unity Hub](https://unity.com/download) e um Editor **LTS** (2022.3 ou Unity 6).
2. No Hub: **New project → 3D (Core)** e crie o projeto.
3. Copie a pasta `Assets/_Project` deste repositório para dentro de `Assets/` do projeto novo.
   Ou clone este repositório *dentro* da pasta do projeto, de forma que o caminho fique `Assets/_Project`.
4. No Unity: **Window → General → Test Runner → EditMode → Run All**.

### Opção 2: jogar pelo Console (protótipo)
Ainda não há interface gráfica: a partida roda e é narrada no **Console**.

1. Crie um GameObject vazio na cena e adicione o componente **SovereignBootstrapper**.
2. (Opcional) Crie os assets em **Create → Sovereign of Ashes** (Recipe Book, cartas, perfis) e arraste-os no componente.
   Sem nenhum asset, a partida já roda com valores padrão (Fé, turnos e invasores).
3. Aperte **Play**. O Console mostra o turno 1.
4. Clique com o botão direito no componente (ou nos ⋮ do Inspector):
   - **Jogar Carta Demo**: joga a carta do campo `Demo Card` (precisa de Recipe Book + carta).
   - **Encerrar Turno**: rotação, civilizações, marcha dos invasores, novas ondas e julgamento.

## Estrutura

```
Assets/_Project/
  Scripts/   lógica em C# puro + ScriptableObjects + bootstrapper
  Tests/     testes EditMode (NUnit)
```
