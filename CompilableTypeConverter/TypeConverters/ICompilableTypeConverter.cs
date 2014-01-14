using System;
using System.Linq.Expressions;

namespace CompilableTypeConverter.TypeConverters
{
    /// <summary>
    /// This is a generic compilable type converter that will translate from TSource to TDest, given an instance of TSource
    /// </summary>
    public interface ICompilableTypeConverter<TSource, TDest> : ITypeConverter<TSource, TDest>
    {
		/// <summary>
		/// This must return a Linq Expression that returns a new TDest instance - the specified "param" Expression must have a type that is assignable to TSource.
		/// The resulting Expression may be used to create a Func to take a TSource instance and return a new TDest if the specified param is a ParameterExpression.
		/// If an expression of this form is required then the GetTypeConverterFuncExpression method may be more appropriate to use, this method is only when direct
		/// access to the conversion expression is required, it may be preferable to GetTypeConverterFuncExpression when generating complex expression that this is
		/// to be part of, potentially gaining a minor performance improvement (compared to calling GetTypeConverterFuncExpression) at the cost of compile-time
		/// type safety. Alternatively, this method may be required if an expression value is to be convered where the expression is not a ParameterExpression.
		/// </summary>
		Expression GetTypeConverterExpression(Expression param);

		/// <summary>
		/// This will never return null, it will return an Func Expression for mapping from a TSource instance to a TDest
		/// </summary>
		Expression<Func<TSource, TDest>> GetTypeConverterFuncExpression();
    }
}
