using System;
using System.Collections.Generic;
using Telegram.Bot.Framework.Commands;
using System.Linq;
using Telegram.Bot.Types;

namespace Telegram.Bot.Framework.Menus
{
    public class InlineButton
    {
        object _command;
        private string _text = "";
        public string Text { get => _text; set => _text = value; }
        public object Command { get => _command; set => _command = value; }
        public Dictionary<string, object> CallbackData { get => _callbackData; }
        private object _callbackObj;
        private Dictionary<string, object> _callbackData;
        public InlineButton(object command, Dictionary<string, object> callbackData)
        {
            _command = command ?? throw new ArgumentNullException(nameof(command));
            if (command?.GetType().IsSubclassOf(typeof(IGenericCommand<,>)) ?? false == false)
                throw new ArgumentException(nameof(command) + " is not inheriting any command");
            if (callbackData != null)
                _callbackData = callbackData;
        }
        public InlineButton(object command, object callbackObj)
            : this(command, GetCallbackData(callbackObj))
        {
        }
        public InlineButton(string text, object command, object callbackObj)
            : this(command, callbackObj)
        {
            _text = text;
        }
        private static Dictionary<string, object> GetCallbackData(object callbackObj)
        {
            if (callbackObj != null)
            {
                Type callbackObjType = callbackObj.GetType();
                return callbackObjType
                          .GetProperties()
                          .Where(prop => prop.GetMethod != null)
                          .ToDictionary(prop => prop.Name, prop => prop.GetValue(callbackObj));
            }
            return null;
        }
    }
}