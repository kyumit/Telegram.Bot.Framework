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
        public MethodInfo CanInvokeMethodInfo { get; set; } = null;
        public MethodInfo InvokeMethodInfo { get; set; } = null;

        public CommandMatch(object command, CommandAttribute commandAttribute)
        {
            _command = command;
            _commandAttribute = commandAttribute;
        }
        public async Task<bool> Invoke(ITelegramBotClient client, Update update)
        {
            if (InvokeMethodInfo is MethodInfo invoke && Command is object command)
                return await (invoke.Invoke(command, new object[] { client, update, Args }) as Task<bool>);
            return true;
        }
        public async Task<bool> CanInvoke(ITelegramBotClient client, Update update)
        {
            if (CanInvokeMethodInfo is MethodInfo canInvoke && Command is object command)
                return await (canInvoke.Invoke(command, new object[] { client, update, Args }) as Task<bool>);
            return false;
        }
    }
}
