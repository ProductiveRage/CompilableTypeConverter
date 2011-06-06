using System.Linq.Expressions;

namespace AutoMapperConstructor.TypeConverters
{
    public interface ICompilableTypeConverterByConstructor<TSource, TDest> : ICompilableTypeConverter<TSource, TDest>, ITypeConverterByConstructor<TSource, TDest> { }
}
