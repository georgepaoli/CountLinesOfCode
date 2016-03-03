using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineArgs {
    public class BindParamsException :Exception {
        public BindParamsException(string message) : base(message) {

        }
    }
}
