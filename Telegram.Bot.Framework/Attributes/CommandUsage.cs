using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Bot.Framework.Attributes
{
    [Flags]
    public enum CommandUsage
    {
        Command = 1 << 0,
        CallbackQueryCommand = 1 << 1,
        All = (1 << 2)-1
    }
}
