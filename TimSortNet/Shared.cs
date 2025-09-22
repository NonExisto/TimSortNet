using System.Runtime.CompilerServices;

namespace TimSortNet;

internal static class Shared
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsLowerThan<T, TComparer>(this TComparer comparer, T x, T y) where TComparer : IComparer<T>
	{
		ArgumentNullException.ThrowIfNull(comparer);
		return comparer.Compare(x, y) <= 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static CopyContext<T> CopyFromAndUpdate<T>(this Span<T> dest, Span<T> src, int len)
	{
		src[..len].CopyTo(dest);
		return new(dest[len..], src[len..]);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static CopyContext<T> CopyFromBackAndUpdate<T>(this Span<T> dest, Span<T> src)
	{
		dest[^1] = src[^1];
		return new(dest[..^1], src[..^1]);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static CopyContext<T> CopyFromBackAndUpdate<T>(this Span<T> dest, Span<T> src, int len)
	{
		src[^len..].CopyTo(dest[^len..]);
		return new(dest[..^len], src[..^len]);
	}

	internal readonly ref struct CopyContext<T>
	{
		public Span<T> X { get; }
		public Span<T> Y { get; }

		public CopyContext(Span<T> x, Span<T> y)
		{
			X = x;
			Y = y;
		}

		public void Deconstruct(out Span<T> x, out Span<T> y)
		{
			x = X;
			y = Y;
		}
	}
}

internal struct Unit { }
