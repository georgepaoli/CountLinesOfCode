using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommandLineArgs
{
    // DO NOT REFACTOR THIS ANYMORE!!! STAYS AS IS UNLESS ABSOLUTELY NECESSARY
    // THIS WAS ALREADY REFACTORED LIKE MILION TIMES AND KEEP COMING BACK TO SOMETHING SIMILAR TO THIS:
    public class CommandLineArg
    {
        public string OriginalValue;

        //[any special characters]Name[(=|:)[Value]]
        // unless you represent this as value and Regex
        public string Name;
        public string Operator;
        public string Value;

        public CommandLineArg(string originalValue)
        {
            OriginalValue = originalValue;

            int start = 0;
            // skip special characters
            for (; start < originalValue.Length; start++)
            {
                if (char.IsLetterOrDigit(originalValue[start]))
                {
                    break;
                }
            }

            // search for name-value separator
            int p = originalValue.IndexOfAny(new char[] { ':', '=' }, start);
            if (p == -1)
            {
                Name = originalValue;
            }
            else
            {
                Name = originalValue.Substring(0, p);
                Operator = originalValue.Substring(p, 1);
                Value = originalValue.Substring(p + 1);
            }
        }

        public override string ToString()
        {
            return OriginalValue;
        }
    }
}
