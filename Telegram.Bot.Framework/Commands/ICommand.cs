using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Telegram.Bot.Framework.Commands
{


    public interface ICommand<T> where T : new()
    {
        Task<bool> Invoke(ITelegramBotClient client, Update update, T args);
        Task<bool> CanInvoke(ITelegramBotClient client, Update update, T args);

    }
    public interface ICommand : ICommand<object>
    {

    }
}