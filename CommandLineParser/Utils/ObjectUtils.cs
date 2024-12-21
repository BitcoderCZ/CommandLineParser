using System.Globalization;

namespace CommandLineParser.Utils;

internal static class ObjectUtils
{
	public static string? ToString(object? obj)
		=> obj is null
		? null
		: obj is Enum
		? EnumUtils.GetName(obj)
		: obj is IFormattable formattable
		? formattable.ToString(null, CultureInfo.InvariantCulture)
		: obj.ToString();
}