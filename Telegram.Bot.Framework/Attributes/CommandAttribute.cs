using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Telegram.Bot.Framework.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class CommandAttribute : Attribute
    {
        private static Regex _regex = new Regex(@"(\{.+?\})");
        private string _expression;
        private CommandUsage _commandUsage = CommandUsage.All;
        public virtual string Expression { get => _expression; set => SetExpression(value); }
        public CommandUsage CommandUsage { get => _commandUsage; set => _commandUsage = value; }
        public Type ArgumentType { get; set; } = null;
        public Regex RegularExpression { get; private set; }
        public bool IsUsingRegularExpression { get; private set; } = false;
        public string[] Parameters { get; private set; } = null;

        public CommandAttribute(string expression)
        {
            SetExpression(expression);
        }
        protected void SetExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                //throw new ArgumentException(nameof(expression) + " is empty.");
                _expression = null;
                return;
            }
            _expression = expression.Trim().ToLower();
            if (_regex.IsMatch(_expression))
            {
                IsUsingRegularExpression = true;
                string regexStr = "^" + _expression + "$";
                string[] splittedInput = _regex.Split(_expression);
                //StringBuilder stringBuilder = new StringBuilder("^");
                List<string> parameters = new List<string>();
                foreach (string item in splittedInput)
                {
                    string trimmed = item.Trim();
                    if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
                    {
                        regexStr = regexStr.Replace(item, "(.+)");
                       // stringBuilder.Append("(.+)");
                        parameters.Add(trimmed.Substring(1, trimmed.Length - 2).ToLower());
                    }
               //     else
           //             stringBuilder.Append(trimmed);
                }
                //stringBuilder.Append("$");
                RegularExpression = new Regex(regexStr);
                Parameters = parameters.ToArray();
            }
            else
                RegularExpression = new Regex("^" + _expression + "$");
        }

        public static List<CommandAttribute> GetAttributes(object obj)
        {
            if (obj != null)
                return obj.GetType().GetCustomAttributes(true).Where(attr => attr.GetType() == typeof(CommandAttribute)).Cast<CommandAttribute>().ToList();
            return null;
        }
    }
}
