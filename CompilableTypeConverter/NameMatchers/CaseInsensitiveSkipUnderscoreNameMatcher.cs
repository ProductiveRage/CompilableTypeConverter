using System;

namespace ProductiveRage.CompilableTypeConverter.NameMatchers
{
	/// <summary>
	/// Determine whether two property names can be considered equal for mappings (this ignores case and ignores any underscore but considers no other transformations)
	/// </summary>
	public class CaseInsensitiveSkipUnderscoreNameMatcher : INameMatcher
	{
		public bool IsMatch(string from, string to)
		{
			from = (from ?? "").Trim();
			if (from == "")
				throw new ArgumentNullException("from");
			to = (to ?? "").Trim();
			if (to == "")
				throw new ArgumentNullException("to");

			return from.Replace("_", "").Equals(to.Replace("_", ""), StringComparison.OrdinalIgnoreCase);
		}
	}
}
