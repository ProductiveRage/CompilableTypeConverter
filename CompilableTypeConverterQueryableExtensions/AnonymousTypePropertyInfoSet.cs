using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace CompilableTypeConverterQueryableExtensions
{
	public class AnonymousTypePropertyInfoSet : IEnumerable<PropertyInfo>
	{
		private readonly int _hashCode;
		private readonly ReadOnlyCollection<PropertyInfo> _validatedProperties;
		public AnonymousTypePropertyInfoSet(IEnumerable<PropertyInfo> requiredReadAndWriteProperties)
		{
			if (requiredReadAndWriteProperties == null)
				throw new ArgumentNullException("requiredReadAndWriteProperties");

			_hashCode = 0;
			var validatedProperties = new List<PropertyInfo>();
			foreach (var property in requiredReadAndWriteProperties)
			{
				if (property == null)
					throw new ArgumentException("Null reference encountered in requiredReadAndWriteProperties set");
				if (property.GetIndexParameters().Any())
					throw new ArgumentException("Indexed properties are not supported");

				// If the same name appears multiple times then ignore the duplication, this may just mean that the same property
				// was required multiple times by the caller (in the context of the type converter, it may mean that the same
				// property on the source object was used for multiple properties / constructor arguments on the target)
				var propertiesWithTheSameName = validatedProperties.Where(p => p.Name == property.Name);
				if (propertiesWithTheSameName.Any())
				{
					// Unless there are multiple properties with the same name but different types
					var firstPropertyWithSameNameButDifferentType = propertiesWithTheSameName.FirstOrDefault(p => p.PropertyType != property.PropertyType);
					if (firstPropertyWithSameNameButDifferentType != null)
						throw new ArgumentException("Multiple properties name \"" + property.Name + "\" with different types - invalid");
				}
				else
				{
					validatedProperties.Add(property);
					_hashCode ^= property.Name.GetHashCode();
					_hashCode ^= property.PropertyType.GetHashCode();
				}
			}
			_validatedProperties = validatedProperties.OrderBy(p => p.Name).ToList().AsReadOnly();
		}

		/// <summary>
		/// This will never be null nor contain any null references. There will be no indexed properties and there will be no duplicated
		/// property names. This set will be ordered by property name.
		/// </summary>
		public IEnumerable<PropertyInfo> ValidatedProperties { get { return _validatedProperties; } }

		public override bool Equals(object obj)
		{
			var validatedPropertySet = obj as AnonymousTypePropertyInfoSet;
			if (validatedPropertySet == null)
				return false;

			if (_validatedProperties.Count != validatedPropertySet._validatedProperties.Count)
				return false;

			for (var index = 0; index < _validatedProperties.Count; index++)
			{
				if ((_validatedProperties[index].Name != validatedPropertySet._validatedProperties[index].Name)
				|| (_validatedProperties[index].PropertyType != validatedPropertySet._validatedProperties[index].PropertyType))
					return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			return _hashCode;
		}

		public IEnumerator<PropertyInfo> GetEnumerator()
		{
			return ValidatedProperties.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
