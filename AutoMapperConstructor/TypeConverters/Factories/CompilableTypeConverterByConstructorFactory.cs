using System;
using System.Collections.Generic;
using System.Linq;
using CompilableTypeConverter.ConstructorInvokers.Factories;
using CompilableTypeConverter.ConstructorPrioritisers.Factories;
using CompilableTypeConverter.PropertyGetters;
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
                        candidate = false;
                        break;
                    }
                    else
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
    }
}
