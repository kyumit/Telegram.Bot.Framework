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

        public void RegisterCommand(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (GetCustomAttributes<CommandAttribute>(command).Count == 0) throw new Exception("Missing CommandAttribute at command " + command.GetType().Name + ".");

            _commands.Add(command);
        }
        public void RegisterCommand<T>(ICommand<T> command) where T : new()
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (GetCustomAttributes<CommandAttribute>(command).Count == 0) throw new Exception("Missing CommandAttribute at command " + command.GetType().Name + ".");

            _commands.Add(command);
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
                default:
                    break;
            }
            if (q != null)
            {
                q = q.ToLower();
                List<CommandMatch> commandMatches = GetCommandMatches(_commands, q, usage);
                foreach (var item in commandMatches)
                {
                    if (!CheckAccessRules(client, update, item.Command))
                        continue;
                    object commandArg = BuildCommandArgObject(q, item);
                    var item2 = item;
                    item2.Args = commandArg;
                    Type parametersType = commandArg?.GetType() ?? typeof(object);
                    Type commandType = item.Command.GetType();

                    if (GetInvokeMethodInfo(commandType, nameof(ICommand.CanInvoke), parametersType) is MethodInfo canInvokeMethodInfo)
                        item2.CanInvokeMethodInfo = canInvokeMethodInfo;
                    if (GetInvokeMethodInfo(commandType, nameof(ICommand.Invoke), parametersType) is MethodInfo invokeMethodInfo)
                        item2.InvokeMethodInfo = invokeMethodInfo;

                    if (await item.CanInvoke(client, update))
                        return item;
                }
            }
            return null;
        }
        private bool CheckAccessRules(ITelegramBotClient client, Update update, object obj)
        {
            if (GetCustomAttributes<AccessRuleAttribute>(obj) is List<AccessRuleAttribute> rules)
                return rules.Select(rule => Activator.CreateInstance(rule.Rule) as IAccessRule).All(rule => rule.HasAccess(client, update));
            return false;
        }
        private List<CommandMatch> GetCommandMatches(List<object> commands, string query, CommandUsage usage)
        {
          return  commands
                     .SelectMany(cmd =>
                        GetCustomAttributes<CommandAttribute>(cmd)
                        .Select(attr => new CommandMatch(cmd, attr))
                        .Where(match => match.CommandAttribute.CommandUsage.HasFlag(usage) && match.CommandAttribute.RegularExpression.IsMatch(query))
                        )
                        .ToList();
        }
        private MethodInfo GetInvokeMethodInfo(Type commandType, string methodName, Type parametersType)
        {
            return commandType.GetMethod(methodName, new Type[] { typeof(ITelegramBotClient), typeof(Update), parametersType });
        }
        public object BuildCommandArgObject(string query, CommandMatch commandMatch)
        {
            Regex regex = commandMatch.CommandAttribute.RegularExpression;
            if (regex.IsMatch(query) && commandMatch.CommandAttribute.ArgumentType is Type argumentType)
            {
                try
                {
                    if (Activator.CreateInstance(argumentType) is object o)
                    {
                        if (argumentType?.GetProperties().Where(prop => prop.SetMethod != null).ToArray() is PropertyInfo[] pInfos)
                        {
                            Match match = regex.Match(query);
                            string[] parameterNames = commandMatch.CommandAttribute.Parameters;
                            for (int i = 0; i < match.Groups.Count - 1 && i < parameterNames.Length; i++)
                            {
                                try
                                {
                                    string value = match.Groups[i + 1].Value;
                                    if (pInfos.FirstOrDefault(p => p.Name.ToLower() == parameterNames[i].ToLower()) is PropertyInfo pInfo)
                                        pInfo.SetValue(o, Convert.ChangeType(value, pInfo.PropertyType));
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
