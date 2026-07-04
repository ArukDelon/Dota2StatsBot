using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dota2StatsBot.Models
{
    public class HeroStats
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("localized_name")]
        public string Name { get; set; } = "";

        [JsonProperty("name")]
        public string InternalName { get; set; } = ""; // npc_dota_hero_antimage

        [JsonProperty("primary_attr")]
        public string PrimaryAttr { get; set; } = "";

        [JsonProperty("attack_type")]
        public string AttackType { get; set; } = "";

        [JsonProperty("roles")]
        public List<string> Roles { get; set; } = new();

        // ✅ Піки по рангах Herald(1) → Divine(7)
        // Immortal (8) завжди = 0 в heroStats, тому НЕ використовуємо
        [JsonProperty("1_pick")] public int Rank1Pick { get; set; } // Herald
        [JsonProperty("2_pick")] public int Rank2Pick { get; set; } // Guardian
        [JsonProperty("3_pick")] public int Rank3Pick { get; set; } // Crusader
        [JsonProperty("4_pick")] public int Rank4Pick { get; set; } // Archon
        [JsonProperty("5_pick")] public int Rank5Pick { get; set; } // Legend
        [JsonProperty("6_pick")] public int Rank6Pick { get; set; } // Ancient
        [JsonProperty("7_pick")] public int Rank7Pick { get; set; } // Divine
        [JsonProperty("8_pick")] public int Rank8Pick { get; set; } // Immortal (= 0)

        [JsonProperty("1_win")] public int Rank1Win { get; set; }
        [JsonProperty("2_win")] public int Rank2Win { get; set; }
        [JsonProperty("3_win")] public int Rank3Win { get; set; }
        [JsonProperty("4_win")] public int Rank4Win { get; set; }
        [JsonProperty("5_win")] public int Rank5Win { get; set; }
        [JsonProperty("6_win")] public int Rank6Win { get; set; }
        [JsonProperty("7_win")] public int Rank7Win { get; set; }
        [JsonProperty("8_win")] public int Rank8Win { get; set; }

        // ✅ Загальна публічна статистика (всі ранги разом)
        [JsonProperty("pub_pick")]
        public int PubPick { get; set; }

        [JsonProperty("pub_win")]
        public int PubWin { get; set; }

        // ✅ Turbo режим
        [JsonProperty("turbo_picks")]
        public int TurboPicks { get; set; }

        [JsonProperty("turbo_wins")]
        public int TurboWins { get; set; }

        // ✅ Про-сцена
        [JsonProperty("pro_pick")] public int ProPick { get; set; }
        [JsonProperty("pro_win")] public int ProWin { get; set; }
        [JsonProperty("pro_ban")] public int ProBan { get; set; }

        // ✅ Winrate по рангах
        public double GetWinRateByRank(int rank)
        {
            var (pick, win) = rank switch
            {
                1 => (Rank1Pick, Rank1Win),
                2 => (Rank2Pick, Rank2Win),
                3 => (Rank3Pick, Rank3Win),
                4 => (Rank4Pick, Rank4Win),
                5 => (Rank5Pick, Rank5Win),
                6 => (Rank6Pick, Rank6Win),
                7 => (Rank7Pick, Rank7Win),
                _ => (PubPick, PubWin) // default = загальний pub
            };
            return pick > 0 ? Math.Round((double)win / pick * 100, 2) : 0;
        }

        // ✅ Загальний WinRate (pub = всі ранги)
        public double WinRate => PubPick > 0
            ? Math.Round((double)PubWin / PubPick * 100, 2)
            : 0;

        // ✅ Назва рангу
        public static string GetRankName(int rank) => rank switch
        {
            1 => "Herald",
            2 => "Guardian",
            3 => "Crusader",
            4 => "Archon",
            5 => "Legend",
            6 => "Ancient",
            7 => "Divine",
            8 => "Immortal",
            _ => "All"
        };

        // ✅ Іконка атрибута
        public string AttrEmoji => PrimaryAttr switch
        {
            "agi" => "🟢 AGI",
            "str" => "🔴 STR",
            "int" => "🔵 INT",
            "all" => "🟡 ALL",
            _ => PrimaryAttr.ToUpper()
        };

        // ✅ URL картинки героя через internal name
        public string ImageUrl
        {
            get
            {
                // "npc_dota_hero_antimage" → "antimage"
                var heroSlug = InternalName.Replace("npc_dota_hero_", "");
                return $"https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/heroes/{heroSlug}.png";
            }
        }

        public int GetPickByRank(int rank) => rank switch
        {
            1 => Rank1Pick,
            2 => Rank2Pick,
            3 => Rank3Pick,
            4 => Rank4Pick,
            5 => Rank5Pick,
            6 => Rank6Pick,
            7 => Rank7Pick,
            _ => PubPick
        };
    }
}
