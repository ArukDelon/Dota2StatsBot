using Dota2StatsBot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dota2StatsBot.Services
{
    public class OpenDotaService
    {
        private readonly HttpClient _http;
        private const string BaseUrl = "https://api.opendota.com/api";

        private List<HeroStats>? _cachedStats;
        private DateTime _cacheTime = DateTime.MinValue;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);

        public OpenDotaService()
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("User-Agent", "Dota2StatsDiscordBot/1.0");
        }

        private async Task<List<HeroStats>> GetHeroStatsAsync()
        {
            if (_cachedStats != null && DateTime.UtcNow - _cacheTime < _cacheDuration)
                return _cachedStats;

            Console.WriteLine("🔄 Завантажуємо дані з OpenDota API...");
            var response = await _http.GetStringAsync($"{BaseUrl}/heroStats");
            _cachedStats = JsonConvert.DeserializeObject<List<HeroStats>>(response)
                           ?? new List<HeroStats>();
            _cacheTime = DateTime.UtcNow;
            Console.WriteLine($"✅ Завантажено {_cachedStats.Count} героїв.");
            return _cachedStats;
        }

        // ✅ Топ за pub_pick (всі ранги разом) — реальні дані
        public async Task<List<HeroStats>> GetTopPicksAsync(int count = 10)
        {
            var stats = await GetHeroStatsAsync();
            return stats
                .Where(h => h.PubPick > 0)
                .OrderByDescending(h => h.PubPick)
                .Take(count)
                .ToList();
        }

        // ✅ Топ за winrate серед pub ігор (мін. 1000 ігор)
        public async Task<List<HeroStats>> GetTopWinRateAsync(int count = 10)
        {
            var stats = await GetHeroStatsAsync();
            return stats
                .Where(h => h.PubPick >= 1000)
                .OrderByDescending(h => h.WinRate)
                .Take(count)
                .ToList();
        }

        // ✅ Топ за пікрейтом конкретного рангу (1-7)
        public async Task<List<HeroStats>> GetTopPicksByRankAsync(int rank, int count = 10)
        {
            var stats = await GetHeroStatsAsync();
            return stats
                .OrderByDescending(h => h.GetPickByRank(rank))
                .Where(h => h.GetPickByRank(rank) > 0)
                .Take(count)
                .ToList();
        }

        // ✅ Топ про-сцени
        public async Task<List<HeroStats>> GetTopProPicksAsync(int count = 10)
        {
            var stats = await GetHeroStatsAsync();
            return stats
                .Where(h => h.ProPick > 0)
                .OrderByDescending(h => h.ProPick)
                .Take(count)
                .ToList();
        }

        // ✅ Топ Turbo режиму
        public async Task<List<HeroStats>> GetTopTurboAsync(int count = 10)
        {
            var stats = await GetHeroStatsAsync();
            return stats
                .Where(h => h.TurboPicks > 0)
                .OrderByDescending(h => h.TurboPicks)
                .Take(count)
                .ToList();
        }

        // ✅ Пошук героя
        public async Task<HeroStats?> GetHeroAsync(string heroName)
        {
            var stats = await GetHeroStatsAsync();
            return stats.FirstOrDefault(h =>
                h.Name.Contains(heroName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
