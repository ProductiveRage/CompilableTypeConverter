using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace ProductiveRage.CompilableTypeConverter.TypeConverters.Factories
{
	[Serializable]
	public class ByPropertyMappingFailureException : MappingFailureException
	{
		public ByPropertyMappingFailureException(Type sourceType, Type destType, FailureReasonOptions failureReason, PropertyInfo destProperty)
			: base(GetMessage(sourceType, destType, failureReason, destProperty), sourceType, destType)
		{
			if (failureReason == FailureReasonOptions.NoParameterLessConstructor)
			{
				if (destProperty != null)
					throw new ArgumentException("destProperty must be null if failureReason is NoParameterLessConstructor");
			}
			else if (failureReason == FailureReasonOptions.UnableToMapProperty)
			{
				if (destProperty == null)
					throw new ArgumentException("destProperty must not be be null if failureReason is UnableToMapProperty");
			}
			else
				throw new ArgumentOutOfRangeException("failureReason");

			FailureReason = failureReason;
			DestProperty = destProperty;
		}

		private static string GetMessage(Type sourceType, Type destType, FailureReasonOptions failureReason, PropertyInfo destProperty)
		{
			if (sourceType == null)
				throw new ArgumentNullException("sourceType");
			if (destType == null)
				throw new ArgumentNullException("destType");

			string explanation;
			if (failureReason == FailureReasonOptions.NoParameterLessConstructor)
				explanation = "no parameter-less constructor available";
			else if (failureReason == FailureReasonOptions.UnableToMapProperty)
			{
				if (destProperty == null)
					throw new ArgumentException("destProperty may not be null if failureReason is UnableToMapProperty");
				explanation = "unable to map property \"" + destProperty.Name + "\"";
			}
			else
				throw new ArgumentOutOfRangeException("failureReason");

			return string.Format(
				"Unable to map type {0} to type {1} by property-setting, {2}",
				sourceType,
				destType,
				explanation
			);
		}

		protected ByPropertyMappingFailureException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			FailureReason = (FailureReasonOptions)info.GetValue("FailureReason", typeof(FailureReasonOptions));
			DestProperty = (PropertyInfo)info.GetValue("DestProperty", typeof(PropertyInfo));
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			info.AddValue("FailureReason", FailureReason);
			info.AddValue("DestProperty", DestProperty);
			base.GetObjectData(info, context);
		}

		public FailureReasonOptions FailureReason { get; private set; }

		/// <summary>
		/// This will be null if FailureReason is NoParameterLessConstructor and non-null if it is UnableToMapProperty
		/// </summary>
		public PropertyInfo DestProperty { get; private set; }

		public enum FailureReasonOptions
		{
			NoParameterLessConstructor,
			UnableToMapProperty
		}
	}
}
