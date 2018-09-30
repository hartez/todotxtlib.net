using System;

namespace todotxtlib.net
{
	public static class StringExtensions
	{
		public static bool Contains(this string source, string toCheck, StringComparison comp)
		{
			return source.IndexOf(toCheck, comp) >= 0;
		}
	}
}