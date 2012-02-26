namespace CompilableTypeConverter.NameMatchers
{
    public interface INameMatcher
    {
        /// <summary>
        /// Determine whether two property names can be considered equal for mappings (eg. allow may mapping "Name" to "name" or "_name")
        /// </summary>
        bool IsMatch(string from, string to);
    }
}
