using NUnit.Framework;
using SovereignOfAshes.Session;

namespace SovereignOfAshes.Tests
{
    public sealed class UpdateManifestTests
    {
        [TestCase("0.3.0", "0.4.0", true)]
        [TestCase("0.3.0", "0.3.1", true)]
        [TestCase("0.9.0", "0.10.0", true)]
        [TestCase("0.3.0", "0.3.0", false)]
        [TestCase("0.4.0", "0.3.9", false)]
        [TestCase("1.0", "1.0.0", false)]
        [TestCase("1.0", "1.0.1", true)]
        [TestCase("abc", "1.0", false)]
        [TestCase("1.0", "", false)]
        [TestCase(null, "1.0", false)]
        public void IsNewer_ComparesNumerically(string current, string latest, bool expected)
        {
            Assert.AreEqual(expected, GameVersion.IsNewer(current, latest));
        }

        [Test]
        public void TryParse_ReadsAValidManifest()
        {
            const string json = "{\n  \"version\": \"0.4.0\",\n  \"url\": \"https://www.mediafire.com/file/x/y.zip/file\",\n  \"notes\": \"Novo chefe\"\n}";

            Assert.IsTrue(UpdateManifest.TryParse(json, out UpdateManifest manifest));
            Assert.AreEqual("0.4.0", manifest.Version);
            Assert.AreEqual("https://www.mediafire.com/file/x/y.zip/file", manifest.Url);
            Assert.AreEqual("Novo chefe", manifest.Notes);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not json")]
        [TestCase("{\"version\": \"0.4.0\"}")]
        [TestCase("{\"url\": \"https://a.b/c\"}")]
        [TestCase("{\"version\": \"x.y\", \"url\": \"https://a.b/c\"}")]
        [TestCase("{\"version\": \"0.4.0\", \"url\": \"http://insecure.example/c\"}")]
        [TestCase("{\"version\": \"0.4.0\", \"url\": \"javascript:alert(1)\"}")]
        public void TryParse_RejectsInvalidOrUnsafeManifests(string json)
        {
            Assert.IsFalse(UpdateManifest.TryParse(json, out UpdateManifest manifest));
            Assert.IsNull(manifest);
        }

        [Test]
        public void TutorialDone_SurvivesSaveAndLoad()
        {
            var settings = new GameSettings { TutorialDone = true };

            Assert.IsTrue(GameSettings.Parse(settings.Serialize()).TutorialDone);
            Assert.IsTrue(settings.Clone().TutorialDone);
            Assert.IsFalse(GameSettings.Parse(string.Empty).TutorialDone);
        }
    }
}
