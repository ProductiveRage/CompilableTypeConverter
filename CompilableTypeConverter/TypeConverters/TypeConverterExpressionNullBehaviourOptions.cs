namespace CompilableTypeConverter.TypeConverters
{
	public enum TypeConverterExpressionNullBehaviourOptions
	{
		/// <summary>
		/// With this option, if a null source reference is passed then a default(TDest) will be returned
		/// </summary>
		UseDestDefaultIfSourceIsNull,

		/// <summary>
		/// With this option, no null checking will be performed - this may result in an exception for a null input. This should only be used
		/// if the input is not expected to be null and where the generated expression must appear consistent (eg. when used in some LINQ
		/// scenarios)
		/// </summary>
		SkipNullHandling
	}
}
