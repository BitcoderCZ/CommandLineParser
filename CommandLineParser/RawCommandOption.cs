using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser
{
    public readonly struct RawCommandOption
    {
        public readonly string Name;
        public readonly string Value;
        public readonly bool IsLongName;

        public RawCommandOption(string name, string value, bool isLongName)
        {
            Name = name;
            Value = value;
            IsLongName = isLongName;
        }

        public void Deconstruct(out string name, out string value, out bool isLongName)
        {
            name = Name;
            value = Value;
            isLongName = IsLongName;
        }
    }
}
