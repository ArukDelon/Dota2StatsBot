using Discord;
using Discord.WebSocket;
using Dota2StatsBot.Commands;
using Dota2StatsBot.Services;

const ulong DEV_GUILD_ID = 714567196961144883;
const bool USE_GUILD_COMMANDS = true;

var webBuilder = WebApplication.CreateBuilder(args);

// Вимикаємо зайві логи ASP.NET щоб не засмічувати консоль
webBuilder.Logging.ClearProviders();
webBuilder.Logging.AddConsole();

var webApp = webBuilder.Build();

webApp.Map("/", () => Results.Ok(new
{
    status = "🟢 online",
    bot = "Dota2StatsBot",
    time = DateTime.UtcNow.ToString("o")
}));

webApp.Map("/health", () => Results.Ok("OK"));

// ──────────────────────────────────────────────
// 🔑  ТОКЕН
// Render передає токен через Environment Variable
// Локально — читаємо з appsettings.json
// ──────────────────────────────────────────────
string token;
try
{
    token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
            ?? System.Text.Json.JsonDocument
                 .Parse(File.ReadAllText("appsettings.json"))
                 .RootElement.GetProperty("DiscordToken").GetString()
            ?? throw new Exception("Токен порожній!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Не вдалося отримати токен: {ex.Message}");
    throw;
}

// ──────────────────────────────────────────────
// 🤖  DISCORD КЛІЄНТ
// ──────────────────────────────────────────────
var config = new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
};

var client = new DiscordSocketClient(config);
var dotaService = new OpenDotaService();
var dotaCommands = new DotaCommands(dotaService);

// ──────────────────────────────────────────────
// 📋  ЛОГУВАННЯ
// ──────────────────────────────────────────────
client.Log += log =>
{
    // Фільтруємо незначні повідомлення щоб не спамити
    if (log.Severity <= LogSeverity.Warning || log.Exception != null)
        Console.WriteLine($"[Discord/{log.Severity}] {log.Message} {log.Exception?.Message}");
    return Task.CompletedTask;
};

// ──────────────────────────────────────────────
// 🚀  READY — реєстрація команд
// ──────────────────────────────────────────────
client.Ready += async () =>
{
    Console.WriteLine($"✅ Бот онлайн як: {client.CurrentUser.Username}");
    Console.WriteLine("📋 Реєструємо slash-команди...");

    var builtCommands = DotaCommands.GetCommandBuilders()
                                    .Select(b => b.Build())
                                    .ToArray();

    if (USE_GUILD_COMMANDS)
    {
        var guild = client.GetGuild(DEV_GUILD_ID);

        if (guild == null)
        {
            Console.WriteLine($"❌ Гільдію {DEV_GUILD_ID} не знайдено!");
            return;
        }

        try
        {
            await guild.BulkOverwriteApplicationCommandAsync(builtCommands);
            Console.WriteLine($"⚡ Guild-команди зареєстровано на: {guild.Name}");
        }
        catch (Discord.Net.HttpException ex)
        {
            Console.WriteLine($"❌ Помилка реєстрації команд: {ex.Message}");
        }
    }
    else
    {
        try
        {
            await client.BulkOverwriteGlobalApplicationCommandsAsync(builtCommands);
            Console.WriteLine("🌍 Global-команди зареєстровано (до 1 год на оновлення).");
        }
        catch (Discord.Net.HttpException ex)
        {
            Console.WriteLine($"❌ Помилка реєстрації команд: {ex.Message}");
        }
    }
};

// ──────────────────────────────────────────────
// 🎮  ОБРОБКА SLASH-КОМАНД
// ──────────────────────────────────────────────
client.InteractionCreated += async interaction =>
{
    if (interaction is SocketSlashCommand slashCmd)
    {
        try
        {
            await dotaCommands.HandleCommandAsync(slashCmd);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Помилка команди [{slashCmd.Data.Name}]: {ex.Message}");
            try { await slashCmd.FollowupAsync("❌ Сталася помилка. Спробуй пізніше."); }
            catch { /* ігноруємо якщо interaction вже протух */ }
        }
    }
};

// ──────────────────────────────────────────────
// ▶️  ЗАПУСК: Bot + Web Server паралельно
// ──────────────────────────────────────────────

// Запускаємо Discord бота у фоновому потоці
var botTask = Task.Run(async () =>
{
    try
    {
        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();
        Console.WriteLine("🎮 Discord бот запущений!");

        // Тримаємо бота живим (web server тримає весь процес)
        await Task.Delay(Timeout.Infinite);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Критична помилка бота: {ex.Message}");
    }
});

Console.WriteLine("🌐 Запускаємо Keep-Alive Web Server...");

// Запускаємо Web Server (він тримає весь процес живим на Render)
// Render автоматично інжектить PORT через env змінну
await webApp.RunAsync();