using System;

namespace AutoMapperConstructor.ConstructorInvokers
{
    public interface IConstructorInvoker<TDest> : IConstructorInvoker
    {
        /// <summary>
        /// This returns a new instance of the target type - intended to be implemented by a specified constructor being called for the target (with
        /// provided arguments being passed)
        /// </summary>
        new TDest Invoke(object[] args);
    }

    public interface IConstructorInvoker
    {
        Type TargetType { get; }
        object Invoke(object[] args);
    }
}
