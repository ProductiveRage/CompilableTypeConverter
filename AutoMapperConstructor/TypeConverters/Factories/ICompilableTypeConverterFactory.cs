namespace AutoMapperConstructor.TypeConverters.Factories
{
    public interface ICompilableTypeConverterFactory : ITypeConverterFactory
    {
        /// <summary>
        /// This will return null if a converter could not be generated
        /// </summary>
        new ICompilableTypeConverter<TSource, TDest> Get<TSource, TDest>();
    }
}
