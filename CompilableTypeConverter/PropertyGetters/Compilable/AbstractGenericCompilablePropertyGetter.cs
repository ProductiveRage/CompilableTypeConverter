using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ProductiveRage.CompilableTypeConverter.PropertyGetters.Compilable
{
	/// <summary>
	/// This provides a lot of the plumbing for an ICompilablePropertyGetter implementation - the derived class only declare the Property value and override the
	/// GetPropertyGetterExpression method, all other ICompilablePropertyGetter and IPropertyGetter requirements will be satisfied here
	/// </summary>
	/// <typeparam name="TSourceObject">This is the type of the target object, whose property is to be retrieved</typeparam>
	/// <typeparam name="TPropertyAsRetrieved">This is the type that the property's value will be returned as</typeparam>
	public abstract class AbstractGenericCompilablePropertyGetter<TSourceObject, TPropertyAsRetrieved> : ICompilablePropertyGetter
    {
        private Lazy<Func<TSourceObject, TPropertyAsRetrieved>> _getter;
        public AbstractGenericCompilablePropertyGetter()
        {
            // Only perform the work of generating the Func<TSourceObject, TPropertyAsRetrieved> from the derived class' GetDirectGetterExpression once
            // - We want this to be thread-safe so that type converters using these property getters can be thread-safe as well
            _getter = new Lazy<Func<TSourceObject, TPropertyAsRetrieved>>(generateGetter, true);
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
        public abstract PropertyInfo Property { get; }

        /// <summary>
        /// This is the type that the property value should be converted to and returned as
        /// </summary>
        public Type TargetType
        {
            get { return typeof(TPropertyAsRetrieved); }
        }

		/// <summary>
		/// If the source value is null should this property getter still be processed? If not, the assumption is that the target property / constructor argument on
		/// the destination type will be assigned default(TPropertyAsRetrieved).
		/// </summary>
		public abstract bool PassNullSourceValuesForProcessing { get; }

        /// <summary>
        /// Try to retrieve the value of the specified Property from the specified object (which must be of type SrcType) - this will throw an exception for null input
        /// or if retrieval fails
        /// </summary>
        object IPropertyGetter.GetValue(object src)
        {
            if (src == null)
                throw new ArgumentNullException("src");
            if (!src.GetType().Equals(typeof(TSourceObject)))
                throw new ArgumentException("The type of src must match typeparam TSourceObject");
            return GetValue((TSourceObject)src);
        }

        /// <summary>
        /// Try to retrieve the value of the specified Property from the specified object
        /// </summary>
        public TPropertyAsRetrieved GetValue(TSourceObject src)
        {
            if (src == null)
                throw new ArgumentNullException("src");
            return _getter.Value(src);
        }

        /// <summary>
        /// This must return a Linq Expression that retrieves the value from SrcType.Property as TargetType - the specified "param" Expression must have a type that
        /// is assignable to SrcType. The resulting Expression must be assignable to a Lambda Expression typed as a TSourceObject to TPropertyAsRetrieved Func.
        /// </summary>
        public abstract Expression GetPropertyGetterExpression(Expression param);

        /// <summary>
        /// Use the derived class' GetDirectGetterExpression to generate a TSourceObject to TPropertyAsRetrieved Func that can be used to return
        /// data from the GetValue method.
        /// </summary>
        private Func<TSourceObject, TPropertyAsRetrieved> generateGetter()
        {
            var param = Expression.Parameter(typeof(TSourceObject), "src");
            return Expression.Lambda<Func<TSourceObject, TPropertyAsRetrieved>>(
                GetPropertyGetterExpression(param),
                param
            ).Compile();
        }
    }
}
