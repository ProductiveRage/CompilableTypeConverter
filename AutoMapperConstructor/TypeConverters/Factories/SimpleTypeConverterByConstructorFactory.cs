using System;
using System.Collections.Generic;
using AutoMapperConstructor.ConstructorInvokers.Factories;
using AutoMapperConstructor.ConstructorPrioritisers.Factories;
using AutoMapperConstructor.PropertyGetters;
using AutoMapperConstructor.PropertyGetters.Factories;

namespace AutoMapperConstructor.TypeConverters.Factories
{
    public class SimpleTypeConverterByConstructorFactory : ITypeConverterByConstructorFactory
    {
        private ITypeConverterPrioritiserFactory _constructorPrioritiserFactory;
		private IConstructorInvokerFactory _constructorInvokerFactory;
		private IPropertyGetterFactory _propertyGetterFactory;
        public SimpleTypeConverterByConstructorFactory(
            ITypeConverterPrioritiserFactory constructorPrioritiserFactory,
			IConstructorInvokerFactory constructorInvokerFactory,
			IPropertyGetterFactory propertyGetterFactory)
		{
            if (constructorPrioritiserFactory == null)
                throw new ArgumentNullException("constructorPrioritiserFactory");
			if (constructorInvokerFactory == null)
				throw new ArgumentNullException("constructorInvokerFactory");
            if (propertyGetterFactory == null)
                throw new ArgumentNullException("propertyGetterFactory");

            _constructorPrioritiserFactory = constructorPrioritiserFactory;
			_constructorInvokerFactory = constructorInvokerFactory;
			_propertyGetterFactory = propertyGetterFactory;
		}

        /// <summary>
		/// This will return null if no suitable constructors were retrieved
		/// </summary>
        public ITypeConverterByConstructor<TSource, TDest> Get<TSource, TDest>()
        {
            var constructorCandidates = new List<ITypeConverterByConstructor<TSource, TDest>>();
            var constructors = typeof(TDest).GetConstructors();
			foreach (var constructor in constructors)
			{
				var args = constructor.GetParameters();
				var propertyGetters = new List<IPropertyGetter>();
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
                    var constructorCandidate = (ITypeConverterByConstructor<TSource, TDest>)Activator.CreateInstance(
                        typeof(SimpleTypeConverterByConstructor<,>).MakeGenericType(
                            typeof(TSource),
                            typeof(TDest)
                        ),
					    constructor,
                        propertyGetters,
					    _constructorInvokerFactory
                    );
                    constructorCandidates.Add(constructorCandidate);
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
