using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Commands;
using Telegram.Bot.Types;

namespace Telegram.Bot.Framework.AccessRules
{
    public interface IAccessRule
    {
        bool HasAccess(ITelegramBotClient client, Update update);
    }
}
