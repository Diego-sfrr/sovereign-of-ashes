using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SovereignOfAshes.Alchemy;
using SovereignOfAshes.Board;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Civilization;
using SovereignOfAshes.Core;
using SovereignOfAshes.Economy;
using SovereignOfAshes.Invaders;
using SovereignOfAshes.Rules;
using SovereignOfAshes.Rules.Combat;
using SovereignOfAshes.Rules.Commandments;
using SovereignOfAshes.Session;
using SovereignOfAshes.Turns;
using SovereignOfAshes.Turns.Phases;

namespace SovereignOfAshes.Tests
{
    /// <summary>
    /// Testes de lógica pura: rodam no Test Runner (EditMode) sem cena e sem MonoBehaviour.
    /// </summary>
    public sealed class CoreSystemsTests
    {
        // ---------- Tabuleiro ----------

        [Test]
        public void ModularMath_Wrap_HandlesNegativeValues()
        {
            Assert.AreEqual(7, ModularMath.Wrap(-1, 8));
            Assert.AreEqual(0, ModularMath.Wrap(16, 8));
        }

        [Test]
        public void GetNeighbors_WrapsAroundLaterally()
        {
            var board = new TectonicBoard(8);
            TectonicSector first = board.GetSectorAt(RingType.MiddleCivilization, 0);

            SectorNeighbors neighbors = board.GetNeighbors(first);

            Assert.AreEqual(1, board.GetPositionOf(neighbors.Clockwise));
            Assert.AreEqual(7, board.GetPositionOf(neighbors.CounterClockwise));
            Assert.AreEqual(4, neighbors.All.Count);
        }

        [Test]
        public void GetNeighbors_InnerRingTouchesCoreAndHasNoInnerNeighbor()
        {
            var board = new TectonicBoard(6);
            SectorNeighbors neighbors = board.GetNeighbors(board.GetSectorAt(RingType.InnerSanctuary, 2));

            Assert.IsTrue(neighbors.IsAdjacentToCore);
            Assert.IsNull(neighbors.Inner);
            Assert.IsNotNull(neighbors.Outer);
        }

        [Test]
        public void RotateRing_ChangesRadialAlignment()
        {
            var board = new TectonicBoard(8);
            TectonicSector middle = board.GetSectorAt(RingType.MiddleCivilization, 0);
            TectonicSector outerPlate6 = board.GetRing(RingType.OuterWilderness).Sectors[6];

            board.RotateRing(RingType.OuterWilderness, 2, RotationDirection.Clockwise);

            // A placa 6 do anel externo agora está na posição (6 + 2) mod 8 = 0, alinhada com o setor do meio.
            Assert.AreSame(outerPlate6, board.GetNeighbors(middle).Outer);
        }

        [Test]
        public void RotateRing_CounterClockwiseUndoesClockwise()
        {
            var board = new TectonicBoard(5);
            TectonicSector sector = board.GetSectorAt(RingType.InnerSanctuary, 3);

            board.RotateRing(RingType.InnerSanctuary, 7, RotationDirection.Clockwise);
            board.RotateRing(RingType.InnerSanctuary, 7, RotationDirection.CounterClockwise);

            Assert.AreEqual(3, board.GetPositionOf(sector));
        }

        // ---------- Alquimia ----------

        private sealed class FakeElement : IElementDefinition
        {
            public FakeElement(string id) { Id = id; }
            public string Id { get; }
            public string DisplayName => Id;
            public ElementKind Kind => ElementKind.Concept;
        }

        private sealed class FakeRecipe : IAlchemyRecipe
        {
            public IElementDefinition Reagent { get; set; }
            public ConceptType Catalyst { get; set; }
            public IElementDefinition Result { get; set; }
        }

        private sealed class FakeProvider : IAlchemyRecipeProvider
        {
            public readonly Dictionary<ConceptType, IElementDefinition> Bases = new Dictionary<ConceptType, IElementDefinition>();
            public readonly List<IAlchemyRecipe> Recipes = new List<IAlchemyRecipe>();

            public bool TryGetBaseElement(ConceptType concept, out IElementDefinition element) =>
                Bases.TryGetValue(concept, out element);

            public bool TryGetRecipe(IElementDefinition reagent, ConceptType catalyst, out IAlchemyRecipe recipe)
            {
                recipe = Recipes.FirstOrDefault(r => r.Reagent.Id == reagent.Id && r.Catalyst == catalyst);
                return recipe != null;
            }
        }

        [Test]
        public void Alchemy_ManifestsThenTransmutes()
        {
            var iron = new FakeElement("Iron");
            var forge = new FakeElement("SacredForge");
            var provider = new FakeProvider();
            provider.Bases[ConceptType.Iron] = iron;
            provider.Recipes.Add(new FakeRecipe { Reagent = iron, Catalyst = ConceptType.Fire, Result = forge });

            var alchemy = new AlchemySystem(provider, new DefaultElementFactory());
            TectonicSector sector = new TectonicBoard(3).GetSectorAt(RingType.MiddleCivilization, 0);

            Assert.AreEqual(AlchemyOutcome.Manifested, alchemy.ApplyConcept(sector, ConceptType.Iron).Outcome);
            AlchemyResult fusion = alchemy.ApplyConcept(sector, ConceptType.Fire);

            Assert.AreEqual(AlchemyOutcome.Transmuted, fusion.Outcome);
            Assert.AreSame(forge, sector.Content.Definition);
            Assert.AreEqual(1, ((SectorElement)sector.Content).FusionDepth);
            Assert.AreEqual(AlchemyOutcome.NoReaction, alchemy.ApplyConcept(sector, ConceptType.Fear).Outcome);
        }

        // ---------- Cartas ----------

        private sealed class FakeCard : ICardDefinition
        {
            public string Id => "fake";
            public string DisplayName => "Fake";
            public string Description => "Carta de teste.";
            public int Cost { get; set; } = 1;
            public ConceptType PrimaryConcept => ConceptType.Iron;
            public TargetRings AllowedRings { get; set; } = TargetRings.All;
            public bool RequiresEmptySector { get; set; }
            public bool RequiresInvader { get; set; }
            public IReadOnlyList<ITileEffect> Effects { get; set; } = new List<ITileEffect>();
        }

        private static (TectonicBoard board, CardResolver resolver) CreateCardSetup(FaithPool wallet = null)
        {
            var (board, resolver, _) = CreateCardSetupWithInvaders(wallet);
            return (board, resolver);
        }

        private static (TectonicBoard board, CardResolver resolver, InvaderRoster invaders) CreateCardSetupWithInvaders(
            FaithPool wallet = null, RuleModifierSystem rules = null)
        {
            var iron = new FakeElement("Iron");
            var provider = new FakeProvider();
            provider.Bases[ConceptType.Iron] = iron;
            var board = new TectonicBoard(6);
            var alchemy = new AlchemySystem(provider, new DefaultElementFactory());
            var invaders = new InvaderRoster(board);
            var combat = new CombatResolver(rules ?? new RuleModifierSystem());
            var services = new CardPlayServices(board, alchemy, invaders, combat,
                new Combatant("Core", 30, 0), new List<CivilizationEntity>());
            var resolver = new CardResolver(services, wallet ?? new FaithPool(99, 99));
            return (board, resolver, invaders);
        }

        // ---------- Baralho, cartas globais e sessão ----------

        [Test]
        public void CardDeck_DrawsDiscardsAndReshuffles()
        {
            var cards = Enumerable.Range(0, 7).Select(i => (ICardDefinition)new FakeCard()).ToList();
            var deck = new CardDeck(cards, handSize: 5, random: new System.Random(1));

            Assert.AreEqual(5, deck.DrawUpToHandSize());
            Assert.AreEqual(2, deck.DrawPileCount);

            deck.DiscardFromHand(0);
            Assert.AreEqual(4, deck.Hand.Count);
            Assert.AreEqual(1, deck.DiscardPileCount);

            deck.DiscardHand();
            Assert.AreEqual(5, deck.DiscardPileCount);

            Assert.AreEqual(5, deck.DrawUpToHandSize()); // 2 da pilha + reembaralha o descarte
            Assert.AreEqual(7, deck.TotalCount);
        }

        [Test]
        public void GlobalCards_NeedNoTarget_AndAffectCivilizationsAndCore()
        {
            var board = new TectonicBoard(4);
            var rules = new RuleModifierSystem();
            var core = new Combatant("Core", 30, 0);
            core.TakeDamage(10);
            var civ = new CivilizationEntity("Civ", new CivilizationStatsConfig(5, 10, 6, 20, 10, 3));
            var services = new CardPlayServices(board, new AlchemySystem(new InMemoryRecipeProvider(), new DefaultElementFactory()),
                new InvaderRoster(board), new CombatResolver(rules), core, new List<CivilizationEntity> { civ });
            var resolver = new CardResolver(services, new FaithPool(10, 10));

            var sermon = new SimpleCardDefinition("s", "Sermão", "", 1, ConceptType.Faith, TargetRings.None, false, false,
                new ITileEffect[] { new CivilizationInfluenceEffect(-3, 0, 0) });
            var miracle = new SimpleCardDefinition("m", "Milagre", "", 2, ConceptType.Faith, TargetRings.None, false, false,
                new ITileEffect[] { new HealCoreEffect(6) });
            var spear = new SimpleCardDefinition("l", "Lança", "", 1, ConceptType.Fire, TargetRings.All, false, true,
                new ITileEffect[] { new InvaderStrikeEffect(4, false) });

            Assert.AreEqual(CardPlayOutcome.Played, resolver.Play(sermon, null));
            Assert.AreEqual(3, civ.Rationality);
            Assert.AreEqual(CardPlayOutcome.Played, resolver.Play(miracle, null));
            Assert.AreEqual(26, core.CurrentHealth);
            Assert.AreEqual(CardPlayOutcome.TargetRequired, resolver.Play(spear, null));
        }

        [Test]
        public void PlayerActions_RespectTurnAndMovePlayedCardToDiscard()
        {
            GameSessionConfig config = DefaultContent.CreateConfig();
            config.Seed = 123;
            config.StartingDeck.Clear();
            config.StartingDeck.AddRange(Enumerable.Repeat<ICardDefinition>(
                new SimpleCardDefinition("s", "Sermão", "", 1, ConceptType.Faith, TargetRings.None, false, false,
                    new ITileEffect[] { new CivilizationInfluenceEffect(-1, 0, 0) }), 6));

            using (var session = new GameSession(config))
            {
                Assert.IsFalse(session.Player.CanAct); // partida ainda não começou
                Assert.IsFalse(session.Player.EndTurn());

                session.Start();
                Assert.IsTrue(session.Player.CanAct);
                Assert.AreEqual(5, session.Deck.Hand.Count);

                int faithBefore = session.Faith.Current;
                Assert.AreEqual(CardPlayOutcome.Played, session.Player.PlayFromHand(0, null));
                Assert.AreEqual(4, session.Deck.Hand.Count);
                Assert.AreEqual(1, session.Deck.DiscardPileCount);
                Assert.AreEqual(faithBefore - 1, session.Faith.Current);

                Assert.IsTrue(session.Player.EndTurn());
                Assert.AreEqual(2, session.Turns.TurnNumber);
                Assert.AreEqual(5, session.Deck.Hand.Count);
            }
        }

        [Test]
        public void DefaultSession_AutoPlayedGame_ReachesGameOverWithoutErrors()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                GameSessionConfig config = DefaultContent.CreateConfig();
                config.Seed = seed;

                using (var session = new GameSession(config))
                {
                    session.Start();
                    int guard = 0;

                    while (session.Turns.State == TurnState.AwaitingPlayer && guard++ < 100)
                    {
                        AutoPlayTurn(session);
                        session.Player.EndTurn();
                    }

                    Assert.AreEqual(TurnState.GameOver, session.Turns.State, $"semente {seed}");
                    Assert.IsTrue(session.Log.Entries.Count > 0);
                }
            }
        }

        private static void AutoPlayTurn(GameSession session)
        {
            // Joga, enquanto possível, cada carta da mão no primeiro alvo válido (ou sem alvo, se global).
            bool playedSomething = true;

            while (playedSomething)
            {
                playedSomething = false;

                for (int i = 0; i < session.Deck.Hand.Count && !playedSomething; i++)
                {
                    if (session.Deck.Hand[i].IsGlobal())
                    {
                        playedSomething = session.Player.PlayFromHand(i, null) == CardPlayOutcome.Played;
                        continue;
                    }

                    foreach (TectonicSector sector in session.Board.GetAllSectors())
                    {
                        if (session.Player.Preview(i, sector) == CardPlayOutcome.Played)
                        {
                            playedSomething = session.Player.PlayFromHand(i, sector) == CardPlayOutcome.Played;
                            break;
                        }
                    }
                }
            }
        }

        [Test]
        public void PixelSprites_AllRasterize_AndCatalogReferencesExist()
        {
            int count = 0;

            foreach (var sprite in SovereignOfAshes.Presentation.Art.PixelSprites.All)
            {
                var pixels = SovereignOfAshes.Presentation.Art.PixelGrid.Rasterize(sprite, SovereignOfAshes.Presentation.Art.PixelPalette.Default);
                Assert.AreEqual(16, sprite.Width, sprite.Id);
                Assert.AreEqual(16, sprite.Height, sprite.Id);
                Assert.AreEqual(256, pixels.Length, sprite.Id);
                count++;
            }

            Assert.Greater(count, 20);

            foreach (string id in SovereignOfAshes.Presentation.Art.ArtCatalog.ReferencedSprites)
            {
                Assert.IsTrue(SovereignOfAshes.Presentation.Art.PixelSprites.TryGet(id, out _), $"sprite ausente: {id}");
            }
        }

        [Test]
        public void CaptureHarness_ReadsDirectoryOnlyWhenRequested()
        {
            Assert.IsNull(SovereignOfAshes.Presentation.SovereignCaptureHarness.ReadDirectory(new[] { "game.exe" }));
            Assert.IsNull(SovereignOfAshes.Presentation.SovereignCaptureHarness.ReadDirectory(new[] { "game.exe", "-sovereign-capture" }));
            Assert.AreEqual(@"C:\shots", SovereignOfAshes.Presentation.SovereignCaptureHarness.ReadDirectory(
                new[] { "game.exe", "-screen-width", "1600", "-sovereign-capture", @"C:\shots" }));
        }

        [Test]
        public void PixelGrid_RasterizesBottomUp()
        {
            var sprite = new SovereignOfAshes.Presentation.Art.PixelSpriteDefinition("t", "k.", "..");
            var pixels = SovereignOfAshes.Presentation.Art.PixelGrid.Rasterize(sprite, SovereignOfAshes.Presentation.Art.PixelPalette.Default);

            Assert.AreEqual(255, pixels[2].A); // fileira de cima ("k.") vira y = 1 no Unity
            Assert.AreEqual(0, pixels[0].A);
        }

        [Test]
        public void BoardLayout_PicksRingAndPositionClockwiseFromTop()
        {
            var layout = new SovereignOfAshes.Presentation.BoardLayout(8, 3);

            // Topo do anel do meio = posição 0.
            float radius = layout.GetCenterRadius(1);
            Assert.IsTrue(layout.TryPick(0f, radius, out int ring, out int position));
            Assert.AreEqual(1, ring);
            Assert.AreEqual(0, position);

            // Direita = 90° no sentido horário = posição 2 (de 8).
            Assert.IsTrue(layout.TryPick(layout.GetCenterRadius(2), 0f, out ring, out position));
            Assert.AreEqual(2, ring);
            Assert.AreEqual(2, position);

            // Ida e volta: o centro de cada posição é "pickado" de volta na mesma posição.
            for (int p = 0; p < 8; p++)
            {
                SovereignOfAshes.Presentation.BoardLayout.PolarToCartesian(layout.GetCenterRadius(0), layout.GetAngleDegrees(p), out float x, out float y);
                Assert.IsTrue(layout.TryPick(x, y, out ring, out position));
                Assert.AreEqual(0, ring);
                Assert.AreEqual(p, position);
            }

            Assert.IsFalse(layout.TryPick(0f, 0f, out _, out _)); // núcleo
            Assert.IsFalse(layout.TryPick(0f, layout.OuterEdge + 1f, out _, out _)); // fora
        }

        [Test]
        public void StrikeCard_DamagesInvaderOnTarget_AndRequiresInvader()
        {
            var wallet = new FaithPool(10, 10);
            var (board, resolver, invaders) = CreateCardSetupWithInvaders(wallet);
            var invaderDef = new SimpleInvaderDefinition("brute", "Bruto", maxHealth: 10, attack: 3, faithBounty: 0);
            TectonicSector occupied = board.GetSectorAt(RingType.OuterWilderness, 1);
            invaders.TrySpawn(invaderDef, occupied, out InvaderEntity brute);

            var card = new FakeCard
            {
                Cost = 2,
                RequiresInvader = true,
                Effects = new List<ITileEffect> { new InvaderStrikeEffect(4, includeNeighbors: false) }
            };

            TectonicSector empty = board.GetSectorAt(RingType.OuterWilderness, 4);
            Assert.AreEqual(CardPlayOutcome.NoInvaderOnTarget, resolver.Play(card, empty));
            Assert.AreEqual(10, wallet.Current);

            Assert.AreEqual(CardPlayOutcome.Played, resolver.Play(card, occupied));
            Assert.AreEqual(6, brute.CurrentHealth);
            Assert.AreEqual(8, wallet.Current);
        }

        [Test]
        public void StrikeCard_WithNeighbors_KillsAndGrantsBounty()
        {
            var wallet = new FaithPool(0, 50);
            var config = new FaithEconomyConfig(0, 50, 0, 0);
            var rules = new RuleModifierSystem();
            var (board, resolver, invaders) = CreateCardSetupWithInvaders(wallet, rules);
            var bounty = new InvaderBountyRewarder(invaders, new FaithIncomeService(wallet, config, rules));
            var weakling = new SimpleInvaderDefinition("imp", "Diabrete", maxHealth: 3, attack: 1, faithBounty: 2);

            TectonicSector center = board.GetSectorAt(RingType.MiddleCivilization, 2);
            invaders.TrySpawn(weakling, center, out _);
            invaders.TrySpawn(weakling, board.GetSectorAt(RingType.MiddleCivilization, 3), out _); // lateral
            invaders.TrySpawn(weakling, board.GetSectorAt(RingType.OuterWilderness, 2), out _);    // radial externo
            invaders.TrySpawn(weakling, board.GetSectorAt(RingType.OuterWilderness, 5), out InvaderEntity far);

            var card = new FakeCard
            {
                Cost = 0,
                Effects = new List<ITileEffect> { new InvaderStrikeEffect(3, includeNeighbors: true) }
            };

            Assert.AreEqual(CardPlayOutcome.Played, resolver.Play(card, center));

            Assert.AreEqual(1, invaders.Count);
            Assert.AreSame(far, invaders.Invaders[0]);
            Assert.AreEqual(6, wallet.Current); // 3 abates x 2 de Fé

            bounty.Dispose();
        }

        [Test]
        public void StrikeCard_DamageGoesThroughCommandments()
        {
            var rules = new RuleModifierSystem();
            rules.AddCommandment(new DoubleDamageCommandment());
            var (board, resolver, invaders) = CreateCardSetupWithInvaders(rules: rules);
            var def = new SimpleInvaderDefinition("brute", "Bruto", maxHealth: 10, attack: 0, faithBounty: 0);
            TectonicSector sector = board.GetSectorAt(RingType.OuterWilderness, 0);
            invaders.TrySpawn(def, sector, out InvaderEntity brute);

            var card = new FakeCard
            {
                Cost = 0,
                Effects = new List<ITileEffect> { new InvaderStrikeEffect(3, includeNeighbors: false) }
            };
            resolver.Play(card, sector);

            Assert.AreEqual(4, brute.CurrentHealth); // 3 x 2 = 6 de dano
        }

        private sealed class DoubleDamageCommandment : CommandmentBase
        {
            public DoubleDamageCommandment() : base("double", "Ira Dobrada", 0) { }

            public override bool AppliesTo(RuleContext context) =>
                context.Channel == RuleChannel.Damage && context.Source is ICardDefinition;

            public override void Apply(RuleContext context) => context.SetAmount(context.Amount * 2);
        }

        [Test]
        public void CardResolver_ChargesFaithOnlyWhenPlayed()
        {
            var wallet = new FaithPool(3, 10);
            var (board, resolver) = CreateCardSetup(wallet);
            TectonicSector sector = board.GetSectorAt(RingType.MiddleCivilization, 0);
            var card = new FakeCard
            {
                Cost = 2,
                RequiresEmptySector = true,
                Effects = new List<ITileEffect> { new ConceptInfusionEffect(ConceptType.Iron) }
            };

            Assert.AreEqual(CardPlayOutcome.Played, resolver.Play(card, sector));
            Assert.AreEqual(1, wallet.Current);

            // Alvo inválido: não cobra.
            Assert.AreEqual(CardPlayOutcome.SectorOccupied, resolver.Play(card, sector));
            Assert.AreEqual(1, wallet.Current);

            // Sem Fé: não executa nem cobra.
            TectonicSector other = board.GetSectorAt(RingType.MiddleCivilization, 3);
            Assert.AreEqual(CardPlayOutcome.InsufficientFaith, resolver.Play(card, other));
            Assert.IsTrue(other.IsEmpty);
            Assert.AreEqual(1, wallet.Current);
        }

        // ---------- Fé ----------

        [Test]
        public void FaithPool_ClampsToCapacityAndRejectsOverspend()
        {
            var pool = new FaithPool(8, 10);

            Assert.AreEqual(2, pool.Add(5));
            Assert.AreEqual(10, pool.Current);
            Assert.IsFalse(pool.TrySpend(11));
            Assert.IsTrue(pool.TrySpend(4));
            Assert.AreEqual(6, pool.Current);

            pool.SetCapacity(5);
            Assert.AreEqual(5, pool.Current);
        }

        [Test]
        public void FaithIncome_HereticsYieldNothing_AndCommandmentsModifyIncome()
        {
            var config = new FaithEconomyConfig(startingFaith: 0, capacity: 100, baseIncomePerTurn: 2, civilizationYieldPercent: 50);
            var pool = config.CreatePool();
            var rules = new RuleModifierSystem();
            var income = new FaithIncomeService(pool, config, rules);

            var devout = new CivilizationEntity("Devotos", new CivilizationStatsConfig(1, 10, 0, 20, 10, 3));
            var heretic = new CivilizationEntity("Hereges", new CivilizationStatsConfig(1, 10, 10, 20, 10, 3));
            var civs = new[] { devout, heretic };

            FaithIncomeReport first = income.CollectTurnIncome(civs);
            Assert.AreEqual(7, first.GrossIncome); // 2 base + 5 devotos + 0 hereges
            Assert.AreEqual(7, pool.Current);

            rules.AddCommandment(new FaithMultiplierCommandment("tithe", "Dízimo", 0, 200));
            FaithIncomeReport second = income.CollectTurnIncome(civs);
            Assert.AreEqual(7, second.GrossIncome);
            Assert.AreEqual(14, second.ModifiedIncome); // 4 + 10
            Assert.AreEqual(21, pool.Current);
        }

        [Test]
        public void FaithIncome_ReportsOverflow()
        {
            var config = new FaithEconomyConfig(9, 10, 5, 0);
            var pool = config.CreatePool();
            var income = new FaithIncomeService(pool, config, new RuleModifierSystem());

            FaithIncomeReport report = income.CollectTurnIncome(null);

            Assert.AreEqual(1, report.Deposited);
            Assert.AreEqual(4, report.Overflow);
        }

        [Test]
        public void TargetRings_IncludesOnlyFlaggedRings()
        {
            TargetRings mask = TargetRings.InnerSanctuary | TargetRings.OuterWilderness;

            Assert.IsTrue(mask.Includes(RingType.InnerSanctuary));
            Assert.IsFalse(mask.Includes(RingType.MiddleCivilization));
            Assert.IsTrue(TargetRings.All.Includes(RingType.MiddleCivilization));
            Assert.IsFalse(TargetRings.None.Includes(RingType.InnerSanctuary));
        }

        [Test]
        public void CardResolver_RejectsWrongRingAndOccupiedSector()
        {
            var (board, resolver) = CreateCardSetup();
            TectonicSector middle = board.GetSectorAt(RingType.MiddleCivilization, 0);

            var outerOnly = new FakeCard { AllowedRings = TargetRings.OuterWilderness };
            Assert.AreEqual(CardPlayOutcome.InvalidRing, resolver.Play(outerOnly, middle));

            var emptyOnly = new FakeCard
            {
                RequiresEmptySector = true,
                Effects = new List<ITileEffect> { new ConceptInfusionEffect(ConceptType.Iron) }
            };
            Assert.AreEqual(CardPlayOutcome.Played, resolver.Play(emptyOnly, middle));
            Assert.AreEqual(CardPlayOutcome.SectorOccupied, resolver.Play(emptyOnly, middle));
        }

        [Test]
        public void CardResolver_NeighborInfusionHitsAllNeighbors()
        {
            var (board, resolver) = CreateCardSetup();
            TectonicSector middle = board.GetSectorAt(RingType.MiddleCivilization, 2);
            var card = new FakeCard
            {
                Effects = new List<ITileEffect> { new NeighborInfusionEffect(ConceptType.Iron, includeTarget: true) }
            };

            resolver.Play(card, middle);

            Assert.IsFalse(middle.IsEmpty);
            Assert.IsTrue(board.GetNeighbors(middle).All.All(s => !s.IsEmpty));
            Assert.AreEqual(5, board.GetAllSectors().Count(s => !s.IsEmpty));
        }

        // ---------- Civilização ----------

        [Test]
        public void Civilization_BecomesHereticAtMaxRationality_AndRedeemsBelowThreshold()
        {
            var civ = new CivilizationEntity("Teste", new CivilizationStatsConfig(5, 10, 0, 20, 10, 3));
            int heresyEvents = 0;
            civ.HeresyDeclared += _ => heresyEvents++;

            civ.ChangeRationality(9);
            Assert.AreEqual(AttackTarget.ExternalInvaders, civ.GetAttackTarget());

            civ.ChangeRationality(50); // limitado ao teto
            Assert.AreEqual(10, civ.Rationality);
            Assert.IsTrue(civ.IsHeretic);
            Assert.AreEqual(AttackTarget.DivineCore, civ.GetAttackTarget());
            Assert.AreEqual(1, heresyEvents);

            civ.ChangeRationality(-5); // 5: ainda acima do limiar (histerese)
            Assert.IsTrue(civ.IsHeretic);

            civ.ChangeRationality(-2); // 3: redimida
            Assert.IsFalse(civ.IsHeretic);
        }

        // ---------- Turnos ----------

        private sealed class RecordingPhase : ITurnPhase
        {
            private readonly System.Func<int, PhaseResult> _behaviour;
            public readonly List<int> ExecutedOnTurns = new List<int>();

            public RecordingPhase(string name, System.Func<int, PhaseResult> behaviour)
            {
                Name = name;
                _behaviour = behaviour;
            }

            public string Name { get; }

            public PhaseResult Execute(int turnNumber)
            {
                ExecutedOnTurns.Add(turnNumber);
                return _behaviour(turnNumber);
            }
        }

        [Test]
        public void TurnSystem_StopsAtPlayerPhaseAndAdvancesTurns()
        {
            var before = new RecordingPhase("antes", _ => PhaseResult.Continue);
            var after = new RecordingPhase("depois", _ => PhaseResult.Continue);
            var turns = new TurnSystem(new ITurnPhase[] { before, new PlayerActionPhase(), after });
            var started = new List<int>();
            var ended = new List<int>();
            turns.TurnStarted += started.Add;
            turns.TurnEnded += ended.Add;

            turns.StartGame();

            Assert.AreEqual(TurnState.AwaitingPlayer, turns.State);
            Assert.AreEqual(1, turns.TurnNumber);
            CollectionAssert.AreEqual(new[] { 1 }, before.ExecutedOnTurns);
            Assert.IsEmpty(after.ExecutedOnTurns);

            turns.EndPlayerTurn();

            Assert.AreEqual(TurnState.AwaitingPlayer, turns.State);
            Assert.AreEqual(2, turns.TurnNumber);
            CollectionAssert.AreEqual(new[] { 1 }, after.ExecutedOnTurns);
            CollectionAssert.AreEqual(new[] { 1, 2 }, before.ExecutedOnTurns);
            CollectionAssert.AreEqual(new[] { 1, 2 }, started);
            CollectionAssert.AreEqual(new[] { 1 }, ended);
        }

        [Test]
        public void TurnSystem_RejectsInvalidCalls()
        {
            var turns = new TurnSystem(new ITurnPhase[] { new PlayerActionPhase() });

            Assert.Throws<System.InvalidOperationException>(() => turns.EndPlayerTurn());
            turns.StartGame();
            Assert.Throws<System.InvalidOperationException>(() => turns.StartGame());
        }

        [Test]
        public void TurnSystem_WithoutPlayerPhase_ThrowsInsteadOfLooping()
        {
            var turns = new TurnSystem(new ITurnPhase[] { new RecordingPhase("auto", _ => PhaseResult.Continue) });

            Assert.Throws<System.InvalidOperationException>(() => turns.StartGame());
        }

        [Test]
        public void GameOverCheck_DeclaresDefeatAndVictory()
        {
            var core = new Combatant("Core", 10, 0);
            var defeatTurns = new TurnSystem(new ITurnPhase[] { new PlayerActionPhase(), new GameOverCheckPhase(core, 0) });
            GameOverInfo info = null;
            defeatTurns.GameOver += i => info = i;

            defeatTurns.StartGame();
            core.TakeDamage(10);
            defeatTurns.EndPlayerTurn();

            Assert.AreEqual(TurnState.GameOver, defeatTurns.State);
            Assert.AreEqual(GameOutcome.Defeat, info.Outcome);
            Assert.Throws<System.InvalidOperationException>(() => defeatTurns.EndPlayerTurn());

            var victoryTurns = new TurnSystem(new ITurnPhase[]
            {
                new PlayerActionPhase(),
                new GameOverCheckPhase(new Combatant("Core", 10, 0), 3)
            });
            victoryTurns.StartGame();
            victoryTurns.EndPlayerTurn();
            victoryTurns.EndPlayerTurn();
            Assert.AreEqual(TurnState.AwaitingPlayer, victoryTurns.State);
            victoryTurns.EndPlayerTurn();

            Assert.AreEqual(TurnState.GameOver, victoryTurns.State);
            Assert.AreEqual(GameOutcome.Victory, victoryTurns.Result.Outcome);
            Assert.AreEqual(3, victoryTurns.Result.TurnNumber);
        }

        [Test]
        public void CivilizationPhase_HereticsDamageCoreThroughCombatResolver()
        {
            var core = new Combatant("Core", 30, 0);
            var devout = new CivilizationEntity("Devotos", new CivilizationStatsConfig(4, 10, 0, 20, 10, 3));
            var rebels = new CivilizationEntity("Rebeldes", new CivilizationStatsConfig(6, 10, 9, 20, 10, 3));
            var civs = new List<CivilizationEntity> { devout, rebels };
            var phase = new CivilizationPhase(civs, new CombatResolver(new RuleModifierSystem()), core,
                new InvaderRoster(new TectonicBoard(3)), rationalityGrowthPerTurn: 1);

            phase.Execute(1); // Rebeldes chegam a 10 de Racionalidade -> hereges -> atacam com 6.

            Assert.IsTrue(rebels.IsHeretic);
            Assert.IsFalse(devout.IsHeretic);
            Assert.AreEqual(24, core.CurrentHealth);
        }

        [Test]
        public void PatternPlanner_RespectsInterval()
        {
            var planner = new PatternTectonicShiftPlanner(new[]
            {
                new RingShiftRule(new RingRotation(RingType.InnerSanctuary, 2, RotationDirection.CounterClockwise), 3),
                new RingShiftRule(new RingRotation(RingType.OuterWilderness, 1, RotationDirection.Clockwise), 1)
            });

            Assert.AreEqual(1, planner.PlanShifts(1).Count);
            Assert.AreEqual(1, planner.PlanShifts(2).Count);
            Assert.AreEqual(2, planner.PlanShifts(3).Count);
            Assert.AreEqual(2, planner.PlanShifts(6).Count);
        }

        [Test]
        public void FullTurn_RotatesBoardAndCollectsIncome()
        {
            var board = new TectonicBoard(8);
            var config = new FaithEconomyConfig(0, 50, 2, 0);
            var pool = config.CreatePool();
            var rules = new RuleModifierSystem();
            var core = new Combatant("Core", 30, 0);
            var civs = new List<CivilizationEntity>();
            var planner = new PatternTectonicShiftPlanner(new[]
            {
                new RingShiftRule(new RingRotation(RingType.OuterWilderness, 1, RotationDirection.Clockwise), 1)
            });

            var turns = new TurnSystem(new ITurnPhase[]
            {
                new FaithIncomePhase(new FaithIncomeService(pool, config, rules), civs),
                new PlayerActionPhase(),
                new TectonicShiftPhase(board, planner),
                new CivilizationPhase(civs, new CombatResolver(rules), core, new InvaderRoster(board), 1),
                new GameOverCheckPhase(core, 0)
            });

            turns.StartGame();
            Assert.AreEqual(2, pool.Current);
            Assert.AreEqual(0, board.GetRing(RingType.OuterWilderness).RotationOffset);

            turns.EndPlayerTurn();
            Assert.AreEqual(1, board.GetRing(RingType.OuterWilderness).RotationOffset);
            Assert.AreEqual(4, pool.Current); // renda do turno 2 já entrou
            Assert.AreEqual(2, turns.TurnNumber);
        }

        // ---------- Invasores ----------

        private static readonly SimpleInvaderDefinition Cultist =
            new SimpleInvaderDefinition("cultist", "Cultista", maxHealth: 5, attack: 2, faithBounty: 3);

        [Test]
        public void InvaderAdvance_MarchesInwardAlongAlignedSectorsAndAttacksCore()
        {
            var board = new TectonicBoard(8);
            var roster = new InvaderRoster(board);
            var core = new Combatant("Core", 30, 0);
            var phase = new InvaderAdvancePhase(roster, board, new CombatResolver(new RuleModifierSystem()), core);

            board.RotateRing(RingType.MiddleCivilization, 3, RotationDirection.Clockwise);
            roster.TrySpawn(Cultist, board.GetSectorAt(RingType.OuterWilderness, 2), out InvaderEntity invader);

            phase.Execute(1);
            Assert.AreEqual(RingType.MiddleCivilization, invader.Sector.Ring);
            Assert.AreEqual(2, board.GetPositionOf(invader.Sector)); // alinhamento radial: mesma posição angular

            phase.Execute(2);
            Assert.AreEqual(RingType.InnerSanctuary, invader.Sector.Ring);
            Assert.AreEqual(30, core.CurrentHealth);

            phase.Execute(3);
            Assert.AreEqual(28, core.CurrentHealth);
            Assert.AreEqual(RingType.InnerSanctuary, invader.Sector.Ring);
        }

        [Test]
        public void InvaderAdvance_BlockedByOccupiedSector_ButQueueMovesInnermostFirst()
        {
            var board = new TectonicBoard(6);
            var roster = new InvaderRoster(board);
            var phase = new InvaderAdvancePhase(roster, board, new CombatResolver(new RuleModifierSystem()), new Combatant("Core", 30, 0));

            roster.TrySpawn(Cultist, board.GetSectorAt(RingType.MiddleCivilization, 0), out InvaderEntity front);
            roster.TrySpawn(Cultist, board.GetSectorAt(RingType.OuterWilderness, 0), out InvaderEntity back);

            phase.Execute(1); // o da frente entra no Santuário e libera o caminho para o de trás

            Assert.AreEqual(RingType.InnerSanctuary, front.Sector.Ring);
            Assert.AreEqual(RingType.MiddleCivilization, back.Sector.Ring);

            phase.Execute(2); // o da frente fica atacando; o de trás está bloqueado por ele

            Assert.AreEqual(RingType.MiddleCivilization, back.Sector.Ring);
        }

        [Test]
        public void DevoutCivilization_KillsPriorityInvader_AndBountyGrantsFaith()
        {
            var board = new TectonicBoard(6);
            var roster = new InvaderRoster(board);
            var config = new FaithEconomyConfig(0, 50, 0, 0);
            var pool = config.CreatePool();
            var rules = new RuleModifierSystem();
            var bounty = new InvaderBountyRewarder(roster, new FaithIncomeService(pool, config, rules));

            roster.TrySpawn(Cultist, board.GetSectorAt(RingType.OuterWilderness, 0), out InvaderEntity far);
            roster.TrySpawn(Cultist, board.GetSectorAt(RingType.MiddleCivilization, 3), out InvaderEntity near);

            var civ = new CivilizationEntity("Devotos", new CivilizationStatsConfig(5, 10, 0, 20, 10, 3));
            var phase = new CivilizationPhase(new List<CivilizationEntity> { civ }, new CombatResolver(rules),
                new Combatant("Core", 30, 0), roster, 0);

            phase.Execute(1);

            Assert.IsTrue(near.IsDead);
            Assert.IsFalse(far.IsDead);
            Assert.AreEqual(1, roster.Count);
            Assert.AreEqual(3, pool.Current);

            bounty.Dispose();
        }

        [Test]
        public void InvasionWave_RepeatsAndEscalates()
        {
            var wave = new InvasionWave(Cultist, firstTurn: 2, baseCount: 1, everyNTurns: 3, countGrowthPerRepeat: 2);

            Assert.AreEqual(0, wave.CountForTurn(1));
            Assert.AreEqual(1, wave.CountForTurn(2));
            Assert.AreEqual(0, wave.CountForTurn(3));
            Assert.AreEqual(3, wave.CountForTurn(5));
            Assert.AreEqual(5, wave.CountForTurn(8));
        }

        [Test]
        public void InvaderSpawn_FillsFreeOuterSectorsAndReportsOverflow()
        {
            var board = new TectonicBoard(3);
            var roster = new InvaderRoster(board);
            var planner = new WaveInvasionPlanner(new[] { new InvasionWave(Cultist, 1, 5, 0, 0) });
            var phase = new InvaderSpawnPhase(roster, board, planner, new System.Random(42));
            int overflow = 0;
            phase.SpawnOverflow += (_, missing) => overflow = missing;

            phase.Execute(1);

            Assert.AreEqual(3, roster.Count);
            Assert.AreEqual(2, overflow);
            Assert.IsTrue(roster.Invaders.All(i => i.Sector.Ring == RingType.OuterWilderness));
        }

        // ---------- Mandamentos ----------

        [Test]
        public void HealingInflictsDamage_TurnsHealIntoDamage()
        {
            var rules = new RuleModifierSystem();
            rules.AddCommandment(new HealingInflictsDamageCommandment("heal_hurts", "A Cura é Ferida", 10));
            var combat = new CombatResolver(rules);
            var core = new Combatant("Core", 30, 0);

            combat.Heal(core, 8);

            Assert.AreEqual(22, core.CurrentHealth);
        }

        [Test]
        public void OverhealToAttack_ConvertsExcessHealing()
        {
            var rules = new RuleModifierSystem();
            rules.AddCommandment(new OverhealToAttackCommandment("overheal", "Transbordar é Fúria", 0, 2));
            var combat = new CombatResolver(rules);
            var core = new Combatant("Core", 30, 1);
            core.TakeDamage(4);

            combat.Heal(core, 10); // 4 curam, 6 excedem -> +3 ataque

            Assert.AreEqual(30, core.CurrentHealth);
            Assert.AreEqual(4, core.Attack);
        }

        [Test]
        public void Commandments_RespectPriority()
        {
            var rules = new RuleModifierSystem();
            rules.AddCommandment(new OverhealToAttackCommandment("overheal", "Transbordar", 0, 1));
            rules.AddCommandment(new HealingInflictsDamageCommandment("heal_hurts", "Cura é Ferida", 10));
            var combat = new CombatResolver(rules);
            var core = new Combatant("Core", 30, 0);

            combat.Heal(core, 5); // "Cura é Ferida" roda primeiro e zera a cura; "Transbordar" não age.

            Assert.AreEqual(25, core.CurrentHealth);
            Assert.AreEqual(0, core.Attack);
        }
    }
}
