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

        public Type TargetType
        {
            get { return typeof(TDest); }
        }

        public TDest Invoke(object[] args)
        {
            return (TDest)_constructor.Invoke(args);
        }

        object IConstructorInvoker.Invoke(object[] args)
        {
            return Invoke(args);
        }
    }
}
