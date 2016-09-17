using System;
using System.Collections.Generic;
using System.Linq;

namespace ProductiveRage.CompilableTypeConverter.QueryableExtensions
{
	public static class IQueryable_Extensions
	{
		/// <summary>
		/// This is an extension method to make using the ProjectionConverter's GetProjection method easier more natural when dealing with an IQueryable result
		/// set. The type parameter inference will not perform partial matches, so in order to prevent having to specify TSource when you already have an
		/// IQueryable of TSource, calling the Project method effectively encapsulates TSource in the returned CompilableTypeConverterProjectionConfigurer
		/// instance, from which the To method may be called, specifying only a TDest type param.
		/// </summary>
		public static CompilableTypeConverterProjectionConfigurer<TSource> Project<TSource>(this IQueryable<TSource> source)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			return new CompilableTypeConverterProjectionConfigurer<TSource>(source);
		}

		public class CompilableTypeConverterProjectionConfigurer<TSource>
		{
			private readonly IQueryable<TSource> _source;
			public CompilableTypeConverterProjectionConfigurer(IQueryable<TSource> source)
			{
				if (source == null)
					throw new ArgumentNullException("source");

				_source = source;
			}

			public IEnumerable<TDest> To<TDest>()
			{
				return ProjectionConverter.GetProjection<TSource, TDest>()(_source);
			}
		}
	}
}