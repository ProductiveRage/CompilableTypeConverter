using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ProductiveRage.CompilableTypeConverter.PropertyGetters.Compilable
{
	public class CompilableConstructorDefaultValuePropertyGetter<TSourceObject, TPropertyAsRetrieved> : ICompilableConstructorDefaultValuePropertyGetter
	{
		private readonly ParameterInfo _argument;
		public CompilableConstructorDefaultValuePropertyGetter(ConstructorInfo constructor, string argumentName)
		{
			if (constructor == null)
				throw new ArgumentNullException("constructor");
			if (string.IsNullOrWhiteSpace(argumentName))
				throw new ArgumentException("Null/blank argumentName specified");

			_argument = constructor.GetParameters().FirstOrDefault(p => p.Name == argumentName);
			if (_argument == null)
				throw new ArgumentException("The specified argumentName does not correspond to any arguments of the provided constructor");
			if (!_argument.IsOptional)
				throw new ArgumentException("The specified constructor argument is not optional");
			if (!typeof(TPropertyAsRetrieved).IsAssignableFrom(_argument.ParameterType))
				throw new ArgumentException("The constructor argument's type is not assignable to TPropertyAsRetriever");

			Constructor = constructor;
		}

		public Type SrcType { get { return typeof(TSourceObject); } }

		/// <summary>
		/// Since this implementation isn't retrieving the value from a property (it's using a default constructor argument value) this
		/// will have to return null
		/// </summary>
		public PropertyInfo Property { get { return null; } }

		public Type TargetType { get { return typeof(TPropertyAsRetrieved); } }

		/// <summary>
		/// This will never be null
		/// </summary>
		public ConstructorInfo Constructor { get; private set; }

		/// <summary>
		/// This will nevere be null or blank, it will correspond to an argument of the Constructor and have a type that is assignable
		/// to TPropertyAsRetriever
		/// </summary>
		public string ArgumentName { get { return _argument.Name; } }

		/// <summary>
		/// If the source value is null should this property getter still be processed? If not, the assumption is that the target property / constructor argument on
		/// the destination type will be assigned default(TPropertyAsRetrieved). For this implementation, it doesn't make any difference since a fixed value will be
		/// returned, the source reference will never be accessed. Since that is the case, returning true here can remove the burden from the caller of having to
		/// branch based upon the source reference being null or not.
		/// </summary>
		public bool PassNullSourceValuesForProcessing { get { return true; } }

		public object GetValue(object src)
		{
			if (src == null)
				throw new ArgumentNullException("src");
			if (!src.GetType().Equals(typeof(TSourceObject)))
				throw new ArgumentException("The type of src must match typeparam TSourceObject");

			return _argument.DefaultValue;
		}

		public Expression GetPropertyGetterExpression(Expression param)
		{
			if (param == null)
				throw new ArgumentNullException("param");
			if (!typeof(TSourceObject).IsAssignableFrom(param.Type))
				throw new ArgumentException("param.Type must be assignable to typeparam TSourceObject");

			return Expression.Constant(
				_argument.DefaultValue,
				typeof(TPropertyAsRetrieved)
			);
		}
	}
}
