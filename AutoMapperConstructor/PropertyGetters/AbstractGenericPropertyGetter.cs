using System;
using System.Reflection;

namespace AutoMapperConstructor.PropertyGetters
{
    /// <summary>
    /// This base class extends IPropertyGetter such that the return type of the property to access is restricted to typeparam TPropertyAsRetrieved
    /// </summary>
    /// <typeparam name="TSourceObject">This is the type of the target object, whose property is to be retrieved</typeparam>
    /// <typeparam name="TPropertyAsRetrieved">This is the type that the property's value will be returned as</typeparam>
    public abstract class AbstractGenericPropertyGetter<TSourceObject, TPropertyAsRetrieved> : IPropertyGetter
    {
        /// <summary>
        /// This is the property on the source type
        /// </summary>
        public abstract PropertyInfo Property { get; }

        /// <summary>
        /// This is the type whose property is being accessed
        /// </summary>
        public Type SrcType
        {
            get { return typeof(TSourceObject); }
        }

        /// <summary>
        /// This is the type that the property value should be converted to and returned as
        /// </summary>
        public Type TargetType
        {
            get { return typeof(TPropertyAsRetrieved); }
        }

        /// <summary>
        /// Try to retrieve the value of the specified Property from the specified object (which must be of type SrcType)
        /// </summary>
        object IPropertyGetter.GetValue(object src)
        {
            if (src == null)
                throw new ArgumentNullException("src");
            if (!src.GetType().Equals(typeof(TSourceObject)))
                throw new ArgumentException("The type of src must match typeparam U");
            return GetValue((TSourceObject)src);
        }

        public abstract TPropertyAsRetrieved GetValue(TSourceObject src);
    }
}
