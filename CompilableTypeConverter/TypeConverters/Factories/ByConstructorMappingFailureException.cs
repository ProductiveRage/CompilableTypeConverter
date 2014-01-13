using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace CompilableTypeConverter.TypeConverters.Factories
{
	[Serializable]
	public class ByConstructorMappingFailureException : MappingFailureException
	{
		public ByConstructorMappingFailureException(Type sourceType, Type destType, IEnumerable<ConstructorOptionFailureDetails> failedConstructorTargets)
			: base(GetMessage(sourceType, destType, failedConstructorTargets), sourceType, destType)
		{
			if (failedConstructorTargets == null)
				throw new ArgumentNullException("failedConstructorTargets");
			
			FailedConstructorTargets = failedConstructorTargets.ToList().AsReadOnly();
			if (FailedConstructorTargets.Any(f => f == null))
				throw new ArgumentException("Null reference encountered in failedConstructorTargets set");
		}

		private static string GetMessage(Type sourceType, Type destType, IEnumerable<ConstructorOptionFailureDetails> failedConstructorTargets)
		{
			if (sourceType == null)
				throw new ArgumentNullException("sourceType");
			if (destType == null)
				throw new ArgumentNullException("destType");
			if (failedConstructorTargets == null)
				throw new ArgumentNullException("failedConstructorTargets");

			string explanation;
			var failedConstructorTargetsArray = failedConstructorTargets.ToArray();
			if (!failedConstructorTargetsArray.Any())
				explanation = "no constructors were identified to try to match parameters to";
			else if (failedConstructorTargetsArray.Length == 1)
			{
				var failedConstructorTarget = failedConstructorTargetsArray[0];
				switch (failedConstructorTarget.FailureReason)
				{
					default:
						explanation = "unexplained failure";
						break;
					case ConstructorOptionFailureDetails.FailureReasonOptions.ParameterLessConstructorNotAllowed:
						explanation = "the only constructor attempted was the parameter-less one, which was not allow due to configuration";
						break;
					case ConstructorOptionFailureDetails.FailureReasonOptions.FilteredOutByPrioritiser:
						explanation = "the only constructor attempted filtered out by the constructor prioritiser";
						break;
					case ConstructorOptionFailureDetails.FailureReasonOptions.UnableToMapConstructorArgument:
						explanation = "the only constructor attempted failed when it came to map the argument \"" + failedConstructorTarget.ConstructorArgumentWhereApplicable.Name + "\"";
						break;
				}
			}
			else
				explanation = "various reasons (consult the FailedConstructorTargets set for detailed information)";

			return string.Format(
				"Unable to map type {0} to type {1} via constructor, {2}",
				sourceType,
				destType,
				explanation
			);
		}

		protected ByConstructorMappingFailureException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			FailedConstructorTargets = (IEnumerable<ConstructorOptionFailureDetails>)info.GetValue("FailedConstructorTargets", typeof(IEnumerable<ConstructorOptionFailureDetails>));
		}

		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			info.AddValue("FailedConstructorTargets", FailedConstructorTargets);
			base.GetObjectData(info, context);
		}

		/// <summary>
		/// This will never be null nor will it contain any null references
		/// </summary>
		public IEnumerable<ConstructorOptionFailureDetails> FailedConstructorTargets { get; private set; }

		[Serializable]
		public class ConstructorOptionFailureDetails
		{
			public ConstructorOptionFailureDetails(ConstructorInfo constructor, FailureReasonOptions failureReason, ParameterInfo constructorArgumentWhereApplicable)
			{
				if (constructor == null)
					throw new ArgumentNullException("constructor");
				if (!Enum.IsDefined(typeof(FailureReasonOptions), failureReason))
					throw new ArgumentOutOfRangeException("failureReason");
				if ((failureReason == FailureReasonOptions.UnableToMapConstructorArgument) && (constructorArgumentWhereApplicable == null))
					throw new ArgumentException("constructorArgumentWhereApplicable must not be null if failureReason is UnableToMapConstructorArgument");
				if ((failureReason != FailureReasonOptions.UnableToMapConstructorArgument) && (constructorArgumentWhereApplicable != null))
					throw new ArgumentException("constructorArgumentWhereApplicable must be null if failureReason is anything other than UnableToMapConstructorArgument");

				Constructor = constructor;
				FailureReason = failureReason;
				ConstructorArgumentWhereApplicable = constructorArgumentWhereApplicable;
			}

			/// <summary>
			/// This will never be null
			/// </summary>
			public ConstructorInfo Constructor { get; private set; }
			
			public FailureReasonOptions FailureReason { get; private set; }
			
			/// <summary>
			/// This will not be null if the FailureReason is UnableToMapConstructorArgument and will be null otherwise
			/// </summary>
			public ParameterInfo ConstructorArgumentWhereApplicable { get; private set; }

			public enum FailureReasonOptions
			{
				FilteredOutByPrioritiser,
				ParameterLessConstructorNotAllowed,
				UnableToMapConstructorArgument
			}
		}
	}
}
