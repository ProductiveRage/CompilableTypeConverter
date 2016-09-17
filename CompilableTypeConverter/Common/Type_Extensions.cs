using System;
using System.Linq;
using System.Reflection;

namespace ProductiveRage.CompilableTypeConverter.Common
{
	public static class Type_Extensions
	{
		public static bool HasProperty(this Type type, PropertyInfo property, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (property == null)
				throw new ArgumentNullException("property");

			// Doing a Contains on the concat'd property sets won't do the job since there may be multiple PropertyInfo instances floating
			// around that represent the same data but that aren't the same instance
			return type
				.GetProperties(bindingFlags)
				.Concat(
					type.GetInterfaces().SelectMany(i => i.GetProperties(bindingFlags))
				)
				.Any(p => p.MatchesProperty(property));
		}
	}
}
