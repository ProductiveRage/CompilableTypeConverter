using System;
using System.Collections.Generic;
using System.Linq;
using CompilableTypeConverter.ConstructorInvokers.Factories;
using CompilableTypeConverter.ConstructorPrioritisers.Factories;
using CompilableTypeConverter.PropertyGetters;
using CompilableTypeConverter.PropertyGetters.Factories;
using CompilableTypeConverter.PropertyGetters.Compilable;

namespace CompilableTypeConverter.TypeConverters.Factories
{
    public class SimpleTypeConverterByConstructorFactory : ITypeConverterFactory
    {
		private readonly ITypeConverterPrioritiserFactory _constructorPrioritiserFactory;
		private readonly IConstructorInvokerFactory _constructorInvokerFactory;
		private readonly IPropertyGetterFactory _propertyGetterFactory;
		private readonly ParameterLessConstructorBehaviourOptions _parameterLessConstructorBehaviour;
		public SimpleTypeConverterByConstructorFactory(
            ITypeConverterPrioritiserFactory constructorPrioritiserFactory,
			IConstructorInvokerFactory constructorInvokerFactory,
			IPropertyGetterFactory propertyGetterFactory,
			ParameterLessConstructorBehaviourOptions parameterLessConstructorBehaviour)
		{
            if (constructorPrioritiserFactory == null)
                throw new ArgumentNullException("constructorPrioritiserFactory");
			if (constructorInvokerFactory == null)
				throw new ArgumentNullException("constructorInvokerFactory");
            if (propertyGetterFactory == null)
                throw new ArgumentNullException("propertyGetterFactory");
			if (!Enum.IsDefined(typeof(ParameterLessConstructorBehaviourOptions), parameterLessConstructorBehaviour))
				throw new ArgumentOutOfRangeException("parameterLessConstructorBehaviour");

            _constructorPrioritiserFactory = constructorPrioritiserFactory;
			_constructorInvokerFactory = constructorInvokerFactory;
			_propertyGetterFactory = propertyGetterFactory;
			_parameterLessConstructorBehaviour = parameterLessConstructorBehaviour;
		}

		/// <summary>
		/// This will throw an exception if no suitable constructors were retrieved, it will never return null
		/// </summary>
		public ITypeConverter<TSource, TDest> Get<TSource, TDest>()
        {
			var failedCandidates = new List<ByConstructorMappingFailureException.ConstructorOptionFailureDetails>();
			var constructorCandidates = new List<ITypeConverterByConstructor<TSource, TDest>>();
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

				var defaultValuePropertyGetters = new List<IConstructorDefaultValuePropertyGetter>();
				var otherPropertyGetters = new List<IPropertyGetter>();
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
							(IConstructorDefaultValuePropertyGetter)Activator.CreateInstance(
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
                        new SimpleTypeConverterByConstructor<TSource, TDest>(
							otherPropertyGetters,
							defaultValuePropertyGetters,
	    				    _constructorInvokerFactory.Get<TDest>(constructor)
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
			if (selectedCandidate == null)
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
			return selectedCandidate;
		}
    }
}
