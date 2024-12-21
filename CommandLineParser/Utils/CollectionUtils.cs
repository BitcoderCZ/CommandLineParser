namespace CommandLineParser.Utils;

internal static class CollectionUtils
{
	public static IEnumerable<T> GetNth<T>(this IReadOnlyList<T> collection, int n)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(n, 0);

		if (n == 0)
		{
			yield break;
		}

		for (int i = 0; i < collection.Count; i += n)
		{
			yield return collection[i];
		}
	}
}
