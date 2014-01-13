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
		/// This will throw an exception if no suitable constructors were retrieved, it will never return null
		/// </summary>
        public ICompilableTypeConverter<TSource, TDest> Get<TSource, TDest>()
        {
			var failedCandidates = new List<ByConstructorMappingFailureException.ConstructorOptionFailureDetails>();
            var constructorCandidates = new List<ICompilableTypeConverterByConstructor<TSource, TDest>>();
            var constructors = typeof(TDest).GetConstructors();
			foreach (var constructor in constructors)
			{
				var args = constructor.GetParameters();
				if ((args.Length == 0) && (_parameterLessConstructorBehaviour == ParameterLessConstructorBehaviourOptions.Ignore))
				{
					failedCandidates.Add(new ByConstructorMappingFailureException.ConstructorOptionFailureDetails(
						constructor,
						ByConstructorMappingFailureException.ConstructorOptionFailureDetails.FailureReasonOptions.ParameterLessConstructorNotAllowed,
						null
					));
					continue;
				}

				var defaultValuePropertyGetters = new List<ICompilableConstructorDefaultValuePropertyGetter>();
				var otherPropertyGetters = new List<ICompilablePropertyGetter>();
				var candidate = true;
				foreach (var arg in args)
				{
                    var propertyGetter = _propertyGetterFactory.TryToGet(typeof(TSource), arg.Name, arg.ParameterType);
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

					failedCandidates.Add(new ByConstructorMappingFailureException.ConstructorOptionFailureDetails(
						constructor,
						ByConstructorMappingFailureException.ConstructorOptionFailureDetails.FailureReasonOptions.UnableToMapConstructorArgument,
						arg
					));
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
			{
				throw new ByConstructorMappingFailureException(
					typeof(TSource),
					typeof(TDest),
					failedCandidates
				);
			}

			// The ITypeConverterPrioritiser interface may be used to prioritise and filter. If there are multiple candidates then it may only
			// prioritise the results (depending upon implementation), but it may also filter some out (again, depending upon implementation).
			// This means that the prioritiser must be consulted even if there is only a single candidate (there is no prioritisation required
			// in that case since there is only one option, but the possibility that this candidate will not be allowed through the filter
			// means that the prioritiser must still be called upon).
			var constructorPrioritiser = _constructorPrioritiserFactory.Get<TSource, TDest>();
            var selectedCandidate = constructorPrioritiser.Get(constructorCandidates);
            var selectedCandidateCompiled = constructorCandidates.FirstOrDefault(c => c == selectedCandidate);
			if (selectedCandidateCompiled == null)
			{
				throw new ByConstructorMappingFailureException(
					typeof(TSource),
					typeof(TDest),
					constructorCandidates.Select(c => new ByConstructorMappingFailureException.ConstructorOptionFailureDetails(
						c.Constructor,
						ByConstructorMappingFailureException.ConstructorOptionFailureDetails.FailureReasonOptions.FilteredOutByPrioritiser,
						null
					))
				);
			}
            return selectedCandidateCompiled;
		}

        /// <summary>
		/// This will throw an exception if no suitable constructors were retrieved, it will never return null
		/// </summary>
		ITypeConverter<TSource, TDest> ITypeConverterFactory.Get<TSource, TDest>()
        {
            return Get<TSource, TDest>();
        }
	}
}
