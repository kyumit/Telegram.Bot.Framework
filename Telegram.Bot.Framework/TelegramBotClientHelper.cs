using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Commands;
using Telegram.Bot.Framework.Entities;
using Telegram.Bot.Types;

namespace Telegram.Bot.Framework
{
    public class TelegramBotClientHelper : IDisposable
    {
        private readonly CommandManager _commandManager;
        private readonly ITelegramBotClient _telegramBotClient;
        private CommandMatch _lastMatch = null;
        private Dictionary<long, CommandMatch> _commandStorage = new Dictionary<long, CommandMatch>();
        public CommandManager CommandManager => _commandManager;

        public ITelegramBotClient Client => _telegramBotClient;

        public AntiFloodManager AntiFloodManager => _antiFloodManager;

        readonly AntiFloodManager _antiFloodManager;

        public TelegramBotClientHelper(ITelegramBotClient telegramBotClient)
        {
            _telegramBotClient = telegramBotClient ?? throw new ArgumentNullException(nameof(telegramBotClient));
            _antiFloodManager = new AntiFloodManager() { AntiFloodMessageCount = 20 };
            string botName = string.Empty;
            SemaphoreSlim tSem = new SemaphoreSlim(1);
            tSem.Wait();
            Task.Run(async () =>
            {
                User me = await _telegramBotClient.GetMeAsync();
                botName = me.Username;
                tSem.Release();
            });
            tSem.Wait();
            _commandManager = new CommandManager(botName);
            telegramBotClient.OnUpdate += TelegramBotClient_OnUpdate;
        }

        private void TelegramBotClient_OnUpdate(object sender, Args.UpdateEventArgs e)
        {
            Task.Run(async () =>
            {
                if (e.Update.Type == Types.Enums.UpdateType.Message && e.Update.Message.Chat.Type == Types.Enums.ChatType.Private && _antiFloodManager.CheckFlood(e.Update.GetUserFrom().Id) == false)
                {
                    await _telegramBotClient.SendTextMessageAsync(e.Update.Message.Chat.Id, "*FLOOD DETECTION!*\r\nThe Bot will not answer for one day!", Types.Enums.ParseMode.Markdown, replyToMessageId: e.Update.Message.MessageId);
                    return;
                }
                //Stopwatch sw = Stopwatch.StartNew();
                CommandMatch match = GetCommandFromStorage(e.Update);
                if (await _commandManager.GetCommandMatch(_telegramBotClient, e.Update) is CommandMatch commandMatch && commandMatch?.Command != null)
                    match = commandMatch;

                PutCommandIntoStorage(e.Update, match?.Invoke(_telegramBotClient, e.Update) == true ? null : match);
                //sw.Stop();
                //if(e.Update.Message != null)
                //await _telegramBotClient.SendTextMessageAsync(new ChatId(e.Update.Message.Chat.Id), "ProcessingTime: " + sw.ElapsedMilliseconds + " ms");
            });
        }

        private void PutCommandIntoStorage(Update update, CommandMatch match)
        {
            if (GetIdFromUpdate(update) is long id)
            {
                lock (_commandStorage)
                {
                    if (_commandStorage.ContainsKey(id))
                    {
                        if (match == null)
                            _commandStorage.Remove(id);
                        else
                            _commandStorage[id] = match;
                    }
                    else if (match != null)
                        _commandStorage.Add(id, match);
                }
            }
        }
        private CommandMatch GetCommandFromStorage(Update update)
        {
            if (GetIdFromUpdate(update) is long id)
                lock (_commandStorage)
                    if (_commandStorage.ContainsKey(id))
                        return _commandStorage[id];
            return null;
        }
        private long? GetIdFromUpdate(Update update)
        {
            if (update.Type == Types.Enums.UpdateType.Message)
                return update.Message.Chat.Id;
            if (update.Type == Types.Enums.UpdateType.CallbackQuery)
                return update.CallbackQuery.Message.Chat.Id;
            return null;
        }

        public void Dispose()
        {
            _antiFloodManager?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
