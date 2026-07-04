using Discord;
using Discord.WebSocket;
using Dota2StatsBot.Commands;
using Dota2StatsBot.Services;

const ulong DEV_GUILD_ID = 714567196961144883;

const bool USE_GUILD_COMMANDS = true;

string token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
               ?? System.Text.Json.JsonDocument
                    .Parse(File.ReadAllText("appsettings.json"))
                    .RootElement.GetProperty("DiscordToken").GetString()
               ?? throw new Exception("Токен не знайдено!");

var config = new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
};

var client = new DiscordSocketClient(config);
var dotaService = new OpenDotaService();
var dotaCommands = new DotaCommands(dotaService);

// Логування
client.Log += log =>
{
    Console.WriteLine($"[{log.Severity}] {log.Message}");
    return Task.CompletedTask;
};

// Реєстрація команд при старті
client.Ready += async () =>
{
    Console.WriteLine("✅ Бот онлайн! Реєструємо slash-команди...");

    var builtCommands = DotaCommands.GetCommandBuilders()
                                   .Select(b => b.Build())
                                   .ToArray();
    if (USE_GUILD_COMMANDS)
    {
        // ⚡ Миттєва реєстрація на конкретній гільдії
        var guild = client.GetGuild(DEV_GUILD_ID);

        if (guild == null)
        {
            Console.WriteLine($"❌ Гільдію {DEV_GUILD_ID} не знайдено. Перевір ID.");
            return;
        }

        try
        {
            // BulkOverwrite замінює ВСІ команди гільдії одним запитом — найшвидший спосіб
            await guild.BulkOverwriteApplicationCommandAsync(builtCommands);
            Console.WriteLine($"⚡ Guild-команди зареєстровано миттєво на: {guild.Name}");
        }
        catch (Discord.Net.HttpException ex)
        {
            Console.WriteLine($"❌ Помилка реєстрації: {ex.Message}");
        }
    }
    else
    {
        // 🌍 Глобальна реєстрація (до 1 год на оновлення)
        try
        {
            await client.BulkOverwriteGlobalApplicationCommandsAsync(builtCommands);
            Console.WriteLine("🌍 Global-команди зареєстровано (оновляться до 1 год).");
        }
        catch (Discord.Net.HttpException ex)
        {
            Console.WriteLine($"❌ Помилка реєстрації: {ex.Message}");
        }
    }
};

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
            Console.WriteLine($"❌ Помилка: {ex.Message}");
            await slashCmd.FollowupAsync("❌ Сталася помилка при отриманні даних.");
        }
    }
};

await client.LoginAsync(TokenType.Bot, token);
await client.StartAsync();

Console.WriteLine("🎮 Dota 2 Stats Bot запущений! Ctrl+C для зупинки.");
await Task.Delay(-1);

