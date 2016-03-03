using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommandLineArgs
{
    public class ConsoleApp
    {
        public List<ConsoleAppParams> Params = new List<ConsoleAppParams>();
        public List<MethodInfo> Commands = new List<MethodInfo>();
        public bool PrintHelp = false;
        public bool AnyCommandRun = false;
        public int NumberOfRunCommands = 0;
        public int NumberOfFailedCommands = 0;

        public static T FromCommandLineArgs<T>(params string[] args)
        {
            T ret = Activator.CreateInstance<T>();

            ConsoleAppParams appParams = new ConsoleAppParams();
            appParams.AddTarget(ret);
            appParams.AddArgs(args);
            if (!appParams.Bind())
            {
                // TODO: improve error experience
                throw new BindParamsException("Could not bind args");
            }

            return ret;
        }

        // TODO: for single assembly
        //public static int StartApp<T>(string[] args)
        //{
        //}

        // TODO: Currently runs for all assemblies
        //       it should have an option to choose
        public static int StartApp<T>(string[] args)
        {
            // Assembly.GetEntryAssembly is missing, GetCallingAssembly is missing too
            Assembly assembly = typeof(T).GetTypeInfo().Assembly;
            ConsoleApp app = new ConsoleApp();

            foreach (var type in assembly.DefinedTypes)
            {
                if (type.AsType().GetConstructor(Type.EmptyTypes) == null)
                {
                    // no parameterless constructor
                    continue;
                }

                object target = Activator.CreateInstance(type.AsType());
                var typeParams = new ConsoleAppParams();
                typeParams.AddTarget(target);

                app.Params.Add(typeParams);

                var typeCommands = type.DeclaredMethods.GetCommands();
                app.Commands.AddRange(typeCommands);
                IEnumerable<MethodInfo> matchedCommands = null;

                string command = null;
                // Try get command and matched commands from first arg
                if (args.Length > 0)
                {
                    command = args[0];
                    matchedCommands = typeCommands.MatchName(command);
                    if (matchedCommands.Any())
                    {
                        args = args.Slice(1);
                    }
                    else
                    {
                        command = null;
                    }
                }

                typeParams.AddArgs(args);

                // Try get command and matched commands from default command
                if (command == null)
                {
                    var defaultCmd = type.GetCustomAttribute(typeof(DefaultCommandAttribute)) as DefaultCommandAttribute;
                    if (defaultCmd != null)
                    {
                        command = defaultCmd.Command;
                        typeParams.AddArgs(defaultCmd.Args);
                        matchedCommands = typeCommands.MatchName(command);
                        if (!matchedCommands.Any())
                        {
                            command = null;
                        }
                    }
                }

                if (command == null || !typeParams.Bind())
                {
                    app.PrintHelp = true;
                    continue;
                }

                foreach (var method in matchedCommands)
                {
                    // TODO: feels like adding arguments here and below wouldn't be that hard
                    try
                    {
                        Console.WriteLine($"---=== Running `{method.Name}` ===---");
                        method.Invoke(
                            obj: method.IsStatic ? null : target,
                            parameters: null);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"---=== Succeeded `{method.Name}` ===---");
                        Console.ResetColor();
                    }
                    catch (TargetInvocationException wrapped)
                    {
                        app.NumberOfFailedCommands++;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine(wrapped.InnerException);
                        Console.Error.WriteLine($"---=== Failed `{method.Name}` ===---");
                        Console.ResetColor();
                    }

                    app.AnyCommandRun = true;
                    app.NumberOfRunCommands++;
                }
            }

            app.PrintHelp &= !app.AnyCommandRun;

            if (app.PrintHelp)
            {
                app.DefaultPrintUsage();
            }

            if (app.AnyCommandRun)
            {
                app.PrintReport();
            }

            return app.AnyCommandRun ? 0 : 1;
        }

        public void PrintListOfParams()
        {
            bool headerPrinted = false;
            foreach (var typeParams in Params)
            {
                foreach (var param in typeParams)
                {
                    if (!headerPrinted)
                    {
                        Console.WriteLine("Usage:");
                        headerPrinted = true;
                    }

                    Console.Write($"{param.ToString().PadLeft(20)}");
                }
            }
        }

        // TODO: most of the print methods look pretty similar...
        public void PrintListOfCommands()
        {
            bool headerPrinted = false;
            foreach (var command in Commands)
            {
                if (!headerPrinted)
                {
                    Console.WriteLine("Commands:");
                    headerPrinted = true;
                }

                Console.WriteLine($"    {command.Name}");
            }
        }

        // TODO: add customization of help
        public void DefaultPrintUsage()
        {
            PrintListOfParams();
            PrintListOfCommands();
        }

        public void PrintReport()
        {
            if (NumberOfRunCommands >= 2)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"---=== Finished running {NumberOfRunCommands} commands ===---");
                Console.ResetColor();

                if (NumberOfFailedCommands > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"---=== {NumberOfFailedCommands} of them failed ===---");
                    Console.ResetColor();
                }
            }
        }
    }
}
