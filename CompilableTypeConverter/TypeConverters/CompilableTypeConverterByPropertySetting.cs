using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CompilableTypeConverter.Common;
using CompilableTypeConverter.PropertyGetters.Compilable;

namespace CompilableTypeConverter.TypeConverters
{
    /// <summary>
    /// This will convert from one class to another by instantiating the target through a parameterless constructor and then setting individual properties. The
    /// conversion process will be compiled using LINQ Expressions with the aim of the resulting code being comparable in speed to a hand-crafted version.
    /// </summary>
    public class CompilableTypeConverterByPropertySetting<TSource, TDest> : ICompilableTypeConverter<TSource, TDest> where TDest : new()
    {
        private readonly IEnumerable<ICompilablePropertyGetter> _propertyGetters;
		private readonly IEnumerable<PropertyInfo> _propertiesToSet;
		private readonly ByPropertySettingNullSourceBehaviourOptions _nullSourceBehaviour;
		private readonly IEnumerable<PropertyInfo> _initialisedFlagsIfTranslatingNullToEmptyInstance;
		private readonly Expression<Func<TSource, TDest>> _converterFuncExpression;
		private readonly Func<TSource, TDest> _converter;
		public CompilableTypeConverterByPropertySetting(
			IEnumerable<ICompilablePropertyGetter> propertyGetters,
			IEnumerable<PropertyInfo> propertiesToSet,
			ByPropertySettingNullSourceBehaviourOptions nullSourceBehaviour,
			IEnumerable<PropertyInfo> initialisedFlagsIfTranslatingNullToEmptyInstance)
        {
            if (propertyGetters == null)
                throw new ArgumentNullException("propertyGetters");
            if (propertiesToSet == null)
                throw new ArgumentNullException("propertiesToSet");
			if (!Enum.IsDefined(typeof(ByPropertySettingNullSourceBehaviourOptions), nullSourceBehaviour))
				throw new ArgumentOutOfRangeException("nullSourceBehaviour");
			if (initialisedFlagsIfTranslatingNullToEmptyInstance == null)
				throw new ArgumentNullException("initialisedFlagsIfTranslatingNullToEmptyInstance");
			if ((nullSourceBehaviour != ByPropertySettingNullSourceBehaviourOptions.CreateEmptyInstanceWithDefaultPropertyValues) && initialisedFlagsIfTranslatingNullToEmptyInstance.Any())
				throw new ArgumentException("initialisedFlagsIfTranslatingNullToEmptyInstance must be empty if nullSourceBehaviour is not CreateEmptyInstanceWithDefaultPropertyValues");

            // Ensure there are no null references in the property lists
            var propertyGettersList = new List<ICompilablePropertyGetter>();
            foreach (var propertyGetter in propertyGetters)
            {
                if (propertyGetter == null)
                    throw new ArgumentException("Null reference encountered in propertyGetters list");
                if (!propertyGetter.SrcType.Equals(typeof(TSource)))
                    throw new ArgumentException("Encountered invalid SrcType in propertyGetters list, must match type param TSource");
                propertyGettersList.Add(propertyGetter);
            }
            var propertiesToSetList = new List<PropertyInfo>();
            foreach (var property in propertiesToSet)
            {
                if (property == null)
                    throw new ArgumentException("Null reference encountered in propertyGetters list");
				if (!typeof(TDest).HasProperty(property))
					throw new ArgumentException("Encountered invalid entry in propertiesToSete set, not available on type TDest");
                propertiesToSetList.Add(property);
            }

            // Ensure that the property getters correspond to the target properties
            if (propertyGettersList.Count != propertiesToSetList.Count)
                throw new ArgumentException("Number of propertyGetters specified must match number of propertiesToSet");
            for (var index = 0; index < propertyGettersList.Count; index++)
            {
                if (!propertiesToSetList[index].PropertyType.IsAssignableFrom(propertyGettersList[index].TargetType))
                    throw new ArgumentException("propertyGetter[" + index + "].TargetType is not assignable to corresponding propertyToSet");
            }

			_propertyGetters = propertyGettersList.AsReadOnly();
            _propertiesToSet = propertiesToSetList.AsReadOnly();
			_nullSourceBehaviour = nullSourceBehaviour;
			_initialisedFlagsIfTranslatingNullToEmptyInstance = initialisedFlagsIfTranslatingNullToEmptyInstance.ToList().AsReadOnly();
			if (_initialisedFlagsIfTranslatingNullToEmptyInstance.Any(p => p == null))
				throw new ArgumentException("Null reference encountered in initialisedFlagsIfTranslatingNullToEmptyInstance set");
			if (_initialisedFlagsIfTranslatingNullToEmptyInstance.Any(p => (p.PropertyType != typeof(bool)) && (p.PropertyType != typeof(bool?))))
				throw new ArgumentException("Encountered invalid property in initialisedFlagsIfTranslatingNullToEmptyInstance set, PropertyType must be bool or nullable bool");
			if (_initialisedFlagsIfTranslatingNullToEmptyInstance.Any(p => !typeof(TDest).HasProperty(p)))
				throw new ArgumentException("Encountered invalid property in initialisedFlagsIfTranslatingNullToEmptyInstance set, must be a property available on TDest");
			PropertyMappings = propertyGettersList
				.Select((sourceProperty, index) => new PropertyMappingDetails(
					sourceProperty.Property,
					propertiesToSetList[index].Name,
					propertiesToSetList[index].PropertyType
				));

			// Generate a Expression<Func<TSource, TDest>>, the _rawConverterExpression is still required for the GetTypeConverterExpression
			// method (this may be called to retrieve the raw expression, rather than the Func-wrapped version - eg. by the ListCompilablePropertyGetter,
			// which has a set of TSource objects and wants to translate them into a set of TDest objects)
			var srcParameter = Expression.Parameter(typeof(TSource), "src");
			_converterFuncExpression = Expression.Lambda<Func<TSource, TDest>>(
				GetTypeConverterExpression(srcParameter),
				srcParameter
			);

			// Compile the expression into an actual Func<TSource, TDest> (this is expected to be the most commonly-used form of the data)
			_converter = _converterFuncExpression.Compile();
		}

		/// <summary>
		/// This will never be null nor contain any null references
		/// </summary>
		public IEnumerable<PropertyMappingDetails> PropertyMappings { get; private set; }

		/// <summary>
		/// If the source value is null should this property getter still be processed? If not, the assumption is that the target property / constructor argument on the
		/// destination type will be assigned default(TPropertyAsRetrieved). For this implementation, this depends upon the ByPropertySettingNullSourceBehaviourOptions
		/// configuration option - if a null source should result in an empty instance being created with the same properties being set as for a non-null source then
		/// this should return true (Entity Framework's LINQ projections rely upon this, it won't accept a branch where zero fields are requested for a null source
		/// and a non-zero quantity of fields being requested otherwise, it requires consistent field access).
		/// </summary>
		public bool PassNullSourceValuesForProcessing
		{
			get { return _nullSourceBehaviour == ByPropertySettingNullSourceBehaviourOptions.CreateEmptyInstanceWithDefaultPropertyValues; }
		}

		/// <summary>
        /// Create a new target type instance from a source value - this will throw an exception if conversion fails
        /// </summary>
        public TDest Convert(TSource src)
        {
            return _converter(src);
        }

		/// <summary>
		/// This must return a Linq Expression that returns a new TDest instance - the specified "param" Expression must have a type that is assignable to TSource.
		/// The resulting Expression may be used to create a Func to take a TSource instance and return a new TDest if the specified param is a ParameterExpression.
		/// If an expression of this form is required then the GetTypeConverterFuncExpression method may be more appropriate to use, this method is only when direct
		/// access to the conversion expression is required, it may be preferable to GetTypeConverterFuncExpression when generating complex expression that this is
		/// to be part of, potentially gaining a minor performance improvement (compared to calling GetTypeConverterFuncExpression) at the cost of compile-time
		/// type safety. Alternatively, this method may be required if an expression value is to be convered where the expression is not a ParameterExpression.
		/// </summary>
		public Expression GetTypeConverterExpression(Expression param)
        {
            if (param == null)
                throw new ArgumentNullException("param");
            if (!typeof(TSource).IsAssignableFrom(param.Type))
                throw new ArgumentException("param.Type must be assignable to typeparam TSource");

			// Generate the property bindings that accompany the parameter-less constructor call to initialise the target
			var propertyBindings = new List<MemberBinding>();
			foreach (var indexedProperty in _propertiesToSet.Select((property, index) => new { Property = property, Index = index }))
			{
				var propertyGetter = _propertyGetters.ElementAt(indexedProperty.Index);
				Expression bindValueExpression = propertyGetter.GetPropertyGetterExpression(param);
				if (_nullSourceBehaviour == ByPropertySettingNullSourceBehaviourOptions.CreateEmptyInstanceWithDefaultPropertyValues)
				{
					// If _compilableTypeConverter supports passing null into it, then don't generate the condition that prevents a null source from being
					// passed to it
					if (!propertyGetter.PassNullSourceValuesForProcessing)
					{
						bindValueExpression = Expression.Condition(
							Expression.Equal(
								param,
								Expression.Constant(null)
							),
							Expression.Constant(
								GetDefaultValue(indexedProperty.Property.PropertyType),
								indexedProperty.Property.PropertyType
							),
							bindValueExpression
						);
					}
				}

				propertyBindings.Add(
					Expression.Bind(
						indexedProperty.Property,
						bindValueExpression
					)
				);
			}
			propertyBindings.AddRange(
				_initialisedFlagsIfTranslatingNullToEmptyInstance.Select(initialisedFlagProperty =>
					Expression.Bind(
						initialisedFlagProperty,
						Expression.Condition(
							Expression.Equal(
								param,
								Expression.Constant(null)
							),
							Expression.Constant(false, initialisedFlagProperty.PropertyType),
							Expression.Constant(true, initialisedFlagProperty.PropertyType)
						)
					)
				)
			);
			
			var conversionExpression = Expression.MemberInit(
				Expression.New(typeof(TDest).GetConstructor(new Type[0])),
				propertyBindings
			);
			if (_nullSourceBehaviour == ByPropertySettingNullSourceBehaviourOptions.CreateEmptyInstanceWithDefaultPropertyValues)
				return conversionExpression;
			else if (_nullSourceBehaviour == ByPropertySettingNullSourceBehaviourOptions.UseDestDefaultIfSourceIsNull)
			{
				return Expression.Condition(
					Expression.Equal(
						param,
						Expression.Constant(null)
					),
					Expression.Constant(default(TDest), typeof(TDest)),
					conversionExpression
				);
			}
			else
				throw new ArgumentOutOfRangeException("typeConverterExpressionNullBehaviour");
		}

		 private object GetDefaultValue(Type type)
		 {
			 if (type == null)
				 throw new ArgumentNullException("t");

			if (type.IsValueType && (Nullable.GetUnderlyingType(type) == null))
				return Activator.CreateInstance(type);
			return null;
		}

		/// <summary>
		/// This will never return null, it will return an Func Expression for mapping from a TSource instance to a TDest
		/// </summary>
		public Expression<Func<TSource, TDest>> GetTypeConverterFuncExpression()
		{
			return _converterFuncExpression;
		}
	}
}
