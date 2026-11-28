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
        public void PanelOffsets_SurviveSaveLoadAndClone()
        {
            var settings = new GameSettings();
            settings.SetPanelOffset(0, 120, -40);
            settings.SetPanelOffset(2, -300, 75);

            GameSettings loaded = GameSettings.Parse(settings.Serialize());
            Assert.AreEqual((120, -40), loaded.GetPanelOffset(0));
            Assert.AreEqual((0, 0), loaded.GetPanelOffset(1));
            Assert.AreEqual((-300, 75), loaded.GetPanelOffset(2));
            Assert.AreEqual((120, -40), settings.Clone().GetPanelOffset(0));
        }

        [Test]
        public void PanelOffsets_AreClampedAndIgnoreInvalidIndexes()
        {
            var settings = new GameSettings();
            settings.SetPanelOffset(1, 999999, -999999);
            settings.SetPanelOffset(7, 5, 5);

            Assert.AreEqual((4000, -4000), settings.GetPanelOffset(1));
            Assert.AreEqual((0, 0), settings.GetPanelOffset(7));
            Assert.AreEqual((0, 0), GameSettings.Parse("po9=5;pox=3;po1=abc").GetPanelOffset(0));
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
