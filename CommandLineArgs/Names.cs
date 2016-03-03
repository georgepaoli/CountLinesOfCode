using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommandLineArgs
{
    public class Names : List<string>
    {
        public Func<string> GetDefaultNames;

        public override string ToString()
        {
            string ret = string.Join("|", this);

            if (string.IsNullOrWhiteSpace(ret))
            {
                return (GetDefaultNames ?? (() => (string)null))();
            }

            return ret;
        }
    }
}
