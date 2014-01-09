using System.Reflection;

namespace CompilableTypeConverter.PropertyGetters.Compilable
{
	public interface IConstructorDefaultValuePropertyGetter : IPropertyGetter
	{
		/// <summary>
		/// This will never be null
		/// </summary>
		ConstructorInfo Constructor { get; }

		/// <summary>
		/// This will nevere be null or blank, it will correspond to an argument of the Constructor and have a type that is assignable
		/// to the TargetType
		/// </summary>
		string ArgumentName { get; }
	}
}
