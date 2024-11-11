namespace CommandLineParser.Utils;

internal static class ObjectUtils
{
    public static string? ToString(object? obj)
        => obj is null
        ? null
        : obj is Enum
        ? EnumUtils.GetName(obj)
        : obj.ToString();
}