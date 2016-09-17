namespace ProductiveRage.CompilableTypeConverter.TypeConverters
{
	/// <summary>
	/// These options are only applicable to by-property-setting conversions, 
	/// </summary>
	public enum ByPropertySettingNullSourceBehaviourOptions
	{
		/// <summary>
		/// If a null source reference is passed then an empty instance of the target type will be generated, with every property that would have
		/// been populated set to the default value for that type
		/// </summary>
		CreateEmptyInstanceWithDefaultPropertyValues,

		/// <summary>
		/// If a null source reference is passed then a default(TDest) will be returned
		/// </summary>
		UseDestDefaultIfSourceIsNull
	}
}
