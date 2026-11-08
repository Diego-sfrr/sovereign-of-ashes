using System.Collections.Generic;
using SovereignOfAshes.Core;

namespace SovereignOfAshes.Presentation.Art
{
    /// <summary>
    /// Arte do jogo em pixel art 16x16, desenhada como texto (cores em <see cref="PixelPalette.Default"/>).
    /// </summary>
    public static class PixelSprites
    {
        private static readonly Dictionary<string, PixelSpriteDefinition> ById = new Dictionary<string, PixelSpriteDefinition>();

        /// <summary>Id do sprite usado quando nenhum outro se aplica.</summary>
        public const string Fallback = "rune";

        /// <summary>Todos os sprites.</summary>
        public static IEnumerable<PixelSpriteDefinition> All => ById.Values;

        static PixelSprites()
        {
            Register(new PixelSpriteDefinition("fire",
                "................",
                ".......o........",
                "......oo........",
                "......oyo.......",
                ".....oyyo..o....",
                ".....oyyoo.oo...",
                "....oyyyyo.oyo..",
                "...ooyyyyyooyo..",
                "...oyywwyyyyyo..",
                "..ooyywwwyyyoo..",
                "..oyyywwwwyyyo..",
                "..oyyywwwwyyyo..",
                "..ooyyywwyyyoo..",
                "...ooyyyyyyoo...",
                "....oooooooo....",
                "................"));

            Register(new PixelSpriteDefinition("iron",
                "................",
                "................",
                "................",
                "................",
                "....kkkkkkkk....",
                "...kssssssssk...",
                "..kswwssssssSk..",
                ".kswwssssssSSSk.",
                ".kssssssssSSSSk.",
                ".kSSSSSSSSSSSSk.",
                ".kSSSSSSSSSSSk..",
                "..kkkkkkkkkkk...",
                "................",
                "................",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("flesh",
                "................",
                "................",
                "...kkk...kkk....",
                "..kfffk.kfffk...",
                ".kffwffkffffFk..",
                ".kfwffffffffFk..",
                ".kffffffffffFk..",
                ".kfffffffffFFk..",
                "..kfffffffFFk...",
                "...kfffffFFk....",
                "....kfffFFk.....",
                ".....kfFFk......",
                "......kFk.......",
                ".......k........",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("time",
                "................",
                "...kkkkkkkkkk...",
                "...knnnnnnnnk...",
                "....kyyyyyyk....",
                "....kyyyyyyk....",
                ".....kyyyyk.....",
                "......kyyk......",
                ".......kk.......",
                "......k..k......",
                ".....k.yy.k.....",
                "....k.yyyy.k....",
                "....kyyyyyyk....",
                "...knnnnnnnnk...",
                "...kkkkkkkkkk...",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("faith",
                "................",
                ".......kk.......",
                "......kggk......",
                "......kggk......",
                "...kkkkggkkkk...",
                "...kggggggggk...",
                "...kGGGggGGGk...",
                "...kkkkggkkkk...",
                "......kggk......",
                "......kggk......",
                "......kggk......",
                "......kGGk......",
                ".....kggggk.....",
                "....kGGGGGGk....",
                "....kkkkkkkk....",
                "................"));

            Register(new PixelSpriteDefinition("fear",
                "................",
                "................",
                "................",
                ".....kkkkkk.....",
                "...kkwwwwwwkk...",
                "..kwwwwppwwwwk..",
                ".kwwwwpPPpwwwwk.",
                ".kwwwpPkkPpwwwk.",
                ".kwwwpPkkPpwwwk.",
                ".kwwwwpPPpwwwwk.",
                "..kwwwwppwwwwk..",
                "...kkwwwwwwkk...",
                ".....kkkkkk.....",
                "................",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("blood",
                "................",
                ".......k........",
                "......krk.......",
                "......krk.......",
                ".....krrrk......",
                ".....krrrk......",
                "....krrrrrk.....",
                "...krrwrrrrk....",
                "...krwrrrrrk....",
                "..krrwrrrrrRk...",
                "..krrrrrrrrRk...",
                "..krrrrrrrRRk...",
                "...krrrrrRRk....",
                "....kRRRRRk.....",
                ".....kkkkk......",
                "................"));

            Register(new PixelSpriteDefinition("forge",
                "................",
                ".......o........",
                "......oyo.......",
                ".....oyyyo......",
                ".....oywyo......",
                "......ooo.......",
                "..kkkkkkkkkkk...",
                ".ksssssssssssk..",
                "..kSSSSSSSSSk...",
                "....kSSSSSk.....",
                ".....kSSSk......",
                "....kSSSSSk.....",
                "...kSSSSSSSk....",
                "...kkkkkkkkk....",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("golem",
                "................",
                ".....kkkkkk.....",
                "....kssssssk....",
                "....kscsscsk....",
                "....kssssssk....",
                "..kkkSSSSSSkkk..",
                ".ksskssssssksSk.",
                ".ksskssssssksSk.",
                ".kSSkSSSSSSkSSk.",
                ".kkkkssssssskkk.",
                "....kssSSssk....",
                "....kssk.ssk....",
                "....kSSk.kSSk...",
                "...kkkkk.kkkk...",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("saint",
                ".....yyyyyy.....",
                "....y......y....",
                ".....yyyyyy.....",
                "......kffk......",
                "......kffk......",
                ".....kwwwwk.....",
                "....kwwwwwwk....",
                "...kwwkwwkwwk...",
                "...kwwkwwkwwk...",
                "....kwwwwwwk....",
                "....kwwggwwk....",
                "....kwwwwwwk....",
                "...kwwwwwwwwk...",
                "...kkkkkkkkkk...",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("wraith",
                "................",
                ".....kkkkkk.....",
                "....kccccccck...",
                "...kcccccccck...",
                "...kcckcckcck...",
                "...kcckcckcck...",
                "...kcccccccck...",
                "...kccckkccck...",
                "...kccccccccck..",
                "...kCcccccccck..",
                "...kCCccccccCk..",
                "...kCCcCcCcCCk..",
                "...kCk.kk.kCk...",
                "....k......k....",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("ashes",
                "................",
                "................",
                "................",
                "................",
                "................",
                "................",
                ".......a........",
                "......aAa..a....",
                ".....aaAaa.aa...",
                "....aaaAaaaaAa..",
                "...aaAaaaAaaaaa.",
                "..aaaaaAaaaaAaa.",
                ".AAaaaaaaAaaaaA.",
                ".AAAAAAAAAAAAAA.",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("altar",
                "................",
                ".......r........",
                "......rrr.......",
                ".......r........",
                "................",
                "..kkkkkkkkkkkk..",
                "..kRrrrrrrrrRk..",
                "..kkkkkkkkkkkk..",
                "...kaaaaaaaak...",
                "...kaAaaaaAak...",
                "...kaaaaaaaak...",
                "...kaAaaaaAak...",
                "..kkkkkkkkkkkk..",
                "..kAAAAAAAAAAk..",
                "..kkkkkkkkkkkk..",
                "................"));

            Register(new PixelSpriteDefinition("cultist",
                "......kkkk......",
                ".....khhhhk.....",
                "....khhhhhhk....",
                "....khkkkkhk....",
                "...khkkrkrkhk...",
                "...khkkkkkkhk...",
                "...khhkkkkhhk...",
                "..khhhhhhhhhhk..",
                "..khhhhRRhhhhk..",
                "..khhhhRRhhhhk..",
                "..khhhhhhhhhhk..",
                "...khhhhhhhhk...",
                "...khhhhhhhhk...",
                "...khhhhhhhhk...",
                "..kkhhhhhhhhkk..",
                "..kkkkkkkkkkkk.."));

            Register(new PixelSpriteDefinition("colossus",
                "...kk......kk...",
                "...kpk....kpk...",
                "...kppkkkkppk...",
                "...kPppppppPk...",
                "..kPpcPPPPcpPk..",
                "..kPppppppppPk..",
                ".kkPPPPPPPPPPkk.",
                "kpPkPppppppPkPpk",
                "kpPkPpPPPPpPkPpk",
                "kPPkPppppppPkPPk",
                ".kk.kPPPPPPk.kk.",
                "....kPPkkPPk....",
                "....kPPk.kPPk...",
                "...kPPPk.kPPPk..",
                "...kkkkk.kkkkk..",
                "................"));

            Register(new PixelSpriteDefinition("core",
                ".......yy.......",
                "...y...gg...y...",
                "....y.gggg.y....",
                ".....gggggg.....",
                "...ggGggggGgg...",
                "..ggGggwwggGgg..",
                ".ygggggwwgggggy.",
                "yygggwwwwwwgggyy",
                "yygggwwwwwwgggyy",
                ".ygggggwwgggggy.",
                "..ggGggwwggGgg..",
                "...ggGggggGgg...",
                ".....gggggg.....",
                "....y.gggg.y....",
                "...y...gg...y...",
                ".......yy......."));

            Register(new PixelSpriteDefinition("spear",
                "..............yo",
                ".............yyo",
                "............yyo.",
                "...........oyo..",
                "..........kso...",
                ".........ksk....",
                "........ksk.....",
                ".......ksk......",
                "......kSk.......",
                ".....kSk........",
                "....knk.........",
                "...knk..........",
                "..knk...........",
                ".knk............",
                ".kk.............",
                "................"));

            Register(new PixelSpriteDefinition("storm",
                "................",
                ".....aaaa.......",
                "...aaAAAAaa.....",
                "..aAAAAAAAAaa...",
                ".aAAAAAAAAAAAa..",
                ".aAAAAAAAAAAAa..",
                "..aaaaaaaaaaa...",
                ".....yo...yo....",
                "....yo...yo.....",
                "...yyy..yyy.....",
                ".....o....o.....",
                "....o....o......",
                "...o..o.....o...",
                ".....o...o......",
                "...o.....o..o...",
                "................"));

            Register(new PixelSpriteDefinition("book",
                "................",
                "................",
                "..kkkkk.kkkkk...",
                ".kwwwwwkwwwwwk..",
                ".kwaaawkwaaawk..",
                ".kwwwwwkwwwwwk..",
                ".kwaaawkwaaawk..",
                ".kwwwwwkwwwwwk..",
                ".kwaaawkwaaawk..",
                ".kwwwwwkwwwwwk..",
                ".knnnnnknnnnnk..",
                "..kkkkkgkkkkk...",
                ".......g........",
                ".......g........",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("sword",
                ".............kk.",
                "............ksk.",
                "...........ksk..",
                "..........ksk...",
                ".........ksk....",
                "........ksk.....",
                "...k...ksk......",
                "...kk.ksk.......",
                "....kksk........",
                "....kgk.........",
                "...kgkk.........",
                "..kgk.kk........",
                ".knk............",
                ".kk.............",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("heart",
                "................",
                "................",
                "..kkk....kkk....",
                ".krrrk..krrrk...",
                "krrwrrkkrrrrRk..",
                "krwrrrrrrrrrRk..",
                "krrrrrrrrrrrRk..",
                ".krrrrrrrrrRk...",
                "..krrrrrrrRk....",
                "...krrrrrRk.....",
                "....krrrRk......",
                ".....krRk.......",
                "......kk........",
                "................",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("gem",
                "................",
                "................",
                ".....kkkkkk.....",
                "....kbbwwbbk....",
                "...kbwwbbbbBk...",
                "..kbbbbbbbbBBk..",
                "..kkkkkkkkkkkk..",
                "...kbbbbbbbBk...",
                "....kbbbbbBk....",
                ".....kbbbBk.....",
                "......kbBk......",
                ".......kk.......",
                "................",
                "................",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("skull",
                "................",
                ".....kkkkkk.....",
                "....kwwwwwwk....",
                "...kwwwwwwwwk...",
                "...kwwwwwwwwk...",
                "...kwkkwwkkwk...",
                "...kwkkwwkkwk...",
                "...kwwwkkwwwk...",
                "....kwwwwwwk....",
                ".....kwkwkwk....",
                ".....kkkkkkk....",
                "................",
                "................",
                "................",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("rune",
                "................",
                "....kkkkkkkk....",
                "...kppppppppk...",
                "..kppkkkkkkppk..",
                "..kpkpppppkppk..",
                "..kpkpkkkpkppk..",
                "..kpkpkppkpppk..",
                "..kpkpkkpkpppk..",
                "..kpkppppkpppk..",
                "..kpkkkkkkpppk..",
                "..kpppppppppkk..",
                "...kppppppppk...",
                "....kkkkkkkk....",
                "................",
                "................",
                "................"));
        }

        /// <summary>
        /// Busca um sprite.
        /// </summary>
        /// <param name="id">Id.</param>
        /// <param name="sprite">Sprite encontrado.</param>
        /// <returns><c>false</c> se não existir.</returns>
        public static bool TryGet(string id, out PixelSpriteDefinition sprite)
        {
            sprite = null;
            return !string.IsNullOrEmpty(id) && ById.TryGetValue(id, out sprite);
        }

        private static void Register(PixelSpriteDefinition sprite) => ById[sprite.Id] = sprite;
    }

    /// <summary>
    /// Qual sprite representa cada coisa do jogo (dados → arte). Ids desconhecidos caem em um padrão por categoria.
    /// </summary>
    public static class ArtCatalog
    {
        private static readonly Dictionary<string, string> Elements = new Dictionary<string, string>
        {
            { "flesh", "flesh" }, { "fire", "fire" }, { "iron", "iron" }, { "time", "time" },
            { "faith", "faith" }, { "fear", "fear" }, { "blood", "blood" },
            { "sacred_forge", "forge" }, { "iron_golem", "golem" }, { "living_saint", "saint" },
            { "wraith", "wraith" }, { "ashes", "ashes" }, { "blood_altar", "altar" }
        };

        private static readonly Dictionary<string, string> Invaders = new Dictionary<string, string>
        {
            { "ash_cultist", "cultist" }, { "void_colossus", "colossus" }
        };

        private static readonly Dictionary<string, string> Cards = new Dictionary<string, string>
        {
            { "fire_spear", "spear" }, { "ash_storm", "storm" }, { "sermon", "book" },
            { "miracle", "core" }, { "recruit", "sword" }, { "infuse_iron", "iron" }, { "infuse_fire", "fire" }
        };

        /// <summary>Todos os ids de sprite referenciados pelo catálogo (para validação).</summary>
        public static IEnumerable<string> ReferencedSprites
        {
            get
            {
                foreach (string id in Elements.Values) yield return id;
                foreach (string id in Invaders.Values) yield return id;
                foreach (string id in Cards.Values) yield return id;
                foreach (ConceptType concept in (ConceptType[])System.Enum.GetValues(typeof(ConceptType))) yield return ForConcept(concept);
                yield return PixelSprites.Fallback;
            }
        }

        /// <summary>Sprite de um elemento da alquimia.</summary>
        /// <param name="elementId">Id do elemento.</param>
        /// <param name="kind">Categoria (usada se o id não estiver no catálogo).</param>
        /// <returns>Id do sprite.</returns>
        public static string ForElement(string elementId, ElementKind kind)
        {
            if (elementId != null && Elements.TryGetValue(elementId, out string sprite))
            {
                return sprite;
            }

            switch (kind)
            {
                case ElementKind.Entity: return "golem";
                case ElementKind.Structure: return "altar";
                default: return PixelSprites.Fallback;
            }
        }

        /// <summary>Sprite de um invasor.</summary>
        /// <param name="invaderId">Id do invasor.</param>
        /// <returns>Id do sprite.</returns>
        public static string ForInvader(string invaderId) =>
            invaderId != null && Invaders.TryGetValue(invaderId, out string sprite) ? sprite : "cultist";

        /// <summary>Sprite da ilustração de uma carta.</summary>
        /// <param name="cardId">Id da carta.</param>
        /// <param name="concept">Conceito da carta (usado se o id não estiver no catálogo).</param>
        /// <returns>Id do sprite.</returns>
        public static string ForCard(string cardId, ConceptType concept) =>
            cardId != null && Cards.TryGetValue(cardId, out string sprite) ? sprite : ForConcept(concept);

        /// <summary>Sprite de um conceito.</summary>
        /// <param name="concept">Conceito.</param>
        /// <returns>Id do sprite.</returns>
        public static string ForConcept(ConceptType concept)
        {
            switch (concept)
            {
                case ConceptType.Flesh: return "flesh";
                case ConceptType.Fire: return "fire";
                case ConceptType.Iron: return "iron";
                case ConceptType.Time: return "time";
                case ConceptType.Faith: return "faith";
                case ConceptType.Fear: return "fear";
                case ConceptType.Blood: return "blood";
                default: return PixelSprites.Fallback;
            }
        }
    }
}
