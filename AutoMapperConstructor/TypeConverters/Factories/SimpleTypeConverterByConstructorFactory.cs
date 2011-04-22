using System;
using System.Collections.Generic;
using AutoMapperConstructor.ConstructorInvokers.Factories;
using AutoMapperConstructor.ConstructorPrioritisers;
using AutoMapperConstructor.PropertyGetters;
using AutoMapperConstructor.PropertyGetters.Factories;

namespace AutoMapperConstructor.TypeConverters.Factories
{
    public class SimpleTypeConverterByConstructorFactory : ITypeConverterByConstructorFactory
    {
		private ITypeConverterPrioritiser _constructorPrioritiser;
		private IConstructorInvokerFactory _constructorInvokerFactory;
		private IPropertyGetterFactory _propertyGetterFactory;
        public SimpleTypeConverterByConstructorFactory(
			ITypeConverterPrioritiser constructorPrioritiser,
			IConstructorInvokerFactory constructorInvokerFactory,
			IPropertyGetterFactory propertyGetterFactory)
		{
			if (constructorPrioritiser == null)
				throw new ArgumentNullException("constructorPrioritiser");
			if (constructorInvokerFactory == null)
				throw new ArgumentNullException("constructorInvokerFactory");
            if (propertyGetterFactory == null)
                throw new ArgumentNullException("propertyGetterFactory");

			_constructorPrioritiser = constructorPrioritiser;
			_constructorInvokerFactory = constructorInvokerFactory;
			_propertyGetterFactory = propertyGetterFactory;
		}

        /// <summary>
		/// This will return null if no suitable constructors were retrieved
		/// </summary>
		public ITypeConverterByConstructor Get(Type srcType, Type destType)
{
            if (srcType == null)
                throw new ArgumentNullException("srcType");
            if (destType == null)
                throw new ArgumentNullException("destType");

            var constructorCandidates = new List<ITypeConverterByConstructor>();
			var constructors = destType.GetConstructors();
			foreach (var constructor in constructors)
			{
				var args = constructor.GetParameters();
				var propertyGetters = new List<IPropertyGetter>();
				var candidate = true;
				foreach (var arg in args)
				{
                    var propertyGetter = _propertyGetterFactory.Get(srcType, arg.Name, arg.ParameterType);
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
                    var constructorCandidate = (ITypeConverterByConstructor)Activator.CreateInstance(
                        typeof(SimpleTypeConverterByConstructor<,>).MakeGenericType(
                            srcType,
                            destType
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
				return _constructorPrioritiser.Get(constructorCandidates);
			return constructorCandidates[0];
		}

        /// <summary>
        /// This will return null if no suitable constructors were retrieved
        /// </summary>
        public ITypeConverterByConstructor<TSource, TDest> Get<TSource, TDest>()
        {
            var converter = Get(typeof(TSource), typeof(TDest));
            if (converter == null)
                return null;
            return converter.AsGeneric<TSource, TDest>();
        }
    }
}
