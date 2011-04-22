using System;
using System.Reflection;

namespace AutoMapperConstructor.ConstructorInvokers.Factories
{
    public class SimpleConstructorInvokerFactory : IConstructorInvokerFactory
    {
        public IConstructorInvoker<TDest> Get<TDest>(ConstructorInfo constructor)
        {
            if (constructor == null)
                throw new ArgumentNullException("constructor");
            return new SimpleConstructorInvoker<TDest>(constructor);
        }

        public IConstructorInvoker Get(ConstructorInfo constructor)
        {
            if (constructor == null)
                throw new ArgumentNullException("constructor");
            return (IConstructorInvoker)Activator.CreateInstance(
                typeof(SimpleConstructorInvoker<>).MakeGenericType(constructor.DeclaringType)
            );
        }
    }
}
