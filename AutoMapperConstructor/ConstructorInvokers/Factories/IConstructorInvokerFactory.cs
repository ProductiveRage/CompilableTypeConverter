using System;
using System.Reflection;

namespace AutoMapperConstructor.ConstructorInvokers.Factories
{
    /// <summary>
    /// Return an IConstructorInvoker instance for the target type using the specified constructor (which should be a constructor for target type)
    /// </summary>
    public interface IConstructorInvokerFactory
    {
        IConstructorInvoker Get(ConstructorInfo constructor);
        IConstructorInvoker<T> Get<T>(ConstructorInfo constructor);
    }
}
