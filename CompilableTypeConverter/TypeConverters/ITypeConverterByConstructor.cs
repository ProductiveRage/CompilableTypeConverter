using System.Reflection;

namespace CompilableTypeConverter.TypeConverters
{
    public interface ITypeConverterByConstructor<TSource, TDest> : ITypeConverter<TSource, TDest>
    {
        /// <summary>
        /// The destination Constructor must be exposed by ITypeConverterByConstructor so that ITypeConverterPrioritiser implementations have something to work
        /// with - this value will never be null
        /// </summary>
        ConstructorInfo Constructor { get; }

		/// <summary>
		/// This will always be zero or greater and less than or equal to the number of parameters that the Constructor reference has
		/// </summary>
		int NumberOfConstructorArgumentsMatchedWithNonDefaultValues { get; }
    }
}
