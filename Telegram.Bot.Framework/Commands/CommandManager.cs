using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Framework.AccessRules;
using Telegram.Bot.Framework.Attributes;
using Telegram.Bot.Framework.Entities;
using Telegram.Bot.Types;

namespace Telegram.Bot.Framework.Commands
{
    public class CommandManager
    {
        private const string COMMAND_START = "/";
        private readonly List<object> _commands = new List<object>();
        private readonly string _botName = string.Empty;
        #region Register Command
        //public void RegisterCommand(ICommand command)
        //{
        //    RegisterCommand(command as object);
        //}
        //public void RegisterCommand<T>(ICommand<T> command) where T : new()
        //{
        //    RegisterCommand(command as object);
        //}
        //public void RegisterCommand(IMessageCommand command)
        //{
        //    RegisterCommand(command as object);
        //}
        //public void RegisterCommand<T>(IMessageCommand<T> command) where T : new()
        //{
        //    RegisterCommand(command as object);
        //}
        //public void RegisterCommand(ICallbackQueryCommand command)
        //{
        //    RegisterCommand(command as object);
        //}
        //public void RegisterCommand<T>(ICallbackQueryCommand<T> command) where T : new()
        //{
        //    RegisterCommand(command as object);
        //}
        //public void RegisterCommand(IInlineQueryCommand command)
        //{
        //    RegisterCommand(command as object);
        //}
        //public void RegisterCommand<T>(IInlineQueryCommand<T> command) where T : new()
        //{
        //    RegisterCommand(command as object);
        //}
        public void RegisterCommand(object command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (command?.GetType().IsSubclassOf(typeof(IGenericCommand<,>)) ?? false == false) throw new Exception("Command is not inheriting any ICommand interface");
            if (GetCustomAttributes<CommandAttribute>(command).Count == 0) throw new Exception("Missing CommandAttribute at command " + command.GetType().Name + ".");

            _commands.Add(command);
        }
        #endregion

        public CommandManager(string botName)
        {
            _botName = botName;
        }

        public async Task<CommandMatch> GetCommandMatch(ITelegramBotClient client, Update update)
        {
            string q = null;
            CommandUsage usage = CommandUsage.All;

            switch (update.Type)
            {
                case Types.Enums.UpdateType.Message:
                    q = update.Message.Text;
                    usage = CommandUsage.Command;
                    break;
                case Types.Enums.UpdateType.CallbackQuery:
                    q = update.CallbackQuery.Data;
                    usage = CommandUsage.CallbackQueryCommand;
                    break;
                case Types.Enums.UpdateType.ChannelPost:
                    q = update.ChannelPost.Text;
                    usage = CommandUsage.ChannelPost;
                    break;
                default:
                    break;
            }

            CommandMatch match = await GetCommandMatch(client, update, q, usage);
            if (match == null && q != null)
                match = await GetCommandMatch(client, update, null, usage);
            return match;
        }
        private async Task<CommandMatch> GetCommandMatch(ITelegramBotClient client, Update update, string q, CommandUsage usage)
        {
            if (q != null || (q == null && update.Type == Types.Enums.UpdateType.Message))
            {
                q = q?.ToLower();
                List<CommandMatch> commandMatches = GetCommandMatches(_commands, q, usage);
                foreach (var item in commandMatches)
                {
                    object commandArg = BuildCommandArgObject(q, item);
                    if (!await CheckAccessRules(client, update, item.Command, commandArg))
                        continue;
                    var item2 = item;
                    item2.Args = commandArg;
                    Type parametersType = commandArg?.GetType() ?? typeof(object);
                    Type commandType = item.Command.GetType();

                    if (GetInvokeMethodInfo(client, update, item.Command, nameof(IMessageCommand.CanInvoke), commandArg) is Func<ITelegramBotClient, Update, object, bool> canInvokeMethodInfo)
                        item2.CanInvokeMethodInfo = canInvokeMethodInfo;
                    if (GetInvokeMethodInfo(client, update, item.Command, nameof(IMessageCommand.Invoke), commandArg) is Func<ITelegramBotClient, Update, object, bool> invokeMethodInfo)
                        item2.InvokeMethodInfo = invokeMethodInfo;

                    if (item.CanInvoke(client, update))
                        return item;
                }
            }
            return null;
        }
        private async Task<bool> CheckAccessRules(ITelegramBotClient client, Update update, object obj, object args)
        {
            try
            {
                if (GetCustomAttributes<AccessRuleAttribute>(obj) is List<AccessRuleAttribute> rules)
                    foreach (IAccessRule rule in rules.Select(rule => Activator.CreateInstance(rule.Rule) as IAccessRule))
                        if (await rule.HasAccess(client, update, args) == false)
                            return false;
            }
            catch { return false; }
            return true;
        }
        private async Task<bool> GetFunc(IAccessRule rule, ITelegramBotClient client, Update update, object args)
        {
            return await rule.HasAccess(client, update, args);
        }
        private List<CommandMatch> GetCommandMatches(List<object> commands, string query, CommandUsage usage)
        {
            query = query?.Replace("@" + _botName, "");
            return commands
                       .SelectMany(cmd =>
                          GetCustomAttributes<CommandAttribute>(cmd)
                          .Select(attr => new CommandMatch(cmd, attr))
                          .Where(match => match.CommandAttribute.CommandUsage.HasFlag(usage) && ((query == null && match.CommandAttribute.Expression == null) || (query != null && match.CommandAttribute.RegularExpression?.IsMatch(query) == true)))
                          )
                          .ToList();
        }
        private Func<ITelegramBotClient, Update, object, bool> GetInvokeMethodInfo(ITelegramBotClient client, Update update, object command, string methodName, object commandArg)
        {
            MethodInfo methodInfo = GetMethodInfo(update, command, methodName, commandArg, out object messageObj);


            if (methodInfo != null)
            {
                return (c, message, arg) =>
                {
                    try
                    {
                        MethodInfo mInfo = GetMethodInfo(message, command, methodName, commandArg, out object msgObj);
                        object result = mInfo.Invoke(command, new object[] { client, msgObj, commandArg });
                        if (result is Task<bool> task)
                            return task.Result;
                        return result as bool? ?? true;
                    }
                    catch (Exception ex)
                    {
                        return true;
                    }
                };
            }
            return null;
        }
        private MethodInfo GetMethodInfo(Update update, object command, string methodName, object commandArg, out object messageObj)
        {
            Type commandType = command?.GetType();
            Type parametersType = commandArg?.GetType() ?? typeof(object);
            Type[] defaultSignature = new Type[] { typeof(ITelegramBotClient), typeof(Update), parametersType };
            Type[] specificSignature = new Type[] { typeof(ITelegramBotClient), typeof(Update), parametersType };
            messageObj = GetMessageObj(update);
            if (messageObj != null)
                specificSignature[1] = messageObj.GetType();

            MethodInfo methodInfo = commandType.GetMethod(methodName, specificSignature);
            if (methodInfo == null)
            {
                methodInfo = commandType.GetMethod(methodName, defaultSignature);
                messageObj = update;
            }

            return methodInfo;
        }
        private object GetMessageObj(Update update)
        {
            object messageObj = null;
            switch (update.Type)
            {
                case Types.Enums.UpdateType.Message:
                    messageObj = update.Message;
                    break;
                case Types.Enums.UpdateType.CallbackQuery:
                    messageObj = update.CallbackQuery;
                    break;
                case Types.Enums.UpdateType.InlineQuery:
                    messageObj = update.InlineQuery;
                    break;
                case Types.Enums.UpdateType.EditedMessage:
                    messageObj = update.EditedMessage;
                    break;
                case Types.Enums.UpdateType.ChannelPost:
                    messageObj = update.ChannelPost;
                    break;
                case Types.Enums.UpdateType.ChosenInlineResult:
                    messageObj = update.ChosenInlineResult;
                    break;
                case Types.Enums.UpdateType.EditedChannelPost:
                    messageObj = update.EditedChannelPost;
                    break;
                case Types.Enums.UpdateType.PreCheckoutQuery:
                    messageObj = update.PreCheckoutQuery;
                    break;
                case Types.Enums.UpdateType.ShippingQuery:
                    messageObj = update.ShippingQuery;
                    break;
                case Types.Enums.UpdateType.Unknown:
                case Types.Enums.UpdateType.Poll:
                //messageObj = update.Poll;
                //break;
                default:
                    messageObj = update;
                    break;
            }
            return messageObj;
        }
        public object BuildCommandArgObject(string query, CommandMatch commandMatch)
        {
            if (query == null)
                return null;
            Regex regex = commandMatch.CommandAttribute.RegularExpression;
            if (regex.IsMatch(query) && commandMatch.CommandAttribute.ArgumentType is Type argumentType)
            {
                try
                {
                    Match match = regex.Match(query);
                    string[] parameterNames = commandMatch.CommandAttribute.Parameters;
                    if (parameterNames?.Length == 1 && (argumentType.IsPrimitive || argumentType == typeof(string)))
                    {
                        string value = match.Groups[1].Value;
                        Type propertyType = Nullable.GetUnderlyingType(argumentType) ?? argumentType;
                        return Convert.ChangeType(value, propertyType);
                    }
                    else if (Activator.CreateInstance(argumentType) is object o)
                    {
                        if (argumentType.GetProperties().Where(prop => prop.SetMethod != null).ToArray() is PropertyInfo[] pInfos)
                        {

                            for (int i = 0; i < match.Groups.Count - 1 && i < parameterNames.Length; i++)
                            {
                                try
                                {
                                    string value = match.Groups[i + 1].Value;
                                    if (pInfos.FirstOrDefault(p => p.Name.ToLower() == parameterNames[i].ToLower()) is PropertyInfo pInfo)
                                    {
                                        Type propertyType = Nullable.GetUnderlyingType(pInfo.PropertyType) ?? pInfo.PropertyType;
                                        object propertyValue = Convert.ChangeType(value, propertyType);
                                        pInfo.SetValue(o, propertyValue);
                                    }
                                }
                                catch { }
                            }
                        }
                        return o;
                    }
                }
                catch { }
            }
            return null;
        }
        private List<T> GetCustomAttributes<T>(object obj, bool inherit = true) where T : Attribute
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return obj.GetType().GetCustomAttributes(typeof(T), inherit).Cast<T>().ToList();
        }
    }
}
