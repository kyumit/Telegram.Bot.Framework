using System;
using System.Collections.Generic;
using Telegram.Bot.Framework.Commands;
using System.Linq;

namespace Telegram.Bot.Framework.Menus
{
    public class InlineButton
    {
        ICommand _command;
        private string _text = "";
        public string Text { get => _text; set => _text = value; }
        public ICommand Command { get => _command; set => _command = value; }
        public Dictionary<string, object> CallbackData { get => _callbackData;  }

        private Dictionary<string, object> _callbackData;
        public InlineButton(ICommand command, Dictionary<string, object> callbackData)
        {
            _command = command ?? throw new ArgumentNullException(nameof(command));
            if (callbackData != null)
                _callbackData = callbackData;
        }

    }
}