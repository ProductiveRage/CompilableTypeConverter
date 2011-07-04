using System.Linq.Expressions;

namespace AutoMapperConstructor.TypeConverters
{
    /// <summary>
    /// This is a compilable type converter that will translate from TSource to TDest, given an instance of TSource, using a specified constructor of TDest
    /// </summary>
    public interface ICompilableTypeConverterByConstructor<TSource, TDest> : ICompilableTypeConverter<TSource, TDest>, ITypeConverterByConstructor<TSource, TDest> { }
}
