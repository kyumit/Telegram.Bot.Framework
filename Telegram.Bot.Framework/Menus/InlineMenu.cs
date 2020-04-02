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
        List<List<InlineButton>> _buttons = new List<List<InlineButton>>();
        Message _message = null;
        ITelegramBotClient _client;
        readonly int _rows, _columns;
        string _title, _content;
        public string Title { get => _title; set => _title = value; }
        public string Content { get => _content; set => _content = value; }
        public Message Message { get => _message; }

        public InlineMenu(ITelegramBotClient client, Message message = null, int columns = -1, int rows = -1)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _rows = rows > 0 ? rows : int.MaxValue;
            _columns = columns > 0 ? columns : int.MaxValue;
            _message = message;
            if (_rows > 0)
                _buttons = new List<List<InlineButton>>();
        }


        public void AddButton(InlineButton button, int row = -1, int column = -1)
        {
            if (_rows >= 0 && row > _rows)
                throw new ArgumentException(nameof(row) + " is greater than the defined grid of the menu.");
            if (_columns >= 0 && column > _columns)
                throw new ArgumentException(nameof(column) + " is greater than the defined grid of the menu.");
            //TODO: Put the button into the correct row / col

            if (row >= 0)
                while (_buttons.Count <= row)
                    _buttons.Add(new List<InlineButton>());
            if (row >= 0 && column >= 0)
                while (_buttons[row].Count <= column)
                    _buttons[row].Add(null);


            if (row >= 0)
            {
                if (column >= 0 && row < _rows && column < _columns)
                {
                    _buttons[row][column] = button;
                }
                else if (column < 0 && row < _rows)
                {
                    int index = _buttons[row].IndexOf(null);
                    if (index >= 0)
                        _buttons[row][index] = button;
                    else
                        _buttons[row].Add(button);
                }
            }
            else if (row < 0)
            {
                if (_buttons.Count == 0)
                    _buttons.Add(new List<InlineButton>());
                if (column >= 0)
                {
                    if (_buttons.FirstOrDefault(r => r.Where(c => c != null).Count() < column) is List<InlineButton> list)
                    {
                        int index = list.IndexOf(null);
                        if (index >= 0)
                            list[index] = button;
                        else
                            list.Add(button);
                    }
                    else
                    {
                        _buttons.Add(new List<InlineButton>() { button });
                    }
                }
                else if (column < 0)
                {
                    for (int r = 0; r < _rows; r++)
                    {
                        if (_buttons.Count <= r)
                            _buttons.Add(new List<InlineButton>());
                        for (int c = 0; c < _columns; c++)
                        {
                            if (_buttons[r].Count <= c)
                            {
                                _buttons[r].Add(button);
                                return;
                            }
                            if (_buttons[r][c] == null)
                            {
                                _buttons[r][c] = button;
                                return;
                            }
                        }
                    }
                }
            }
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
            try
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
            catch { }
        }
        protected virtual string GetMessage()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("*" + _title + "*");
            sb.AppendLine();
            sb.AppendLine(_content);
            return sb.ToString();
        }
        private InlineKeyboardMarkup GetKeyboard()
        {
            if (_buttons.All(row => row.All(btn => btn == null)))
                return null;
            return new InlineKeyboardMarkup(
                _buttons
                .Select(row => row
                    .Select(btn =>
                        new InlineKeyboardButton() { Text = btn?.Text ?? "", CallbackData = GetCallbackData(btn?.Command, btn?.CallbackData) }
                     )
                )
            );
        }
        private string GetCallbackData(object command, Dictionary<string, object> parameters)
        {
            if (command != null)
            {
                string[] keys = parameters?.Keys.Select(k => k.ToLower()).ToArray();

                CommandAttribute commandAttribute = CommandAttribute
                      .GetAttributes(command)
                      .Where(attr => attr.CommandUsage.HasFlag(CommandUsage.CallbackQueryCommand))
                      .Where(attr => (attr?.Parameters != null && keys != null && Enumerable.SequenceEqual(keys.OrderBy(k => k), attr.Parameters.OrderBy(k => k))) || (attr?.Parameters == null && keys == null))
                      .FirstOrDefault();
                if (commandAttribute != null)
                {
                    string callbackData = commandAttribute.Expression;
                    if (parameters != null)
                        for (int i = 0; i < parameters.Count; i++)
                        {
                            string oldStr = "{" + keys[i] + "}";
                            string newStr = parameters.FirstOrDefault(kvp => kvp.Key.ToLower() == keys[i]).Value?.ToString();
                            callbackData = callbackData.Replace(oldStr, newStr);
                        }
                    return callbackData;
                }
            }
            return "-false-";
        }
    }
}
