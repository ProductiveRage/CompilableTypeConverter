using System;
using System.Reflection;
using AutoMapper;

namespace AutoMapperConstructor.PropertyGetters
{
    /// <summary>
    /// AbstractGenericPropertyGetter implementation using reflection to retrieve the specified property value from the source type and AutoMapper to translate the
    /// value into the required type - if AutoMapper is not able to perform the property value mapping, an exception may be thrown from the GetValue method
    /// </summary>
    /// <typeparam name="TSourceObject">This is the type of the target object, whose property is to be retrieved</typeparam>
    /// <typeparam name="TPropertyAsRetrieved">This is the type that the property's value will be returned as</typeparam>
    public class AutoMapperEnabledPropertyGetter<TSourceObject, TPropertyAsRetrieved> : IPropertyGetter
    {
        private PropertyInfo _propertyInfo;
        private IMappingEngine _mappingEngine;
        public AutoMapperEnabledPropertyGetter(PropertyInfo propertyInfo, IMappingEngine mappingEngine)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException("propertyInfo");
            if (!propertyInfo.DeclaringType.Equals(typeof(TSourceObject)))
                throw new ArgumentException("Invalid propertyInfo - DeclaringType must match class typeparam");
            if (mappingEngine == null)
                throw new ArgumentNullException("mappingEngine");

            _propertyInfo = propertyInfo;
            _mappingEngine = mappingEngine;
        }

        /// <summary>
        /// This is the type whose property is being accessed
        /// </summary>
        public Type SrcType
        {
            get { return typeof(TSourceObject); }
        }

        /// <summary>
        /// This is the property on the source type whose value is to be retrieved
        /// </summary>
        public PropertyInfo Property
        {
            get { return _propertyInfo; }
        }

        /// <summary>
        /// This is the type that the property value should be converted to and returned as
        /// </summary>
        public Type TargetType
        {
            get { return typeof(TPropertyAsRetrieved); }
        }

        public TPropertyAsRetrieved GetValue(TSourceObject src)
        {
            if (src == null)
                throw new ArgumentNullException("src");

            var value = _propertyInfo.GetValue(src, null);
            if (value == null)
                return default(TPropertyAsRetrieved);

            return (TPropertyAsRetrieved)_mappingEngine.Map(
                value,
                value.GetType(),
                typeof(TPropertyAsRetrieved)
            );
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
    }
}
