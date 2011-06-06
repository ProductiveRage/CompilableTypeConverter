using System.Linq.Expressions;

namespace AutoMapperConstructor.TypeConverters
{
    public interface ICompilableTypeConverter<TSource, TDest> : ITypeConverter<TSource, TDest>
    {
        /// <summary>
        /// This must return a Linq Expression that returns a new TDest instance - the specified "param" Expression must have a type that is assignable to TSource.
        /// The resulting Expression will be assigned to a Lambda Expression typed as a TSource to TDest Func.
        /// </summary>
        Expression GetTypeConverterExpression(Expression param);
    }
}
