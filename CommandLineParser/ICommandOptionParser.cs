using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser
{
    public interface ICommandOptionParser<T>
    {
        T Parse(ReadOnlySpan<char> value, ParseOptions options);
    }
}
