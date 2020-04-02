using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Attributes;
using Telegram.Bot.Framework.Commands;
using Telegram.Bot.Types;

namespace Telegram.Bot.Framework.Entities
{

    public class CommandMatch
    {
        private readonly object _command;
        private readonly CommandAttribute _commandAttribute;
        public object Command => _command;
        public CommandAttribute CommandAttribute => _commandAttribute;
        public object Args { get; set; } = null;
        public Func<ITelegramBotClient, Update, object, bool> CanInvokeMethodInfo { get; set; } = null;
        public Func<ITelegramBotClient, Update, object, bool> InvokeMethodInfo { get; set; } = null;

        public CommandMatch(object command, CommandAttribute commandAttribute)
        {
            _command = command;
            _commandAttribute = commandAttribute;
        }
        public bool Invoke(ITelegramBotClient client, Update update)
        {
            if (InvokeMethodInfo is var invoke)
                return invoke(client, update, Args);
            return true;
        }
        public bool CanInvoke(ITelegramBotClient client, Update update)
        {
            if (CanInvokeMethodInfo is var canInvoke && canInvoke != null)
                return canInvoke(client, update, Args);
            return false;
        }
    }
}
