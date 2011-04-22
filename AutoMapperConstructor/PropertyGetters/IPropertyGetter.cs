using System;
using System.Reflection;

namespace AutoMapperConstructor.PropertyGetters
{
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
        /// Try to retrieve the value of the specified Property from the specified object (which must be of type SrcType)
        /// </summary>
        object GetValue(object src);
    }
}
