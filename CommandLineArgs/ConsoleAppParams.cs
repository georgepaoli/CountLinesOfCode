using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CommandLineArgs
{
    public class ConsoleAppParams : List<ParameterInformation>
    {
        public CommandLineArgs Args = new CommandLineArgs();

        public static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;
        public Dictionary<string, ParameterInformation> NameToParam = new Dictionary<string, ParameterInformation>(Comparer);
        public LinkedList<ParameterInformation> Verbs = new LinkedList<ParameterInformation>();

        public void AddTarget(object target)
        {
            foreach (var field in target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                AddParameter(new ParameterInformation(target, field));
            }

            foreach (var param in this)
            {
                foreach (var name in param.Names)
                {
                    if (!NameToParam.TryAdd(name, param))
                    {
                        Console.Error.WriteLine($"Duplicate name `{name}`.");
                        Console.Error.WriteLine($"First orrucance has type `{NameToParam[name].Field.FieldType.FullName}`.");
                        Console.Error.WriteLine($"Another occurance has type `{param.Field.FieldType.FullName}`.");
                    }
                }

                if (param.IsVerb)
                {
                    Verbs.AddLast(param);
                }
            }
        }

        // TODO: this looks bad
        public void AddArgs(string[] args)
        {
            if (args != null)
            {
                Args.AddArgs(args);
            }
        }

        public static bool TryFromCommandLineArgs<T>(string[] args, out ConsoleAppParams app)
        {
            app = new ConsoleAppParams();
            app.AddTarget(Activator.CreateInstance<T>());
            app.AddArgs(args);
            if (!app.Bind())
            {
                return false;
            }

            return true;
        }

        public void AddParameter(ParameterInformation parameterInformation)
        {
            Add(parameterInformation);
        }

        private static string GetPrintableString(string s)
        {
            return s == null ? "null" : $"`{s}`";
        }

        private bool BindByNameOrVerb()
        {
            foreach (var arg in Args)
            {
                if (arg.OriginalValue == "--")
                {
                    Args.ForceNextWave();
                    continue;
                }

                ParameterInformation param;
                if (NameToParam.TryGetValue(arg.Name, out param))
                {
                    if (param.StopProcessingNamedArgsAfterThis)
                    {
                        Args.ForceNextWave();
                    }

                    // TODO: should this take an arg itself?
                    if (param.TryBindValue(arg.Value))
                    {
                        continue;
                    }

                    if (arg.Operator != null)
                    {
                        Console.Error.WriteLine($"Warning: Unrecognized value: {GetPrintableString(arg.Value)} for {GetPrintableString(arg.Name)}.");
                        Console.Error.WriteLine($"Warning: Expected type: {GetPrintableString(param.Field.FieldType.FullName)}.");
                        Console.Error.WriteLine($"Warning: Note: Type might not be supported or there might be something wrong with this library");
                        Console.Error.WriteLine($"Warning:       File an issue if you think it is wrong");
                        continue;
                    }

                    // try use next arg as value
                    CommandLineArg nextArg = Args.PeekNext();
                    if (nextArg != null && param.TryBindValue(nextArg.OriginalValue))
                    {
                        Args.Skip();
                        continue;
                    }

                    // bool doesn't require value
                    // TODO: There should be a way of assigning value directly or using different converter
                    if (param.TryBindValue("true"))
                    {
                        continue;
                    }
                }

                LinkedListNode<ParameterInformation> node = Verbs.First;
                for (; node != null; node = node.Next)
                {
                    if (node.Value.TryBindValue(arg.OriginalValue))
                    {
                        break;
                    }
                }

                if (node != null)
                {
                    if (node.Value.StopProcessingNamedArgsAfterThis)
                    {
                        Args.ForceNextWave();
                    }

                    Verbs.Remove(node);
                    continue;
                }

                Args.ProcessCurrentArgLater();
            }

            return true;
        }

        private bool BindByCombinableCharacter()
        {
            // TODO: this looks horrible
            // Special logic for cases like: git clean -fdx (equivalent to: git clean -x -d- f)
            foreach (var arg in Args)
            {
                if (!arg.OriginalValue.StartsWith("-"))
                {
                    Args.ProcessCurrentArgLater();
                    continue;
                }

                bool argUsed = true;
                foreach (var letter in arg.OriginalValue.Skip(1))
                {
                    bool letterUsed = false;
                    foreach (var param in this)
                    {
                        if (param.CombinableSingleLetterAliases.Contains(letter))
                        {
                            // TODO: for now errors ok, should print a warning though
                            // TODO: This should be: CanBindValue + BindValue in the second loop if all of them used
                            if (param.TryBindValue("true"))
                            {
                                letterUsed = true;
                            }
                        }
                    }

                    if (!letterUsed)
                    {
                        argUsed = false;
                    }
                }

                if (!argUsed)
                {
                    Args.ProcessCurrentArgLater();
                    // TODO: return false if at least one letter used?
                }
            }

            return true;
        }

        private bool BindByUnnamedArg()
        {
            foreach (var arg in Args)
            {
                foreach (var param in this)
                {
                    if (param.MaxArgsToPop == 0 && !param.PopsRemainingArgs)
                    {
                        continue;
                    }

                    if (param.TryBindValue(arg.OriginalValue))
                    {
                        if (param.MaxArgsToPop > 0)
                        {
                            param.MaxArgsToPop--;
                        }

                        // not convinced that bool flag will be more readable
                        goto ConsumeArg;
                    }
                }

                Args.ProcessCurrentArgLater();
                ConsumeArg:;
            }

            return true;
        }

        private bool CheckUnusedArg()
        {
            bool ret = true;
            if (!Args.Empty)
            {
                foreach (var arg in Args)
                {
                    Console.Error.WriteLine($"Error: Unused arg: {arg}");
                }

                ret = false;
            }

            return ret;
        }

        private bool CheckRequiredParams()
        {
            bool ret = true;
            foreach (var param in this)
            {
                if (param.Required && param.NumberOfArgsBound == 0)
                {
                    if (!param.Required.SupressMessages)
                    {
                        Console.Error.WriteLine($"Error: Required param `{param}` not provided.");
                    }

                    ret = false;
                }
            }

            return ret;
        }

        // TODO: should this method not return anything and just throw? right now it is mixed :P
        // TODO: split or leave as is (easy to understand vs easy to fix)
        public bool Bind()
        {
            if (!BindByNameOrVerb())
            {
                return false;
            }

            if (!BindByCombinableCharacter())
            {
                return false;
            }

            if (!BindByUnnamedArg())
            {
                return false;
            }

            // watch out when refactoring
            // this is not equivalent to: return BindByUnusedArg() && CheckRequiredParams(); (second one might not get executed)
            bool ret = CheckUnusedArg();
            ret &= CheckRequiredParams();

            return ret;
        }
    }
}
