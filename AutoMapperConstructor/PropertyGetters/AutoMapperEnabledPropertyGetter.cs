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
    public class AutoMapperEnabledPropertyGetter<TSourceObject, TPropertyAsRetrieved> : AbstractGenericPropertyGetter<TSourceObject, TPropertyAsRetrieved>
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

        public override PropertyInfo Property
        {
            get { return _propertyInfo; }
        }

        public override TPropertyAsRetrieved GetValue(TSourceObject src)
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
    }
}
