using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProductiveRage.CompilableTypeConverter.ConstructorInvokers;
using ProductiveRage.CompilableTypeConverter.PropertyGetters;
using ProductiveRage.CompilableTypeConverter.PropertyGetters.Compilable;

namespace ProductiveRage.CompilableTypeConverter.TypeConverters
{
	/// <summary>
	/// A class capable of converting an instance of one type into another by calling a constructor on the target type - the manner in which the
	/// data is retrieved (and converted, if required) from the source type is determined by the provided IPropertyGetter instances, the way in
	/// which the target constructor is executed is determined by the specified IConstructorInvoker; eg. the constructor may have its Invoke
	/// method called or IL code may be generated to call it)
	/// </summary>
	public class SimpleTypeConverterByConstructor<TSource, TDest> : ITypeConverterByConstructor<TSource, TDest>
    {
        private readonly IConstructorInvoker<TDest> _constructorInvoker;
		private readonly List<IPropertyGetter> _propertyGetters;
        public SimpleTypeConverterByConstructor(
			IEnumerable<IPropertyGetter> propertyGetters,
			IEnumerable<IConstructorDefaultValuePropertyGetter> defaultValuePropertyGetters,
			IConstructorInvoker<TDest> constructorInvoker)
        {
            if (propertyGetters == null)
                throw new ArgumentNullException("propertyGetters");
			if (defaultValuePropertyGetters == null)
				throw new ArgumentNullException("defaultValuePropertyGetters");
            if (constructorInvoker == null)
                throw new ArgumentNullException("constructorInvoker");

			// Ensure there are no null references in the property getter content
			var propertyGettersList = new List<IPropertyGetter>();
			foreach (var propertyGetter in propertyGetters)
			{
				if (propertyGetter == null)
					throw new ArgumentException("Null reference encountered in propertyGetters list");
				if (!propertyGetter.SrcType.Equals(typeof(TSource)))
					throw new ArgumentException("Encountered invalid SrcType in propertyGetters list, must match type param TSource");
				propertyGettersList.Add(propertyGetter);
			}
			var defaultValuePropertyGettersList = new List<IPropertyGetter>();
			foreach (var defaultValuePropertyGetter in defaultValuePropertyGetters)
			{
				if (defaultValuePropertyGetter == null)
					throw new ArgumentException("Null reference encountered in defaultValuePropertyGetters list");
				if (defaultValuePropertyGetter.Constructor != constructorInvoker.Constructor)
					throw new ArgumentException("Invalid reference encountered in defaultValuePropertyGetters set, does not match specified constructor");
				defaultValuePropertyGettersList.Add(defaultValuePropertyGetter);
			}

			// Combine the propertyGetters and defaultValuePropertyGetters into a single list that correspond to the constructor arguments
			// (ensuring that the property getters correspond to the constructor that's being targetted and that the numbers of property
			// getters is correct)
			var constructorParameters = constructorInvoker.Constructor.GetParameters();
			if ((propertyGettersList.Count + defaultValuePropertyGettersList.Count) != constructorParameters.Length)
				throw new ArgumentException("Number of propertyGetters.Count must match constructor.GetParameters().Length");
			var combinedPropertyGetters = new List<IPropertyGetter>();
			var propertyMappings = new List<PropertyMappingDetails>();
			for (var index = 0; index < constructorParameters.Length; index++)
			{
				var constructorParameter = constructorParameters[index];
				var defaultValuePropertyGetter = defaultValuePropertyGetters.FirstOrDefault(p => p.ArgumentName == constructorParameter.Name);
				if (defaultValuePropertyGetter != null)
				{
					// There's no validation to perform here, the IConstructorDefaultValuePropertyGetter interface states that the TargetType
					// will match the named constructor argument that it relates to
					combinedPropertyGetters.Add(defaultValuePropertyGetter);
					continue;
				}

				// If there was no default value property getter, then the first entry in the propertyGetters set should correspond to the
				// current constructor argument (since we keep removing the first item in that set when a match is found, this remains true
				// as we process multiple arguments)
				if (propertyGettersList.Count == 0)
					throw new ArgumentException("Unable to match a property getter to constructor argument \"" + constructorParameter.Name + "\"");
				var propertyGetter = propertyGettersList[0];
				if (!constructorParameter.ParameterType.IsAssignableFrom(propertyGetter.TargetType))
					throw new ArgumentException("propertyGetter[" + index + "].TargetType is not assignable to corresponding constructor parameter type");
				combinedPropertyGetters.Add(propertyGetter);
				propertyGettersList.RemoveAt(0);

				propertyMappings.Add(
					new PropertyMappingDetails(
						propertyGetter.Property,
						constructorParameter.Name,
						constructorParameter.ParameterType
					)
				);
			}

            _constructorInvoker = constructorInvoker;
			_propertyGetters = combinedPropertyGetters;

			PropertyMappings = propertyMappings.AsReadOnly();
		}

        /// <summary>
		/// The destination Constructor must be exposed by ITypeConverterByConstructor so that ITypeConverterPrioritiser implementations have something to work
		/// with - this value will never be null. Some of the constructor arguments may be satisfied by relying upon default argument values, to determine
		/// whether this is the case for any particular argument, consult the PropertyMappings set.
		/// </summary>
        public ConstructorInfo Constructor
        {
            get { return _constructorInvoker.Constructor; }
        }

		/// <summary>
		/// This will never be null nor contain any null references
		/// </summary>
		public IEnumerable<PropertyMappingDetails> PropertyMappings { get; private set; }

        /// <summary>
        /// Try to retrieve the value of the specified Property from the specified object (which must be of type SrcType) - this will throw an exception for null input
        /// or if retrieval fails
        /// </summary>
        public TDest Convert(TSource src)
        {
            var args = new object[_propertyGetters.Count];
            for (var index = 0; index < _propertyGetters.Count; index++)
                args[index] = _propertyGetters[index].GetValue(src);
            return (TDest)_constructorInvoker.Invoke(args);
        }
    }
}
