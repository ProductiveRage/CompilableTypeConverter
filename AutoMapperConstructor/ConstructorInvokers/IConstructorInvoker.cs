using System;

namespace AutoMapperConstructor.ConstructorInvokers
{
    public interface IConstructorInvoker<TDest>
    {
        /// <summary>
        /// This returns a new instance of TDest - intended to be implemented by a specified constructor being called for the target (with
        /// arguments being passed) - it will throw an exception if unable to invoke the constructor, it should never return null
        /// </summary>
        TDest Invoke(object[] args);
    }
}
