// See https://aka.ms/new-console-template for more information
using dotenv.net;
using System;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Config;



// Load environment variables from .env file
DotEnv.Load();
var envVars = DotEnv.Read();

string twitchUsername = envVars["TWITCH_USERNAME"];
string twitchToken = envVars["TWITCH_OAUTH_TOKEN"];
string twitchChannel = envVars["TWITCH_CHANNEL"];
string openaiKey = envVars["OPEN_AI_KEY"];

// Validate required environment variables
if (string.IsNullOrWhiteSpace(twitchUsername) ||
    string.IsNullOrWhiteSpace(twitchToken) ||
    string.IsNullOrWhiteSpace(twitchChannel) ||
    string.IsNullOrWhiteSpace(openaiKey))
{
    Console.Error.WriteLine("Missing required environment variables. Please check your .env file.");
    return;
}

// Configure log4net for logging to console and file
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
var log = LogManager.GetLogger(typeof(Program));

log.Info("Starting Twitch Anti-Clanker GPT Bot");

// Set up Twitch client credentials and moderator
var creds = new ConnectionCredentials(twitchUsername, twitchToken);
var client = new TwitchClient();
var moderator = new ChatModerator(openaiKey);

// Initialize Twitch client with credentials and channel
client.Initialize(creds, twitchChannel);

/// <summary>
/// Event handler: triggered when a chat message is received.
/// Logs the message, classifies it using GPT, and moderates if flagged as spam.
/// </summary>
client.OnMessageReceived += async (sender, e) =>
{
    string username = e.ChatMessage.Username;
    string message = e.ChatMessage.Message;

    log.Info($"{username}: {message}");

    // Call GPT moderation
    string result = await moderator.ClassifyMessage(username, message);

    if (result == "spam")
    {
        log.Warn($"[FLAGGED SPAM] {username}: {message}");
        client.TimeoutUser(e.ChatMessage.Channel, username, TimeSpan.FromMinutes(10), "No self-promo");
        client.DeleteMessage(e.ChatMessage.Channel, e.ChatMessage.Id);
        client.SendMessage(e.ChatMessage.Channel, $"@{username} please no self-promo.");
    }
};

/// <summary>
/// Event handler: triggered when the bot successfully connects to Twitch.
/// Logs the connection status.
/// </summary>
client.OnConnected += (s, e) =>
{
    log.Info($"✅ Connected to Twitch channel: {twitchChannel}");
};

// Connect to Twitch
client.Connect();

// Keep the bot running indefinitely
await Task.Delay(-1);
