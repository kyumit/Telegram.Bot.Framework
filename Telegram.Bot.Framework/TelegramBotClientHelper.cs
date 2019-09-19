using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Commands;
using Telegram.Bot.Framework.Entities;

namespace Telegram.Bot.Framework
{
    public class TelegramBotClientHelper
    {
        private readonly CommandManager _commandManager = new CommandManager();
        private readonly ITelegramBotClient _telegramBotClient;
        private CommandMatch _lastMatch = null;
        public CommandManager CommandManager => _commandManager;

        public ITelegramBotClient Client => _telegramBotClient;

        public TelegramBotClientHelper(ITelegramBotClient telegramBotClient)
        {
            _telegramBotClient = telegramBotClient ?? throw new ArgumentNullException(nameof(telegramBotClient));
            telegramBotClient.OnUpdate += TelegramBotClient_OnUpdate;
        }

        private void TelegramBotClient_OnUpdate(object sender, Args.UpdateEventArgs e)
        {
            Task.Run(async () =>
            {
                CommandMatch match = _lastMatch;
                if (await _commandManager.GetCommandMatch(_telegramBotClient, e.Update) is CommandMatch commandMatch && commandMatch?.Command != null)
                    match = commandMatch;

                if (match != null)
                    _lastMatch = (await match.Invoke(_telegramBotClient, e.Update)) ? null : match;
            });
        }
    }
}
