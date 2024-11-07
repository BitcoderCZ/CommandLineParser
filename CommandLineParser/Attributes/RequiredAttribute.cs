namespace CommandLineParser.Attributes
{
    /// <summary>
    /// The value of the property this attribute is applied to must be specified.
    /// if one or more <see cref="DependsOnAttribute"/> are applied, this attribute only takes effect if all of the properties have the specified value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class RequiredAttribute : Attribute
    {
    }
}
