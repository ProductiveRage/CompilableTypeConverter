namespace CompilableTypeConverter.PropertyGetters.Compilable
{
	public enum EnumerableSetNullHandlingOptions
	{
		/// <summary>
		/// This bypasses any null reference checks on enumerable sets that the EnumerableCompilablePropertyGetter may handle, meaning that
		/// null reference exceptions may be raised during conversion if null sets are encountered. The benefit is that the branching that
		/// deals with null inputs is not generated, which can improve compatibility in scenarios where consistent behaviour is required,
		/// such as when translated IQueryable results from Entity Framework. This should only be used when the sets that may be translated
		/// are expected to be non-nullable.
		/// </summary>
		AssumeNonNullInput,

		/// <summary>
		/// This will perform additional work to ensure that a null reference is returned when converting an enumerable set that is a null
		/// reference (this should be considered the default behaviour since it should leave any null reference exceptions to occur in
		/// class validation, if there is any, rather than in the translation process).
		/// </summary>
		ReturnNullSetForNullInput
	}
}
