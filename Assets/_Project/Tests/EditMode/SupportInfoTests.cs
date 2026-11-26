using NUnit.Framework;
using SovereignOfAshes.Session;

namespace SovereignOfAshes.Tests
{
    public sealed class SupportInfoTests
    {
        [Test]
        public void DonationUrl_IsAPublicHttpsLink_WithoutSecrets()
        {
            Assert.IsTrue(SupportInfo.DonationUrl.StartsWith("https://livepix.gg/"));
            Assert.IsFalse(SupportInfo.DonationUrl.Contains("?"), "O link não deve carregar parâmetros nem chaves.");
        }

        [Test]
        public void BuildReport_HasGameContext_AndNoPersonalData()
        {
            var settings = new GameSettings { Seed = 1234, CivilizationId = "ash_children" };
            string report = SupportInfo.BuildReport("1.0", settings, 7, "em jogo");

            StringAssert.Contains("Versão: 1.0", report);
            StringAssert.Contains("Semente: 1234", report);
            StringAssert.Contains("Civilização: ash_children", report);
            StringAssert.Contains("Turno: 7 (em jogo)", report);
        }

        [Test]
        public void BuildReport_ToleratesMissingData()
        {
            string report = SupportInfo.BuildReport(null, null, 0, null);
            StringAssert.Contains("Versão: ?", report);
            StringAssert.Contains("Pacto: -", report);
        }

        [Test]
        public void FeedbackUrl_IsEmptyOrHttps()
        {
            Assert.IsTrue(!SupportInfo.HasFeedbackChannel || SupportInfo.FeedbackUrl.StartsWith("https://"));
        }

        [Test]
        public void DisplayUrl_HasNoScheme()
        {
            Assert.AreEqual("livepix.gg/pollus999", SupportInfo.DisplayUrl);
        }
    }
}
