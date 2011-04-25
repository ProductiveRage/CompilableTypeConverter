using System;
using System.Reflection;

namespace AutoMapperConstructor.ConstructorInvokers
{
    /// <summary>
    /// Returns a new instance of the target type by calling Invoke on the type constructor specified (with the provided arguments)
    /// </summary>
    public class SimpleConstructorInvoker<TDest> : IConstructorInvoker<TDest>
    {
        private ConstructorInfo _constructor;
        public SimpleConstructorInvoker(ConstructorInfo constructor)
        {
            if (constructor == null)
                throw new ArgumentNullException("constructor");
            _constructor = constructor;
        }

        /// <summary>
        /// This returns a new instance of TDest - intended to be implemented by a specified constructor being called for the target (with
        /// arguments being passed) - it will throw an exception if unable to invoke the constructor, it should never return null
        /// </summary>
        public TDest Invoke(object[] args)
        {
            return (TDest)_constructor.Invoke(args);
        }
    }
}
