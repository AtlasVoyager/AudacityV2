using AudacityV2.AWS;
using AudacityV2.comms;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Playwright;
using Newtonsoft.Json;
using System.Reflection.PortableExecutable;
using System.Windows.Markup;

namespace AudacityV2
{
    internal class Program
    {
        //making a discord clone
        private static DiscordClient _Client { get; set; }
        private static CommandsNextExtension _Comms { get; set; }

        static async Task Main(string[] args)
        {
            var jReader = new JReader();
            await jReader.JRead(); //reads the config file and saves it as properties


         

            //config for our discord clone
            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jReader.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            //make a new client
            _Client = new DiscordClient(discordConfig);
            _Client.Ready += Client_Ready;

            var commConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { jReader.prefix },
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = false
            };

            _Comms = _Client.UseCommandsNext(commConfig);
            _Comms.RegisterCommands<Comms.Commands>();

            //testing resolve
            //await helper.Resolve();

            await _Client.ConnectAsync();
            await Task.Delay(-1);

        }

        private static Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }
    }
}
