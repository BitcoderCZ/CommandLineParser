using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser
{
    public sealed class ParseOptions
    {
        public bool ThrowOnDuplicateArgument { get; set; } = true;
    }
}
