using System.Reflection;

namespace ProductiveRage.CompilableTypeConverter.ConstructorInvokers.Factories
{
	/// <summary>
	/// Return an IConstructorInvoker instance for the target type using the specified constructor (which should be a constructor for target type)
	/// </summary>
	public interface IConstructorInvokerFactory
    {
        /// <summary>
        /// This will throw an exception if unable to return an appropriate IConstructorInvoker, it should never return null
        /// </summary>
        IConstructorInvoker<T> Get<T>(ConstructorInfo constructor);
    }
}
