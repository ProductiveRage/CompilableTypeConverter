﻿using System;
using System.Collections.Generic;
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
		/// This will return null if no suitable constructors were retrieved
		/// </summary>
        public ITypeConverter<TSource, TDest> Get<TSource, TDest>()
        {
            var constructorCandidates = new List<ITypeConverterByConstructor<TSource, TDest>>();
            var constructors = typeof(TDest).GetConstructors();
			foreach (var constructor in constructors)
			{
				var args = constructor.GetParameters();
				if ((args.Length == 0) && (_parameterLessConstructorBehaviour == ParameterLessConstructorBehaviourOptions.Ignore))
					continue;

				var defaultValuePropertyGetters = new List<IConstructorDefaultValuePropertyGetter>();
				var otherPropertyGetters = new List<IPropertyGetter>();
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
				return null;
            if (constructorCandidates.Count > 1)
            {
                var constructorPrioritiser = _constructorPrioritiserFactory.Get<TSource, TDest>();
                return constructorPrioritiser.Get(constructorCandidates);
            }
			return constructorCandidates[0];
		}
    }
}
