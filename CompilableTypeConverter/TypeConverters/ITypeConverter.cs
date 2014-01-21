using System.Collections.Generic;
using System.Reflection;

namespace CompilableTypeConverter.TypeConverters
{
    public interface ITypeConverter<TSource, TDest>
    {
        /// <summary>
        /// Create a new target type instance from a source value - this will throw an exception if conversion fails
        /// </summary>
        TDest Convert(TSource src);

		/// <summary>
		/// This will never be null nor contain any null references
		/// </summary>
		IEnumerable<PropertyInfo> SourcePropertiesAccessed { get; }
    }
}
