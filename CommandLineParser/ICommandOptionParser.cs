using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser
{
    public interface ICommandOptionParser
    {
        bool CanParse(Type type);

        object Parse(ReadOnlySpan<char> value, ParseOptions options);
    }
}
