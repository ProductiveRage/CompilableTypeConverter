using System;
using System.Reflection;

namespace ProductiveRage.CompilableTypeConverter.ConstructorInvokers.Factories
{
	public class SimpleConstructorInvokerFactory : IConstructorInvokerFactory
    {
        /// <summary>
        /// This will throw an exception if unable to return an appropriate IConstructorInvoker, it should never return null
        /// </summary>
        public IConstructorInvoker<TDest> Get<TDest>(ConstructorInfo constructor)
        {
            if (constructor == null)
                throw new ArgumentNullException("constructor");
            return new SimpleConstructorInvoker<TDest>(constructor);
        }
    }
}
