using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Telegram.Bot.Framework.Commands
{

    public interface IGenericCommand<MessageType, ParameterType> where ParameterType : new() where MessageType : new()
    {
        Task<bool> Invoke(ITelegramBotClient client, MessageType message, ParameterType args);
        Task<bool> CanInvoke(ITelegramBotClient client, MessageType message, ParameterType args);
    }

    public interface ICommand<T> : IGenericCommand<Update, T> where T : new() { }
    public interface ICommand : ICommand<object> { }

    public interface IMessageCommand<ParameterType> : IGenericCommand<Message, ParameterType> where ParameterType : new() { }
    public interface IMessageCommand : IMessageCommand<object> { }

    public interface ICallbackQueryCommand<ParameterType> : IGenericCommand<CallbackQuery, ParameterType> where ParameterType : new() { }
    public interface ICallbackQueryCommand : ICallbackQueryCommand<object> { }

    public interface IInlineQueryCommand<ParameterType> : IGenericCommand<InlineQuery, ParameterType> where ParameterType : new() { }
    public interface IInlineQueryCommand : IInlineQueryCommand<object> { }
}