using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommandLineArgs
{
    // base class
    public class BoolAttribute : Attribute
    {
        public static implicit operator bool (BoolAttribute attribute)
        {
            return attribute != null;
        }

        public override string ToString()
        {
            return (this != null) ? "true" : "false";
        }
    }

    public class StringAttribute : Attribute
    {
        public string Text;

        public static implicit operator string (StringAttribute attribute)
        {
            return attribute?.Text;
        }

        public override string ToString()
        {
            return (string)this;
        }
    }

    // TODO: add description for help - fields should work, rest of it not
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Assembly | AttributeTargets.Class)]
    public class DescriptionAttribute : StringAttribute
    {
        public DescriptionAttribute(string text)
        {
            Text = text;
        }
    }

    // TODO: This should accept following forms:
    // "name" - adds: --name, -name, /name
    // "-name" - adds only -name
    // "n|name" - adds -n --n /n --name -name /name
    // "-n|name"....
    // <in progress>

    /// <summary>
    /// Alternative name for parameter.
    /// This accepts parameters in following forms:
    /// [Alias("x")] will cause binding when any arg looks like any of the following: /x -x --x
    /// [Alias("x", "y")] will cause all of this to do the same: /x -x --x /y -y --y
    /// [Alias("x|y")] same as above
    /// [Alias("-x")] only -x
    /// [Alias("-x|--xxxxxx")] I think you already got the idea...
    /// Multiple attributes are allowed too
    /// Actually the code for this attribute is shorter than this description lol...
    /// 
    /// Everything not starting with latin letter or digit is treated as special character
    /// which is a marker for starting an arg (except pipe | as it is used as separator).
    /// If you think this should be changed then create an issue.
    /// 
    /// Check out also [NoDefaultAlias], [PopArg], [PopRemainingArgs] and [LastProcessedNamedArg]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
    public class AliasAttribute : Attribute
    {
        public List<string> Names = new List<string>();

        public AliasAttribute(params string[] names)
        {
            foreach (var name in names)
            {
                var parts = name.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (char.IsLetterOrDigit(part[0]))
                    {
                        Names.Add("-" + part);
                        Names.Add("--" + part);
                        Names.Add("/" + part);
                    }
                    else
                    {
                        Names.Add(part);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Disables automatically adding field name as an alias
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class NoDefaultAliasAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks parameter as required.
    /// Exception will be thrown when nothing bound.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class RequiredAttribute : BoolAttribute
    {
        // TODO: should this exist?
        public bool SupressMessages = false;
    }

    /// <summary>
    /// For single values: If not bound by name then tries to bind with first free argument from the left if available and when the conversion to target parameter is possible
    /// For arrays: Takes first free arg from the left if available and when the conversion to target parameter is possible
    /// Possible to use it multiple times on arrays (i.e. when exact amount of args to pop is known)
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class PopArgAttribute : Attribute
    {
    }

    /// <summary>
    /// Binds with all remaining args which can be converted to target parameter.
    /// This option has lowest priority which means that this action will occur after any other argument to parameter binding occurs.
    /// I.e.:
    /// test.exe /a /b /c /d e f g h
    /// with class which has fields: a,b,d,x where [PopRemainingArgs] is applied to x in this particular case
    /// it will not collect argument "/c" before making sure that there is no field with name c
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class PopRemainingArgsAttribute : Attribute
    {
    }

    /// <summary>
    /// This causes to stop processing any named arguments after processing this field.
    /// Has no effect if you put it in some weird combination (i.e. with [NoDefaultAlias])
    /// 
    /// Example:
    /// myapp --help somecommand
    /// myapp somecommand --help
    /// if you want to make sure the --help after somecommand won't be processed in the context of your app
    /// i.e. when you want to execute some other app and pass any remaining args put this attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class LastProcessedNamedArgAttribute : BoolAttribute
    {
    }

    // TODO: Fix the comment below (not sure if triple slash works with Task List)
    /// <summary>
    /// Causes unnamed args to be treated as fake named
    /// TODO: Fix this comment
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class VerbAttribute : BoolAttribute
    {
    }

    // TODO: allow multiple?
    // TODO: rename
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class DefaultCommandAttribute : Attribute
    {
        public string Command;
        public string[] Args;

        // TODO: add logic for splitting string from a single long string too
        // TODO: i.e. [DefaultCommand("asd", "/a /b -asd")]
        public DefaultCommandAttribute(string command, params string[] args)
        {
            Command = command;
            Args = args;
        }
    }

    // TODO: make sure this works
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    // TODO: : DefaultCommandAttribute should inherit from DefaultCommands - when inherited you can put multiple attributes - should multiple just be enabled?
    public class DefaultRunAllCommandsAttribute : Attribute
    {
        public DefaultRunAllCommandsAttribute() //: base(".*")
        {
        }
    }
}
