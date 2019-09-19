using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Telegram.Bot.Framework
{
    public static class ChatExtension
    {
        public static async Task<User> GetOwner(this Chat chat, ITelegramBotClient client)
        {
            if (chat != null)
            {
                if (await client.GetChatAdministratorsAsync(chat.Id) is ChatMember[] admins)
                    return admins.FirstOrDefault(admin => admin.Status == Types.Enums.ChatMemberStatus.Creator)?.User;
            }
            return null;
        }
        public static async Task<List<User>> GetAdministrators(this Chat chat, ITelegramBotClient client)
        {
            if (chat != null)
                return (await client.GetChatAdministratorsAsync(chat.Id)).Select(chatMember => chatMember.User).ToList();
            return null;
        }
    }
}
