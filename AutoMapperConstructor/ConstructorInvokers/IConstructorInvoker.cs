using System;
using System.Reflection;

namespace AutoMapperConstructor.ConstructorInvokers
{
    public interface IConstructorInvoker<TDest>
    {
        /// <summary>
        /// This is the constructor that will be called to create the new instance - having access to this may be used to validate options passed
        /// to ITypeConverterByConstructor (eg. see the property getter validation in SimpleTypeConverterByConstructor's constructor)
        /// </summary>
        ConstructorInfo Constructor { get; }

        /// <summary>
        /// This returns a new instance of TDest - intended to be implemented by a specified constructor being called for the target (with
        /// arguments being passed) - it will throw an exception if unable to invoke the constructor, it should never return null
        /// </summary>
        TDest Invoke(object[] args);
    }
}
