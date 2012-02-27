using System;
using System.Reflection;

namespace CompilableTypeConverter.PropertyGetters
{
    /// <summary>
    /// Note: IPropertyGetter can not specify typeparams as we're likely to need to maintain a list of these (eg. see SimpleTypeConverterByConstructor) before
    /// we know the types of the properties - we don't expect to know the TargetType values until runtime, even if we may know the SrcType at compile time
    /// </summary>
    public interface IPropertyGetter
    {
        /// <summary>
        /// This is the type whose property is being accessed
        /// </summary>
        Type SrcType { get; }

        /// <summary>
        /// This is the property on the source type whose value is to be retrieved
        /// </summary>
        PropertyInfo Property { get; }

        /// <summary>
        /// This is the type that the property value should be converted to and returned as
        /// </summary>
        Type TargetType { get; }

        /// <summary>
        /// Try to retrieve the value of the specified Property from the specified object (which must be of type SrcType) - this will throw an exception for null input
        /// or if retrieval fails
        /// </summary>
        object GetValue(object src);
    }
}
