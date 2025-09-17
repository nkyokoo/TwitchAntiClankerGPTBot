using Xunit;
using TwitchAntiClankerGPTBot;
using System.Threading.Tasks;
using dotenv.net;

namespace TwitchAntiClankerGPTBot.Test
{
    /// <summary>
    /// Integration tests for ChatModerator GPT classification using the real OpenAI API key.
    /// </summary>
    public class ChatModeratorIntegrationTests
    {
        /// <summary>
        /// Verifies that ClassifyMessage returns a non-empty result for a valid message using the real API key.
        /// </summary>
        [Fact]
        public async Task ClassifyMessage_ReturnsResult_ForValidMessage()
        {
            DotEnv.Load();
            var envVars = DotEnv.Read();
            string apiKey = envVars["OPEN_AI_KEY"];

            Assert.False(string.IsNullOrWhiteSpace(apiKey), "OPEN_AI_KEY must be set in .env for this test.");

            var moderator = new ChatModerator(apiKey);

            var result = await moderator.ClassifyMessage("Thomas_Jefferson04", "Check out my channel for free stuff!");
            Console.WriteLine($"Classification result 1: {result}");

            Assert.False(string.IsNullOrWhiteSpace(result), "Classification result should not be empty.");
        }

        /// <summary>
        /// Verifies that ClassifyMessage returns "spam" for a self-promotional message.
        /// </summary>
        [Fact]
        public async Task ClassifyMessage_ReturnsSpam_ForSelfPromoMessage()
        {
            DotEnv.Load();
            var envVars = DotEnv.Read();
            string apiKey = envVars["OPEN_AI_KEY"];
            Assert.False(string.IsNullOrWhiteSpace(apiKey), "OPEN_AI_KEY must be set in .env for this test.");

            var moderator = new ChatModerator(apiKey);

            var result = await moderator.ClassifyMessage("Thomas_Jefferson04", "Follow me for free gift cards and giveaways!");
            Console.WriteLine($"Classification result 2: {result}");

            Assert.Equal("spam", result);
        }

        /// <summary>
        /// Verifies that ClassifyMessage returns "not spam" for a normal chat message.
        /// </summary>
        [Fact]
        public async Task ClassifyMessage_ReturnsNotSpam_ForNormalMessage()
        {
            DotEnv.Load();
            var envVars = DotEnv.Read();
            string apiKey = envVars["OPEN_AI_KEY"];
            Assert.False(string.IsNullOrWhiteSpace(apiKey), "OPEN_AI_KEY must be set in .env for this test.");

            var moderator = new ChatModerator(apiKey);

            var result = await moderator.ClassifyMessage("EmeersVT", "How's everyone doing tonight?");
            Console.WriteLine($"Classification result 3: {result}");
            Assert.Equal("not spam", result);
        }
    }
}
