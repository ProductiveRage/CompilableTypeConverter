using System;
using System.Reflection;

namespace CompilableTypeConverter.TypeConverters
{
    public class PropertyMappingDetails
    {
		public PropertyMappingDetails(PropertyInfo sourceProperty, string destinationName, Type destinationType)
		{
			if (sourceProperty == null)
				throw new ArgumentNullException("source");
			if (string.IsNullOrWhiteSpace(destinationName))
				throw new ArgumentException("Null/blank destinationName specified");
			if (destinationType == null)
				throw new ArgumentNullException("destinationType");

			SourceProperty = sourceProperty;
			DestinationName = destinationName;
			DestinationType = destinationType;
		}

		/// <summary>
		/// This will never be null
		/// </summary>
		public PropertyInfo SourceProperty { get; private set; }

		/// <summary>
		/// This will never be null or blank. It may refer to a croperty or a constructor argument on the destination type, it depends upon the conversion mechanism
		/// </summary>
		public string DestinationName { get; private set; }

		/// <summary>
		/// This will never be null
		/// </summary>
		public Type DestinationType { get; private set; }
	}
}
