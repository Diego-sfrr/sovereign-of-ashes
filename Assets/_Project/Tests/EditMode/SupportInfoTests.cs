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
        public void DisplayUrl_HasNoScheme()
        {
            Assert.AreEqual("livepix.gg/pollus999", SupportInfo.DisplayUrl);
        }
    }
}
