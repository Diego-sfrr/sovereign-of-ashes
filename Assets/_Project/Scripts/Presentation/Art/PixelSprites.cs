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

            Register(new PixelSpriteDefinition("pyre",
                "................",
                "......p.........",
                ".....pp..p......",
                ".....ppp.pp.....",
                "....ppPPppPp....",
                "....pPPyyPPp....",
                "...ppPPyoyPPp...",
                "...pPPyyoyyPp...",
                "...pPPPyyyPPp...",
                "....ppPPPPpp....",
                "..knnnkkkknnnk..",
                "..kNnnNnnNnnNk..",
                "...kkNNnNNkkk...",
                "................",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("executioner",
                "................",
                "..........rr....",
                ".........rrrk...",
                "........rrrk....",
                ".......rrrk.....",
                "......rrrk......",
                ".....rrrk.......",
                "...kkrrk........",
                "..kGgkk.........",
                "..kgGk..........",
                ".kGgk.r.........",
                ".kgk..R.........",
                "..k...r.........",
                "................",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("astrolabe",
                "................",
                ".....kkkkkk.....",
                "...kkgggggGkk...",
                "..kgGkkkkkkGgk..",
                ".kgGk..yy..kGgk.",
                ".kgk...yy...kgk.",
                ".kGk.yyyyyy.kGk.",
                ".kGk.yyyyyy.kGk.",
                ".kgk...yy...kgk.",
                ".kgGk..yy..kGgk.",
                "..kgGkkkkkkGgk..",
                "...kkGgggggkk...",
                ".....kkkkkk.....",
                "................",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("oracle",
                "................",
                "................",
                ".....kkkkkk.....",
                "...kkbbbbbbkk...",
                "..kbbbwwwwbbbk..",
                ".kbbwwbbbbwwbbk.",
                ".kbwwbBBBBbwwbk.",
                ".kbwbBwwwwBbwbk.",
                ".kbwwbBBBBbwwbk.",
                ".kbbwwbbbbwwbbk.",
                "..kbbbwwwwbbbk..",
                "...kkbbbbbbkk...",
                ".....kkkkkk.....",
                "................",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("aberration",
                "................",
                ".....kkkkkk.....",
                "....kppppppk....",
                "...kpPppppPpk...",
                "..kppwkppwkppk..",
                "..kppkkppkkppk..",
                "..kpppppppppPk..",
                "..kpPppkkkpPpk..",
                "..kkpppwwwppkk..",
                ".kpkkpppppkkpk..",
                ".kpk.kkpppk.kpk.",
                ".kk...kppk...kk.",
                "......kk.k......",
                "................",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("reliquary",
                "................",
                "................",
                "..kkkkkkkkkkk...",
                "..krrrrrrrrrk...",
                "..kRrrrrrrrRk...",
                "..kRRrrrrrRRk...",
                "...kRRRRRRRk....",
                "....kkRRRkk.....",
                "......kRk.......",
                "......kRk.......",
                ".....kRRRk......",
                "....kkkkkkk.....",
                "................",
                "................",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("mist",
                "................",
                "................",
                "....kkkk........",
                "...kaaaakkk.....",
                "..kaAaaaaaakk...",
                ".kaaaaAaaaAaak..",
                ".kaAaaaaaaaaak..",
                "..kaaaAaaaAaak..",
                "...kkaaaaaakk...",
                ".....kkkkkk.....",
                "................",
                "................",
                "................",
                "................",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("forge_eternal",
                "................",
                ".......c........",
                "......cwc.......",
                ".....cwwwc......",
                ".....cwwwc......",
                "......ccc.......",
                "..kkkkkkkkkkk...",
                ".kCCCCCCCCCCCk..",
                "..kSSSSSSSSSk...",
                "....kSSSSSk.....",
                ".....kSSSk......",
                "....kSSSSSk.....",
                "...kSSSSSSSk....",
                "...kkkkkkkkk....",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("golem_magma",
                "................",
                ".....kkkkkk.....",
                "....krrrrrrk....",
                "....kryrryrk....",
                "....krrrrrrk....",
                "..kkkRRRRRRkkk..",
                ".krrkrrrrrrkrRk.",
                ".krrkrrrrrrkrRk.",
                ".kRRkRRRRRRkRRk.",
                ".kkkkrrrrrrrkkk.",
                "....krrRRrrk....",
                "....krrk.rrk....",
                "....kRRk.kRRk...",
                "...kkkkk.kkkk...",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("saint_martyr",
                ".....pppppp.....",
                "....p......p....",
                ".....pppppp.....",
                "......kRRk......",
                "......kRRk......",
                ".....krrrrk.....",
                "....krrrrrrk....",
                "...krrkrrkrrk...",
                "...krrkrrkrrk...",
                "....krrrrrrk....",
                "....krrggrrk....",
                "....krrrrrrk....",
                "...krrrrrrrrk...",
                "...kkkkkkkkkk...",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("wraith_reaper",
                "................",
                ".....kkkkkk.....",
                "....kpppppppk...",
                "...kppppppppk...",
                "...kppkppkppk...",
                "...kppkppkppk...",
                "...kppppppppk...",
                "...kpppkkpppk...",
                "...kpppppppppk..",
                "...kPppppppppk..",
                "...kPPppppppPk..",
                "...kPPpPpPpPPk..",
                "...kPk.kk.kPk...",
                "....k......k....",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("altar_profane",
                "................",
                ".......p........",
                "......ppp.......",
                ".......p........",
                "................",
                "..kkkkkkkkkkkk..",
                "..kPppppppppPk..",
                "..kkkkkkkkkkkk..",
                "...kPPPPPPPPk...",
                "...kPkPPPPkPk...",
                "...kPPPPPPPPk...",
                "...kPkPPPPkPk...",
                "..kkkkkkkkkkkk..",
                "..kkkkkkkkkkkk..",
                "..kkkkkkkkkkkk..",
                "................"));

            Register(new PixelSpriteDefinition("fire_phoenix",
                "................",
                ".......r........",
                "......rr........",
                "......ror.......",
                ".....roor..r....",
                ".....roorr.rr...",
                "....roooor.ror..",
                "...rrooooorror..",
                "...rooyyooooor..",
                "..rrooyyyooorr..",
                "..roooyyyyooor..",
                "..roooyyyyooor..",
                "..rroooyyooorr..",
                "...rroooooorr...",
                "....rrrrrrrr....",
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
                "..kkkk....kkkk..",
                ".kffwrrk..krrrRk",
                "kffwrrrrkkrrrrRk",
                "kfwrrrrrrrrrrrRk",
                "krrrrrrrrrrrrrRk",
                "krrrrrrrrrrrrRRk",
                ".krrrrrrrrrrRRk.",
                "..krrrrrrrrRRk..",
                "...krrrrrrRRk...",
                "....krrrrRRk....",
                ".....krrRRk.....",
                "......krRk......",
                ".......kk.......",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("crystal",
                "................",
                ".......kk.......",
                "......kccwk.....",
                ".....kcccwbk....",
                "....kccwbbbBk...",
                "...kcccwbbbbBBk.",
                "...kccwbbbbbBBk.",
                "...kcbbbbbbbBBk.",
                "...kbbbbbbbbBBk.",
                "....kbbbbbbBBk..",
                ".....kbbbbBBk...",
                "......kbbBBk....",
                ".......kBBk.....",
                "........kk......",
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

            Register(new PixelSpriteDefinition("shield",
                "................",
                "..kkkkkkkkkkkk..",
                "..ksssssssssSk..",
                "..kswwsssssSSk..",
                "..ksswsssssSSk..",
                "..ksssssssSSSk..",
                "..ksssssssSSSk..",
                "..kssssssSSSSk..",
                "...kssssssSSk...",
                "...kssssSSSSk...",
                "....kssSSSSk....",
                ".....kSSSSk.....",
                "......kSSk......",
                ".......kk.......",
                "................",
                "................"));

            Register(new PixelSpriteDefinition("chevron",
                "................",
                "......kkkk......",
                "......kyyk......",
                "......kyyk......",
                "......kyyk......",
                "..kkkkkyykkkkk..",
                "..kyyyyyyyyyyk..",
                "...kyyyyyyyyk...",
                "....kyyyyyyk....",
                ".....kyyyyk.....",
                "......kyyk......",
                ".......kk.......",
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
            { "wraith", "wraith" }, { "ashes", "ashes" }, { "blood_altar", "altar" },
            { "fear_pyre", "pyre" }, { "crimson_executioner", "executioner" }, { "astrolabe", "astrolabe" },
            { "oracle", "oracle" }, { "abomination", "aberration" }, { "blood_reliquary", "reliquary" }, { "fear_mist", "mist" },
            { "eternal_forge", "forge_eternal" }, { "magma_golem", "golem_magma" }, { "martyr", "saint_martyr" },
            { "phoenix", "fire_phoenix" }, { "grim_reaper", "wraith_reaper" }, { "profane_altar", "altar_profane" }
        };

        private static readonly Dictionary<string, string> Invaders = new Dictionary<string, string>
        {
            { "ash_cultist", "cultist" }, { "void_colossus", "colossus" },
            { "profaner", "cultist" }, { "heretic_herald", "cultist" }, { "inquisitor", "colossus" }
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

        /// <summary>
        /// Cor que diferencia, no tabuleiro, invasores que reaproveitam o sprite de outro (Profanador roxo, Arauto dourado, Inquisidor rubro).
        /// </summary>
        /// <param name="invaderId">Id do invasor.</param>
        /// <returns>Cor de tonalização (branco = sem alteração).</returns>
        public static UnityEngine.Color InvaderTint(string invaderId)
        {
            switch (invaderId)
            {
                case "profaner": return new UnityEngine.Color(0.78f, 0.55f, 1f, 1f);
                case "heretic_herald": return new UnityEngine.Color(1f, 0.86f, 0.45f, 1f);
                case "inquisitor": return new UnityEngine.Color(1f, 0.55f, 0.5f, 1f);
                default: return UnityEngine.Color.white;
            }
        }

        /// <summary>Pasta (dentro de <c>Resources</c>) das ilustrações pintadas das cartas, uma por id: <c>CardArt/&lt;id&gt;.png</c>.</summary>
        public const string CardIllustrationFolder = "CardArt";

        /// <summary>Caminho em <c>Resources</c> da ilustração pintada de uma carta.</summary>
        /// <param name="cardId">Id da carta.</param>
        /// <returns>Caminho para <c>Resources.Load</c>, ou <c>null</c> se o id for vazio.</returns>
        public static string CardIllustrationPath(string cardId) =>
            string.IsNullOrWhiteSpace(cardId) ? null : $"{CardIllustrationFolder}/{cardId.Trim()}";

        /// <summary>
        /// Proporção verdadeira de uma textura. As pinturas grandes da interface são reamostradas para potência de 2 na
        /// importação (única forma de comprimi-las), o que muda as dimensões da textura mas não a arte; esta função devolve
        /// a proporção original delas (pelo nome do arquivo) e, para as demais, a da própria textura.
        /// </summary>
        /// <param name="texture">Textura.</param>
        /// <returns>Largura dividida pela altura.</returns>
        public static float TrueAspect(UnityEngine.Texture texture)
        {
            if (texture == null || texture.height <= 0)
            {
                return 1f;
            }

            switch (texture.name)
            {
                case "backdrop":
                case "defeat":
                case "victory":
                    return 1920f / 1080f;
                case "logo":
                    return 1600f / 893f;
                case "cardback":
                    return 384f / 512f;
                default:
                    return (float)texture.width / texture.height;
            }
        }

        /// <summary>Fundo pintado atrás do tabuleiro (<c>Resources/UI/backdrop</c>).</summary>
        public const string BackdropPath = "UI/backdrop";

        /// <summary>Logo do jogo, com bordas translúcidas (<c>Resources/UI/logo</c>).</summary>
        public const string LogoPath = "UI/logo";

        /// <summary>Tela de derrota (<c>Resources/UI/defeat</c>).</summary>
        public const string DefeatPath = "UI/defeat";

        /// <summary>Tela de vitória (<c>Resources/UI/victory</c>).</summary>
        public const string VictoryPath = "UI/victory";

        /// <summary>Verso das cartas, usado nas pilhas de compra e descarte (<c>Resources/UI/cardback</c>).</summary>
        public const string CardBackPath = "UI/cardback";

        /// <summary>Caminho em <c>Resources</c> do retrato pintado de um invasor (<c>Portraits/&lt;id&gt;</c>).</summary>
        /// <param name="invaderId">Id do invasor.</param>
        /// <returns>Caminho, ou <c>null</c> se o id for vazio.</returns>
        public static string InvaderPortraitPath(string invaderId) =>
            string.IsNullOrWhiteSpace(invaderId) ? null : $"Portraits/{invaderId.Trim()}";

        /// <summary>Caminho em <c>Resources</c> do retrato pintado de uma civilização.</summary>
        /// <param name="isHeretic">Se a civilização é herege (retrato sombrio).</param>
        /// <returns>Caminho para <c>Resources.Load</c>.</returns>
        public static string CivilizationPortraitPath(bool isHeretic) =>
            isHeretic ? "Portraits/civ_heretic" : "Portraits/civ_devout";

        /// <summary>Retrato de uma civilização específica (devota); cada seita tem o seu.</summary>
        /// <param name="civilizationId">Id da civilização.</param>
        /// <returns>Caminho para <c>Resources.Load</c> (pode não existir: use <see cref="CivilizationPortraitPath(bool)"/> como reserva).</returns>
        public static string CivilizationPortraitPath(string civilizationId) =>
            string.IsNullOrEmpty(civilizationId) ? CivilizationPortraitPath(false) : "Portraits/civ_" + civilizationId;

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
