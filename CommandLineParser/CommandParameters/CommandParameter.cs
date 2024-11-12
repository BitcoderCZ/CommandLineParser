using System.Reflection;
using CommandLineParser.Attributes;
using CommandLineParser.Commands;
using CommandLineParser.Exceptions;
using CommandLineParser.Utils;

namespace CommandLineParser.CommandParameters;

internal abstract class CommandParameter
{
    protected readonly PropertyInfo _prop;

    protected readonly HelpTextAttribute? helpTextAttribute;
    protected readonly RequiredAttribute? requiredAttrib;
    protected readonly GreaterThanAttribute? greaterThanAttrib;
    protected readonly LessThanAttribute? lessThanAttrib;

    protected CommandParameter(PropertyInfo prop, Type commandType)
    {
        if (!(prop.CanRead && prop.CanWrite && (prop.GetGetMethod(true)?.IsPublic ?? false) && (prop.GetSetMethod(true)?.IsPublic ?? false)))
        {
            throw new ArgumentException($"{nameof(prop)} must have a public getter and setter.", nameof(prop));
        }

        if (!ConsoleCommand.IsCommand(commandType))
        {
            throw new ArgumentException($"{nameof(commandType)} must be {nameof(ConsoleCommand)}.", nameof(commandType));
        }

        _prop = prop;
        CommandType = commandType;

        helpTextAttribute = _prop.GetCustomAttribute<HelpTextAttribute>();
        requiredAttrib = _prop.GetCustomAttribute<RequiredAttribute>();
        greaterThanAttrib = _prop.GetCustomAttribute<GreaterThanAttribute>();
        lessThanAttrib = _prop.GetCustomAttribute<LessThanAttribute>();
    }

    /// <summary>
    /// The <see cref="ConsoleCommand"/> this parameter belongs to.
    /// </summary>
    public Type CommandType { get; }

    /// <summary>
    /// The type of this parameter.
    /// </summary>
    public Type Type => _prop.PropertyType;

    public string PropName => _prop.Name;

    public string? HelpText => helpTextAttribute?.HelpText;

    public bool IsRequired => requiredAttrib is not null;

    public bool HasRangeRequirements => greaterThanAttrib is not null || lessThanAttrib is not null;

    public abstract string GetNames();

    public object? GetValue(ConsoleCommand instance)
        => _prop.GetGetMethod()!.Invoke(instance, []);

    public void SetValue(ConsoleCommand instance, object? value)
    {
        if (HasRangeRequirements)
        {
            var (greaterThan, lessThan) = GetRangeValues();

            if (greaterThan is not null)
            {
                int compVal;

                try
                {
                    compVal = ((IComparable)greaterThan).CompareTo(value);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to compare value assigned to option '{GetNames()}' ({value?.GetType().FullName ?? "null"}) with {nameof(GreaterThanAttribute)}.{nameof(GreaterThanAttribute.Value)} ({greaterThan.GetType()}).", ex);
                }

                if (compVal != -1)
                {
                    throw new ValueOutOfRangeException(GetNames(), $"Value ({ObjectUtils.ToString(value)}) must be greater than '{ObjectUtils.ToString(greaterThan)}'", CommandType);
                }
            }

            if (lessThan is not null)
            {
                int compVal;

                try
                {
                    compVal = ((IComparable)lessThan).CompareTo(value);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to compare value assigned to option '{GetNames()}' ({value?.GetType().FullName ?? "null"}) with {nameof(LessThanAttribute)}.{nameof(LessThanAttribute.Value)} ({lessThan.GetType()}).", ex);
                }

                if (compVal != 1)
                {
                    throw new ValueOutOfRangeException(GetNames(), $"Value ({ObjectUtils.ToString(value)}) must be less than '{ObjectUtils.ToString(greaterThan)}'", CommandType);
                }
            }
        }

        _prop.GetSetMethod()!.Invoke(instance, [value]);
    }

    public (object? GreaterThan, object? LessThan) GetRangeValues()
        => (greaterThanAttrib?.Value, lessThanAttrib?.Value);
}
