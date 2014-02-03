using System.Reflection;

namespace CompilableTypeConverter.TypeConverters
{
    public interface ITypeConverterByConstructor<TSource, TDest> : ITypeConverter<TSource, TDest>
    {
        /// <summary>
        /// The destination Constructor must be exposed by ITypeConverterByConstructor so that ITypeConverterPrioritiser implementations have something to work
        /// with - this value will never be null. Some of the constructor arguments may be satisfied by relying upon default argument values, to determine
		/// whether this is the case for any particular argument, consult the PropertyMappings set.
        /// </summary>
        ConstructorInfo Constructor { get; }
    }
}
