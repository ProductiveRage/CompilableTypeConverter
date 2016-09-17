namespace ProductiveRage.CompilableTypeConverter.TypeConverters.Factories
{
	public interface ICompilableTypeConverterFactory : ITypeConverterFactory
    {
        /// <summary>
		/// This will throw an exception if a converter could not be generated, it will never return null
		/// </summary>
        new ICompilableTypeConverter<TSource, TDest> Get<TSource, TDest>();
    }
}
