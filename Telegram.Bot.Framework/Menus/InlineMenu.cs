using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Attributes;
using Telegram.Bot.Framework.Commands;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Framework.Menus
{
    public class InlineMenu
    {
        List<InlineButton> _buttons = new List<InlineButton>();
        Message _message = null;
        ITelegramBotClient _client;
        readonly int _rows, _columns;
        string _title, _content;
        public string Title { get => _title; set => _title = value; }
        public string Content { get => _content; set => _content = value; }
        public InlineMenu(ITelegramBotClient client, Message message = null, int rows = -1, int columns = -1)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _rows = rows > 0 ? rows : -1;
            _columns = columns > 0 ? columns : -1;
            _message = message;
            if (_rows > 0)
                _buttons = new List<InlineButton>(_rows);
        }


        public void AddButton(InlineButton button, int row = -1, int column = -1)
        {
            if (_rows > 0 && row > _rows)
                throw new ArgumentException(nameof(row) + " is greater than the defined grid of the menu.");
            if (_columns > 0 && column > _columns)
                throw new ArgumentException(nameof(column) + " is greater than the defined grid of the menu.");
            //TODO: Put the button into the correct row / col
            _buttons.Add(button);
        }
        public async Task<Message> SendMenu(Chat chat)
        {
            var message = await _client.SendTextMessageAsync(
                chat.Id,
                GetMessage(),
                Types.Enums.ParseMode.Markdown,
                replyMarkup: GetKeyboard());
            _message = message;
            return message;
        }
        public async Task UpdateMenu()
        {
            if (_message == null)
                throw new ArgumentNullException(nameof(_message));
            await _client.EditMessageTextAsync(
                new ChatId(_message.Chat.Id),
                _message.MessageId,
                GetMessage(),
                parseMode: Types.Enums.ParseMode.Markdown,
                replyMarkup: GetKeyboard());
        }
        protected virtual string GetMessage()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("*"+ _title+ "*");
            sb.AppendLine();
            sb.AppendLine(_content);
            return sb.ToString();
        }
        private InlineKeyboardMarkup GetKeyboard()
        {
            int columns = _columns > 0 ? _columns : 1;
            List<InlineKeyboardButton[]> rows = new List<InlineKeyboardButton[]>();
            InlineKeyboardButton[] currentColumn = null;
            int col = 0;
            for (int i = 0; i < _buttons.Count; i++, col = ++col % columns)
            {
                if(currentColumn == null)
                    currentColumn = new InlineKeyboardButton[columns];
                if (_buttons[i] is InlineButton btn)
                {
                    currentColumn[col] = new InlineKeyboardButton() { Text = btn.Text, CallbackData = GetCallbackData(btn.Command, btn.CallbackData) };
                }
                if ((col + 1) % columns == 0)
                {
                    if (currentColumn != null)
                        rows.Add(currentColumn);
                    currentColumn = null;
                }
            }
            if(currentColumn != null)
                rows.Add(currentColumn);
            return new InlineKeyboardMarkup(rows);
        }
        private string GetCallbackData(object command, Dictionary<string, object> parameters)
        {
            string[] keys = parameters.Keys.ToArray();
            CommandAttribute commandAttribute = CommandAttribute
                  .GetAttributes(command)
                  .Where(attr => attr.CommandUsage.HasFlag(CommandUsage.CallbackQueryCommand))
                  .Where(attr => Enumerable.SequenceEqual(keys.OrderBy(k => k), attr.Parameters.OrderBy(k => k)))
                  .FirstOrDefault();
            if (commandAttribute != null)
            {
                string callbackData = commandAttribute.Expression;
                for (int i = 0; i < parameters.Count; i++)
                   callbackData = callbackData.Replace("{" + keys[i] + "}", parameters[keys[i]].ToString());
                return callbackData;
            }
            return "-false-";
        }
    }
}
