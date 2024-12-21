using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;

namespace CommandLineParser.Utils;

internal static class EnumUtils
{
	public static bool TryParse(Type enumType, ReadOnlySpan<char> value, [NotNullWhen(true)] out object? result)
	{
		foreach (string name in Enum.GetNames(enumType))
		{
			var field = enumType.GetField(name)!;
			var attrib = field.GetCustomAttribute<EnumMemberAttribute>(true);
			if (attrib is null ? value.Equals(name, StringComparison.Ordinal) : (attrib.IsValueSetExplicitly && value.Equals(attrib.Value, StringComparison.Ordinal)))
			{
				result = Convert.ChangeType(field.GetValue(null), enumType)!;
				return true;
			}
		}

		result = null;
		return false;
	}

	public static IEnumerable<string> GetNames(Type enumType)
	{
		foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
		{
			var attrib = field.GetCustomAttribute<EnumMemberAttribute>();

			yield return attrib is not null && attrib.IsValueSetExplicitly && !string.IsNullOrEmpty(attrib.Value)
				? attrib.Value
				: field.Name;
		}
	}

	public static string GetName(object value)
	{
		Type enumType = value.GetType();

		if (!enumType.IsEnum)
		{
			throw new ArgumentException($"{nameof(value)} must be an enum.", nameof(value));
		}

		var field = enumType.GetField(value.ToString()!, BindingFlags.Public | BindingFlags.Static)!;

		var attrib = field.GetCustomAttribute<EnumMemberAttribute>();

		return attrib is not null && attrib.IsValueSetExplicitly && !string.IsNullOrEmpty(attrib.Value)
			? attrib.Value
			: field.Name;
	}
}
