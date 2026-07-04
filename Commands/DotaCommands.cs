using Discord;
using Discord.WebSocket;
using Dota2StatsBot.Models;
using Dota2StatsBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dota2StatsBot.Commands
{
    public class DotaCommands
    {
        private readonly OpenDotaService _dotaService;

        public DotaCommands(OpenDotaService dotaService)
        {
            _dotaService = dotaService;
        }

        // Реєстрація всіх slash-команд
        public static List<SlashCommandBuilder> GetCommandBuilders() =>
       [
           new SlashCommandBuilder()
                .WithName("toppicks")
                .WithDescription("🏆 Топ героїв за кількістю піків (публічні ігри)")
                .AddOption("count", ApplicationCommandOptionType.Integer,
                    "Кількість героїв (1-25, за замовч. 10)", isRequired: false),

            new SlashCommandBuilder()
                .WithName("topwinrate")
                .WithDescription("📈 Топ героїв за winrate (pub, мін. 1000 ігор)")
                .AddOption("count", ApplicationCommandOptionType.Integer,
                    "Кількість героїв (1-25, за замовч. 10)", isRequired: false),

            new SlashCommandBuilder()
                .WithName("propicks")
                .WithDescription("🎮 Топ героїв про-сцени")
                .AddOption("count", ApplicationCommandOptionType.Integer,
                    "Кількість героїв (1-25, за замовч. 10)", isRequired: false),

            new SlashCommandBuilder()
                .WithName("turbo")
                .WithDescription("⚡ Топ героїв у Turbo режимі")
                .AddOption("count", ApplicationCommandOptionType.Integer,
                    "Кількість героїв (1-25, за замовч. 10)", isRequired: false),

            new SlashCommandBuilder()
                .WithName("hero")
                .WithDescription("🦸 Детальна статистика героя")
                .AddOption("name", ApplicationCommandOptionType.String,
                    "Ім'я героя (англ., напр: Pudge, Anti-Mage)", isRequired: true),
        ];

        // Обробка команд
        public async Task HandleCommandAsync(SocketSlashCommand cmd)
        {
            await cmd.DeferAsync();

            try
            {
                switch (cmd.Data.Name)
                {
                    case "toppicks": await HandleTopPicksAsync(cmd); break;
                    case "topwinrate": await HandleTopWinRateAsync(cmd); break;
                    case "propicks": await HandleProPicksAsync(cmd); break;
                    case "turbo": await HandleTurboAsync(cmd); break;
                    case "hero": await HandleHeroAsync(cmd); break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Помилка команди [{cmd.Data.Name}]: {ex.Message}");
                await cmd.FollowupAsync("❌ Помилка при отриманні даних. Спробуй пізніше.");
            }
        }

        private async Task HandleTopPicksAsync(SocketSlashCommand cmd)
        {
            int count = GetCountOption(cmd, 10);
            var heroes = await _dotaService.GetTopPicksAsync(count);

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < heroes.Count; i++)
            {
                var h = heroes[i];
                sb.AppendLine($"`{i + 1,2}.` **{h.Name}** {h.AttrEmoji}");
                sb.AppendLine($"　　Піків: `{h.PubPick:N0}` | WR: `{h.WinRate}%`");
            }

            var embed = new EmbedBuilder()
                .WithTitle($"🏆 Топ {count} піків Dota 2 (всі публічні ігри)")
                .WithDescription(sb.ToString())
                .WithColor(Color.Gold)
                .WithFooter("📊 Джерело: OpenDota API (api.opendota.com) • pub_pick")
                .WithCurrentTimestamp()
                .Build();

            await cmd.FollowupAsync(embed: embed);
        }

        // ─────────────────────────────────────────
        // /topwinrate
        // ─────────────────────────────────────────
        private async Task HandleTopWinRateAsync(SocketSlashCommand cmd)
        {
            int count = GetCountOption(cmd, 10);
            var heroes = await _dotaService.GetTopWinRateAsync(count);

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < heroes.Count; i++)
            {
                var h = heroes[i];
                sb.AppendLine($"`{i + 1,2}.` **{h.Name}** {h.AttrEmoji}");
                sb.AppendLine($"　　WR: `{h.WinRate}%` | Ігор: `{h.PubPick:N0}`");
            }

            var embed = new EmbedBuilder()
                .WithTitle($"📈 Топ {count} героїв за Winrate (pub, мін. 1000 ігор)")
                .WithDescription(sb.ToString())
                .WithColor(Color.Green)
                .WithFooter("📊 Джерело: OpenDota API (api.opendota.com) • pub_win/pub_pick")
                .WithCurrentTimestamp()
                .Build();

            await cmd.FollowupAsync(embed: embed);
        }

        // ─────────────────────────────────────────
        // /propicks
        // ─────────────────────────────────────────
        private async Task HandleProPicksAsync(SocketSlashCommand cmd)
        {
            int count = GetCountOption(cmd, 10);
            var heroes = await _dotaService.GetTopProPicksAsync(count);

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < heroes.Count; i++)
            {
                var h = heroes[i];
                double wr = h.ProPick > 0
                    ? Math.Round((double)h.ProWin / h.ProPick * 100, 1) : 0;
                sb.AppendLine($"`{i + 1,2}.` **{h.Name}** {h.AttrEmoji}");
                sb.AppendLine($"　　Піків: `{h.ProPick}` | Банів: `{h.ProBan}` | WR: `{wr}%`");
            }

            var embed = new EmbedBuilder()
                .WithTitle($"🎮 Топ {count} піків на Про-сцені Dota 2")
                .WithDescription(sb.ToString())
                .WithColor(Color.Blue)
                .WithFooter("📊 Джерело: OpenDota API (api.opendota.com) • pro_pick")
                .WithCurrentTimestamp()
                .Build();

            await cmd.FollowupAsync(embed: embed);
        }

        // ─────────────────────────────────────────
        // /turbo
        // ─────────────────────────────────────────
        private async Task HandleTurboAsync(SocketSlashCommand cmd)
        {
            int count = GetCountOption(cmd, 10);
            var heroes = await _dotaService.GetTopTurboAsync(count);

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < heroes.Count; i++)
            {
                var h = heroes[i];
                double wr = h.TurboPicks > 0
                    ? Math.Round((double)h.TurboWins / h.TurboPicks * 100, 1) : 0;
                sb.AppendLine($"`{i + 1,2}.` **{h.Name}** {h.AttrEmoji}");
                sb.AppendLine($"　　Піків: `{h.TurboPicks:N0}` | WR: `{wr}%`");
            }

            var embed = new EmbedBuilder()
                .WithTitle($"⚡ Топ {count} героїв у Turbo режимі")
                .WithDescription(sb.ToString())
                .WithColor(new Color(0xFF8C00))
                .WithFooter("📊 Джерело: OpenDota API (api.opendota.com) • turbo_picks")
                .WithCurrentTimestamp()
                .Build();

            await cmd.FollowupAsync(embed: embed);
        }

        // ─────────────────────────────────────────
        // /hero <name>
        // ─────────────────────────────────────────
        private async Task HandleHeroAsync(SocketSlashCommand cmd)
        {
            string heroName = cmd.Data.Options.First().Value.ToString() ?? "";
            var hero = await _dotaService.GetHeroAsync(heroName);

            if (hero == null)
            {
                await cmd.FollowupAsync(
                    $"❌ Героя **{heroName}** не знайдено.\n" +
                    $"Спробуй англійською: `Pudge`, `Anti-Mage`, `Crystal Maiden`");
                return;
            }

            double proWr = hero.ProPick > 0
                ? Math.Round((double)hero.ProWin / hero.ProPick * 100, 1) : 0;
            double turboWr = hero.TurboPicks > 0
                ? Math.Round((double)hero.TurboWins / hero.TurboPicks * 100, 1) : 0;

            // Топ-3 ранги за winrate
            var rankStats = Enumerable.Range(1, 7)
                .Select(r => (Rank: r, WR: hero.GetWinRateByRank(r), Pick: hero.GetPickByRank(r)))
                .Where(r => r.Pick > 0)
                .OrderByDescending(r => r.WR)
                .Take(3)
                .Select(r => $"`{HeroStats.GetRankName(r.Rank)}: {r.WR}%`")
                .ToList();

            var embed = new EmbedBuilder()
                .WithTitle($"🦸 {hero.Name}")
                .WithThumbnailUrl(hero.ImageUrl)
                .WithColor(Color.Purple)
                .AddField("⚔️ Тип / Атрибут",
                    $"{hero.AttrEmoji} | {hero.AttackType}\n" +
                    $"Ролі: {string.Join(", ", hero.Roles)}",
                    inline: false)
                .AddField("📊 Публічні ігри (всі ранги)",
                    $"Піків: **{hero.PubPick:N0}**\n" +
                    $"Winrate: **{hero.WinRate}%**",
                    inline: true)
                .AddField("⚡ Turbo режим",
                    $"Піків: **{hero.TurboPicks:N0}**\n" +
                    $"Winrate: **{turboWr}%**",
                    inline: true)
                .AddField("🏟️ Про-сцена",
                    hero.ProPick > 0
                        ? $"Піків: **{hero.ProPick}**\nБанів: **{hero.ProBan}**\nWR: **{proWr}%**"
                        : "_Немає даних_",
                    inline: true)
                .AddField("🏅 Топ-3 ранги за WR",
                    rankStats.Count > 0 ? string.Join("\n", rankStats) : "_Немає даних_",
                    inline: false)
                .WithFooter("📊 Джерело: OpenDota API (api.opendota.com)")
                .WithCurrentTimestamp()
                .Build();

            await cmd.FollowupAsync(embed: embed);
        }

        // ─────────────────────────────────────────
        private static int GetCountOption(SocketSlashCommand cmd, int defaultVal)
        {
            var opt = cmd.Data.Options.FirstOrDefault(o => o.Name == "count");
            if (opt == null) return defaultVal;
            int val = (int)(long)opt.Value;
            return Math.Clamp(val, 1, 25); // Обмежуємо 1-25
        }
    }
}
