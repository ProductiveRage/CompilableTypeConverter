using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CompilableTypeConverter.ConstructorPrioritisers.Factories;
using CompilableTypeConverter.PropertyGetters.Compilable;
using CompilableTypeConverter.PropertyGetters.Factories;

namespace CompilableTypeConverter.TypeConverters.Factories
{
    public class CompilableTypeConverterByConstructorFactory : ICompilableTypeConverterFactory
    {
        private ITypeConverterPrioritiserFactory _constructorPrioritiserFactory;
        private ICompilablePropertyGetterFactory _propertyGetterFactory;
        public CompilableTypeConverterByConstructorFactory(
            ITypeConverterPrioritiserFactory constructorPrioritiserFactory,
			ICompilablePropertyGetterFactory propertyGetterFactory)
		{
            if (constructorPrioritiserFactory == null)
                throw new ArgumentNullException("constructorPrioritiserFactory");
            if (propertyGetterFactory == null)
                throw new ArgumentNullException("propertyGetterFactory");

            _constructorPrioritiserFactory = constructorPrioritiserFactory;
			_propertyGetterFactory = propertyGetterFactory;
		}

        /// <summary>
		/// This will return null if no suitable constructors were retrieved
		/// </summary>
        public ICompilableTypeConverter<TSource, TDest> Get<TSource, TDest>()
        {
            var constructorCandidates = new List<ICompilableTypeConverterByConstructor<TSource, TDest>>();
            var constructors = typeof(TDest).GetConstructors();
			foreach (var constructor in constructors)
			{
				var args = constructor.GetParameters();
                var propertyGetters = new List<ICompilablePropertyGetter>();
				var candidate = true;
				foreach (var arg in args)
				{
                    var propertyGetter = _propertyGetterFactory.Get(typeof(TSource), arg.Name, arg.ParameterType);
                    if (propertyGetter == null)
                    {
						if (arg.IsOptional)
						{
							propertyGetter = (ICompilablePropertyGetter)Activator.CreateInstance(
								typeof(DefaultValuePropertyGetter<,>).MakeGenericType(
									typeof(TSource),
									arg.ParameterType
								),
								arg.DefaultValue
							);
						}
						else
						{
							candidate = false;
							break;
						}
                    }
                    propertyGetters.Add(propertyGetter);
				}
				if (candidate)
			    {
                    constructorCandidates.Add(
                        new CompilableTypeConverterByConstructor<TSource, TDest>(
                            propertyGetters,
                            constructor
                        )
                    );
				}
			}
			if (constructorCandidates.Count == 0)
				return null;
            if (constructorCandidates.Count > 1)
            {
                // Use constructor prioritiser for standard ITypeConverterByConstructorFactory and then match back to original
                // candidate instance so that we can return an ICompilableTypeConverterByConstructor
                var constructorPrioritiser = _constructorPrioritiserFactory.Get<TSource, TDest>();
                var selectedCandidate = constructorPrioritiser.Get(constructorCandidates);
                var selectedCandidateCompiled = constructorCandidates.FirstOrDefault(c => c == selectedCandidate);
                if (selectedCandidateCompiled == null)
                    throw new Exception("constructorPrioritiser failure - didn't return valid candidate");
                return selectedCandidateCompiled;
            }
			return constructorCandidates[0];
		}

        ITypeConverter<TSource, TDest> ITypeConverterFactory.Get<TSource, TDest>()
        {
            return Get<TSource, TDest>();
        }

		private class DefaultValuePropertyGetter<TSourceObject, TPropertyAsRetrieved> : ICompilablePropertyGetter
		{
			private readonly TPropertyAsRetrieved _value;
			public DefaultValuePropertyGetter(TPropertyAsRetrieved value)
			{
				_value = value;
			}

			public Type SrcType { get { return typeof(TSourceObject); } }

			/// <summary>
			/// Since this implementation isn't retrieving the value from a property (it's using a default constructor argument value) this
			/// will have to return null
			/// </summary>
			public PropertyInfo Property { get { return null; } }

			public Type TargetType { get { return typeof(TPropertyAsRetrieved); } }

			public object GetValue(object src)
			{
				if (src == null)
					throw new ArgumentNullException("src");
				if (!src.GetType().Equals(typeof(TSourceObject)))
					throw new ArgumentException("The type of src must match typeparam TSourceObject");

				return _value;
			}

			public Expression GetPropertyGetterExpression(Expression param)
			{
				if (param == null)
					throw new ArgumentNullException("param");
				if (!typeof(TSourceObject).IsAssignableFrom(param.Type))
					throw new ArgumentException("param.Type must be assignable to typeparam TSourceObject");

				return Expression.Constant(_value, typeof(TPropertyAsRetrieved));
			}
		}
	}
}
