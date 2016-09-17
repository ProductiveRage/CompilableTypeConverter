using System;
using System.Reflection;

namespace ProductiveRage.CompilableTypeConverter.Common
{
	public static class Property_Extensions
	{
		public static bool MatchesProperty(this PropertyInfo property, PropertyInfo otherProperty)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			if (otherProperty == null)
				throw new ArgumentNullException("otherProperty");

			// Doing a Contains on the concat'd property sets won't do the job since there may be multiple PropertyInfo instances floating
			// around that represent the same data but that aren't the same instance
			return
				(otherProperty.Name == property.Name) &&
				(otherProperty.PropertyType == property.PropertyType) &&
				(otherProperty.DeclaringType == property.DeclaringType) &&
				DoTypeArraysMatch(otherProperty.GetIndexParameters(), property.GetIndexParameters());
		}

		private static bool DoTypeArraysMatch(ParameterInfo[] x, ParameterInfo[] y)
		{
			if ((x == null) && (y == null))
				return true;
			else if ((x == null) || (y == null))
				return false;

			if (x.Length != y.Length)
				return false;
			for (var index = 0; index < x.Length; index++)
			{
				if ((x[index].Name != y[index].Name) || (x[index].ParameterType != y[index].ParameterType))
					return false;
			}
			return true;
		}
	}
}
