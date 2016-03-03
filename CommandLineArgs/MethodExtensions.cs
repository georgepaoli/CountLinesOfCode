using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CommandLineArgs
{
    internal static class MethodExtensions
    {
        public static IEnumerable<MethodInfo> GetCommands(this IEnumerable<MethodInfo> methods)
        {
            foreach (var method in methods)
            {
                if (!method.IsPublic)
                {
                    continue;
                }

                if (method.ReturnType != typeof(void))
                {
                    continue;
                }

                if (method.GetParameters().Length != 0)
                {
                    // TODO: implement this
                    continue;
                }

                if (method.GetGenericArguments().Length != 0)
                {
                    // TODO: how would that work ???
                    continue;
                }

                yield return method;
            }
        }

        public static IEnumerable<MethodInfo> MatchName(this IEnumerable<MethodInfo> methods, string namePattern)
        {
            Regex regex = new Regex(namePattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            foreach (var method in methods)
            {
                if (regex.IsMatch(method.Name))
                {
                    yield return method;
                }
                else
                {
                    // TODO: heuristics for similar match?
                }
            }
        }
    }
}
