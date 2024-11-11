using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;

namespace CommandLineParser.Utils;

internal static class TypeUtils
{
    public static bool HasGenericInterface(this Type type, Type interfaceType)
        => type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType)
            is not null;

    public static bool IsNumber(Type type)
        => type.HasGenericInterface(typeof(INumber<>));

    public static bool IsInteger(Type type)
        => type.HasGenericInterface(typeof(IBinaryInteger<>));

    public static bool IsFloatingPoint(Type type)
        => type.HasGenericInterface(typeof(IFloatingPoint<>));

    public static bool TryGetMinMaxValues(Type type, [NotNullWhen(true)] out object? min, [NotNullWhen(true)] out object? max)
    {
        if (!type.HasGenericInterface(typeof(IMinMaxValue<>)))
        {
            min = null;
            max = null;
            return false;
        }

        InterfaceMapping mapping = type.GetInterfaceMap(typeof(IMinMaxValue<>).MakeGenericType([type]));

        min = mapping.GetMappedMethod("get_MinValue")!.Invoke(null, []);
        max = mapping.GetMappedMethod("get_MaxValue")!.Invoke(null, []);

        return min is not null && max is not null;
    }

    public static MethodInfo? GetMappedMethod(this InterfaceMapping mapping, string name)
    {
        for (int i = 0; i < mapping.InterfaceMethods.Length; i++)
        {
            if (mapping.InterfaceMethods[i].Name == name)
            {
                return mapping.TargetMethods[i];
            }
        }

        return null;
    }
}
