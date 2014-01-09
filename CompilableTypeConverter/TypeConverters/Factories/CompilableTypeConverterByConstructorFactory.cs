using System;
using System.Collections.Generic;
using System.Linq;
using CompilableTypeConverter.ConstructorPrioritisers.Factories;
using CompilableTypeConverter.PropertyGetters.Compilable;
using CompilableTypeConverter.PropertyGetters.Factories;

namespace CompilableTypeConverter.TypeConverters.Factories
{
    public class CompilableTypeConverterByConstructorFactory : ICompilableTypeConverterFactory
    {
        private readonly ITypeConverterPrioritiserFactory _constructorPrioritiserFactory;
        private readonly ICompilablePropertyGetterFactory _propertyGetterFactory;
		private readonly ParameterLessConstructorBehaviourOptions _parameterLessConstructorBehaviour;
        public CompilableTypeConverterByConstructorFactory(
            ITypeConverterPrioritiserFactory constructorPrioritiserFactory,
			ICompilablePropertyGetterFactory propertyGetterFactory,
			ParameterLessConstructorBehaviourOptions parameterLessConstructorBehaviour)
		{
            if (constructorPrioritiserFactory == null)
                throw new ArgumentNullException("constructorPrioritiserFactory");
            if (propertyGetterFactory == null)
                throw new ArgumentNullException("propertyGetterFactory");
			if (!Enum.IsDefined(typeof(ParameterLessConstructorBehaviourOptions), parameterLessConstructorBehaviour))
				throw new ArgumentOutOfRangeException("parameterLessConstructorBehaviour");

            _constructorPrioritiserFactory = constructorPrioritiserFactory;
			_propertyGetterFactory = propertyGetterFactory;
			_parameterLessConstructorBehaviour = parameterLessConstructorBehaviour;
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
				if ((args.Length == 0) && (_parameterLessConstructorBehaviour == ParameterLessConstructorBehaviourOptions.Ignore))
					continue;

				var defaultValuePropertyGetters = new List<ICompilableConstructorDefaultValuePropertyGetter>();
				var otherPropertyGetters = new List<ICompilablePropertyGetter>();
				var candidate = true;
				foreach (var arg in args)
				{
                    var propertyGetter = _propertyGetterFactory.Get(typeof(TSource), arg.Name, arg.ParameterType);
					if (propertyGetter != null)
					{
						otherPropertyGetters.Add(propertyGetter);
						continue;
					}

					if (arg.IsOptional)
					{
						defaultValuePropertyGetters.Add(
							(ICompilableConstructorDefaultValuePropertyGetter)Activator.CreateInstance(
								typeof(CompilableConstructorDefaultValuePropertyGetter<,>).MakeGenericType(
									typeof(TSource),
									arg.ParameterType
								),
								constructor,
								arg.Name
							)
						);
						continue;
					}
					
					candidate = false;
					break;
				}
				if (candidate)
			    {
                    constructorCandidates.Add(
                        new CompilableTypeConverterByConstructor<TSource, TDest>(
							otherPropertyGetters,
							defaultValuePropertyGetters,
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
	}
}
