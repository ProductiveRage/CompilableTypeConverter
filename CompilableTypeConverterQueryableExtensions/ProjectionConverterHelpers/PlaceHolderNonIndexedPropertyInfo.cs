using System;
using System.Globalization;
using System.Reflection;

namespace CompilableTypeConverter.QueryableExtensions.ProjectionConverterHelpers
{
	/// <summary>
	/// This may be used to include named properties in an AnonyousTypePropertyInfoSet such that a property will be included in the generated type
	/// that doesn't originate in any existing type. The use of this class should be limited to that purpose only since most of its implementation
	/// will throw NotImplementedExceptions. It always returns zero index parameters, as must all properties that are to be included as part of an
	/// AnonyousTypePropertyInfoSet's data.
	/// </summary>
	public class PlaceHolderNonIndexedPropertyInfo : PropertyInfo
	{
		private readonly string _name;
		private readonly Type _propertyType;
		public PlaceHolderNonIndexedPropertyInfo(string name, Type propertyType)
			: base()
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Null/blank name specified");
			if (propertyType == null)
				throw new ArgumentNullException("propertyType");

			_name = name;
			_propertyType = propertyType;
		}

		public override string Name { get { return _name; } }
		public override Type PropertyType { get { return _propertyType; } }

		public override ParameterInfo[] GetIndexParameters() { return new ParameterInfo[0]; }

		public override PropertyAttributes Attributes { get { throw new NotImplementedException(); } }
		public override bool CanRead { get { throw new NotImplementedException(); } }
		public override bool CanWrite { get { throw new NotImplementedException(); } }
		public override MethodInfo[] GetAccessors(bool nonPublic) { throw new NotImplementedException(); }
		public override MethodInfo GetGetMethod(bool nonPublic) { throw new NotImplementedException(); }
		public override MethodInfo GetSetMethod(bool nonPublic) { throw new NotImplementedException(); }
		public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) { throw new NotImplementedException(); }
		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) { throw new NotImplementedException(); }
		public override Type DeclaringType { get { throw new NotImplementedException(); } }
		public override object[] GetCustomAttributes(Type attributeType, bool inherit) { throw new NotImplementedException(); }
		public override object[] GetCustomAttributes(bool inherit) { throw new NotImplementedException(); }
		public override bool IsDefined(Type attributeType, bool inherit) { throw new NotImplementedException(); }
		public override Type ReflectedType { get { throw new NotImplementedException(); } }
	}
}