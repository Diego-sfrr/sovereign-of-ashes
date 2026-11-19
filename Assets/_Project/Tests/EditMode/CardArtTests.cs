using NUnit.Framework;
using SovereignOfAshes.Cards;
using SovereignOfAshes.Presentation.Art;
using SovereignOfAshes.Session;
using UnityEngine;

namespace SovereignOfAshes.Tests
{
    /// <summary>Ilustrações pintadas das cartas (Resources/CardArt).</summary>
    public sealed class CardArtTests
    {
        [Test]
        public void CardIllustrationPath_UsesCardIdInsideCardArtFolder()
        {
            Assert.AreEqual("CardArt/fire_spear", ArtCatalog.CardIllustrationPath("fire_spear"));
            Assert.IsNull(ArtCatalog.CardIllustrationPath(null));
            Assert.IsNull(ArtCatalog.CardIllustrationPath("  "));
        }

        [Test]
        public void EveryStarterCard_HasAnIllustration()
        {
            foreach (ICardDefinition card in DefaultContent.CreateStarterDeck())
            {
                var texture = Resources.Load<Texture2D>(ArtCatalog.CardIllustrationPath(card.Id));
                Assert.IsNotNull(texture, $"Falta a ilustração de '{card.Id}' em Resources/CardArt.");
            }
        }

        [Test]
        public void EveryRewardCard_HasAnIllustration()
        {
            foreach (ICardDefinition card in DefaultContent.CreateRewardPool())
            {
                var texture = Resources.Load<Texture2D>(ArtCatalog.CardIllustrationPath(card.Id));
                Assert.IsNotNull(texture, $"Falta a ilustração de '{card.Id}' em Resources/CardArt.");
            }
        }

        [Test]
        public void EveryCivilization_HasItsOwnPortrait()
        {
            foreach (CivilizationDefinition civ in CivilizationCatalog.All)
            {
                var texture = Resources.Load<Texture2D>(ArtCatalog.CivilizationPortraitPath(civ.Id));
                Assert.IsNotNull(texture, $"Falta o retrato de '{civ.Id}' em Resources/Portraits (civ_{civ.Id}).");
            }
        }

        [TestCase("profaner")]
        [TestCase("heretic_herald")]
        [TestCase("inquisitor")]
        public void NewInvaders_HaveAPaintedPortrait(string invaderId)
        {
            var texture = Resources.Load<Texture2D>(ArtCatalog.InvaderPortraitPath(invaderId));
            Assert.IsNotNull(texture, $"Falta o retrato de '{invaderId}' em Resources/Portraits.");
        }
    }
}
