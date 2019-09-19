using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Framework.AccessRules;

namespace Telegram.Bot.Framework.Attributes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class AccessRuleAttribute : Attribute
    {
        readonly Type _rule;
        public AccessRuleAttribute(Type rule)
        {
            if (!typeof(IAccessRule).IsAssignableFrom(rule)) throw new ArgumentException(nameof(rule) + " is not an ancestor of " + nameof(IAccessRule));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public Type Rule { get => _rule; }
    }
}
