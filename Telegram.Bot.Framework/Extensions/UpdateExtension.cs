using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Telegram.Bot.Framework
{
    public static class UpdateExtension
    {
        public static User GetUserFrom(this Update update)
        {
            User user = null;
            switch (update.Type)
            {
                case Telegram.Bot.Types.Enums.UpdateType.Message:
                    user = update.Message.From;
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.InlineQuery:
                    user = update.InlineQuery.From;
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.ChosenInlineResult:
                    user = update.ChosenInlineResult.From;
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.CallbackQuery:
                    user = update.CallbackQuery.From;
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.EditedMessage:
                    user = update.EditedMessage.From;
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.ChannelPost:
                    user = update.ChannelPost.From;
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.EditedChannelPost:
                    user = update.EditedChannelPost.From;
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.ShippingQuery:
                    user = update.ShippingQuery.From;
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.PreCheckoutQuery:
                    user = update.PreCheckoutQuery.From;
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.Unknown:
                default:
                    return null;
            }
            return user;
        }
    }
}
