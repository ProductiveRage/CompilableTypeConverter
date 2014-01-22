using System.Linq.Expressions;

namespace CompilableTypeConverter.PropertyGetters.Compilable
{
    /// <summary>
    /// Note: ICompilablePropertyGetter can not specify typeparams as we're likely to need to maintain a list of these (eg. see CompilableTypeConverterByConstructor)
    /// before we know the types of the properties - we don't expect to know the TargetType values until runtime, even if we may know the SrcType at compile time.
    /// </summary>
    public interface ICompilablePropertyGetter : IPropertyGetter
    {
        /// <summary>
        /// This must return a Linq Expression that retrieves the value from SrcType.Property as TargetType - the specified "param" Expression must have a type that
        /// is assignable to SrcType. The resulting Expression will be assignable to a Lambda Expression typed as a TSourceObject to TPropertyAsRetrieved Func. We
        /// can't check at compile time for implementations that do not follow this rule as this interface has those types as properties, not typeparams. Having
        /// access to these Expressions allows us to create type converters that are almost as fast as hand-written versions, which is the trade-off.
        /// </summary>
        Expression GetPropertyGetterExpression(Expression param);

		/// <summary>
		/// If the source value is null should this property getter still be processed? If not, the assumption is that the target property / constructor argument on
		/// the destination type will be assigned default(TPropertyAsRetrieved).
		/// </summary>
		bool PassNullSourceValuesForProcessing { get; }
	}
}
