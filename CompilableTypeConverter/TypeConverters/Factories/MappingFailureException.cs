using System;
using System.Runtime.Serialization;

namespace ProductiveRage.CompilableTypeConverter.TypeConverters.Factories
{
	[Serializable]
	public class MappingFailureException : Exception
	{
		public MappingFailureException(string message, Type sourceType, Type destType) : base(message)
		{
			if (sourceType == null)
				throw new ArgumentNullException("sourceType");
			if (destType == null)
				throw new ArgumentNullException("DestType");

			SourceType = sourceType;
			DestType = destType;
		}

		protected MappingFailureException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			SourceType = (Type)info.GetValue("SourceType", typeof(Type));
			DestType = (Type)info.GetValue("DestType", typeof(Type));
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			info.AddValue("SourceType", SourceType);
			info.AddValue("DestType", DestType);
			base.GetObjectData(info, context);
		}

		/// <summary>
		/// This will never be null
		/// </summary>
		public Type SourceType { get; private set; }

		/// <summary>
		/// This will never be null
		/// </summary>
		public Type DestType { get; private set; }
	}
}
