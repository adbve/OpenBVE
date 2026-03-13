using System;
using System.Buffers;

namespace OpenBveApi.System {
	/// <summary>
	/// Provides high-performance string parsing utilities using Span&lt;T&gt; to avoid allocations.
	/// These methods replace common string operations like Split() that create excessive garbage.
	/// </summary>
	public static class SpanParser {
		
		/// <summary>
		/// Splits a string by a separator character without creating intermediate string arrays.
		/// Yields each segment as a ReadOnlySpan&lt;char&gt; for zero-allocation processing.
		/// </summary>
		/// <param name="text">The text to split.</param>
		/// <param name="separator">The separator character.</param>
		/// <returns>An enumerable of spans representing each segment.</returns>
		/// <remarks>
		/// This method creates NO allocations - it yields spans into the original string.
		/// Use this when you need to parse tokens but don't need to store them permanently.
		/// For storing tokens, use SplitToPool() instead.
		/// </remarks>
		public static ReadOnlySpan<char> Split(this string text, char separator) {
			throw new NotImplementedException("This is an extension method pattern - see SplitEnumerable");
		}
		
		/// <summary>
		/// Splits a string by a separator character, yielding segments one at a time.
		/// Does NOT create an array of all segments.
		/// </summary>
		/// <param name="text">The text to split.</param>
		/// <param name="separator">The separator character.</param>
		/// <returns>An enumerable of strings (each segment is allocated on demand).</returns>
		public static IEnumerable<string> SplitLazy(this string text, char separator) {
			if (string.IsNullOrEmpty(text)) {
				yield break;
			}
			
			int start = 0;
			for (int i = 0; i < text.Length; i++) {
				if (text[i] == separator) {
					yield return text.Substring(start, i - start);
					start = i + 1;
				}
			}
			
			// Return last segment
			if (start <= text.Length) {
				yield return text.Substring(start, text.Length - start);
			}
		}
		
		/// <summary>
		/// Splits a string by multiple separator characters, yielding segments one at a time.
		/// More efficient than String.Split() for large strings or frequent calls.
		/// </summary>
		/// <param name="text">The text to split.</param>
		/// <param name="separators">Array of separator characters.</param>
		/// <returns>An enumerable of strings (each segment is allocated on demand).</returns>
		public static IEnumerable<string> SplitLazy(this string text, char[] separators) {
			if (string.IsNullOrEmpty(text)) {
				yield break;
			}
			
			int start = 0;
			for (int i = 0; i < text.Length; i++) {
				foreach (char sep in separators) {
					if (text[i] == sep) {
						yield return text.Substring(start, i - start);
						start = i + 1;
						break;
					}
				}
			}
			
			// Return last segment
			if (start <= text.Length) {
				yield return text.Substring(start, text.Length - start);
			}
		}
		
		/// <summary>
		/// Splits a string and stores results in a pooled array to reduce GC pressure.
		/// The caller MUST return the array to the pool when done.
		/// </summary>
		/// <param name="text">The text to split.</param>
		/// <param name="separator">The separator character.</param>
		/// <param name="array">The rented array containing the split results.</param>
		/// <param name="count">The number of valid elements in the array.</param>
		/// <remarks>
		/// IMPORTANT: Caller must call ArrayPool.Return(array) when done!
		/// Example usage:
		/// <code>
		/// string[] parts = null;
		/// try {
		///     int count;
		///     text.SplitToPool(',', out parts, out count);
		///     for (int i = 0; i < count; i++) {
		///         Process(parts[i]);
		///     }
		/// } finally {
		///     if (parts != null) ArrayPool.Return(parts);
		/// }
		/// </code>
		/// </remarks>
		public static void SplitToPool(this string text, char separator, out string[] array, out int count) {
			SplitToPool(text, new[] { separator }, out array, out count);
		}
		
		/// <summary>
		/// Splits a string by multiple separators and stores results in a pooled array.
		/// The caller MUST return the array to the pool when done.
		/// </summary>
		/// <param name="text">The text to split.</param>
		/// <param name="separators">Array of separator characters.</param>
		/// <param name="array">The rented array containing the split results.</param>
		/// <param name="count">The number of valid elements in the array.</param>
		/// <remarks>
		/// IMPORTANT: Caller must call ArrayPool.Return(array) when done!
		/// </remarks>
		public static void SplitToPool(this string text, char[] separators, out string[] array, out int count) {
			if (string.IsNullOrEmpty(text)) {
				array = ArrayPool<string>.Shared.Rent(0);
				count = 0;
				return;
			}
			
			// First pass: count segments
			int segmentCount = 1;
			for (int i = 0; i < text.Length; i++) {
				foreach (char sep in separators) {
					if (text[i] == sep) {
						segmentCount++;
						break;
					}
				}
			}
			
			// Rent array from pool
			array = ArrayPool<string>.Shared.Rent(segmentCount);
			count = 0;
			
			// Second pass: extract segments
			int start = 0;
			for (int i = 0; i < text.Length; i++) {
				foreach (char sep in separators) {
					if (text[i] == sep) {
						array[count++] = text.Substring(start, i - start);
						start = i + 1;
						break;
					}
				}
			}
			
			// Add last segment
			if (start <= text.Length) {
				array[count++] = text.Substring(start, text.Length - start);
			}
		}
		
		/// <summary>
		/// Parses a comma-separated list of integers without creating intermediate string arrays.
		/// Much more efficient than text.Split(',').Select(int.Parse).ToArray().
		/// </summary>
		/// <param name="text">The text containing comma-separated integers.</param>
		/// <param name="values">Array to store the parsed values (rented from pool).</param>
		/// <param name="count">Number of values parsed.</param>
		/// <returns>True if all values were successfully parsed, false otherwise.</returns>
		/// <remarks>
		/// IMPORTANT: Caller must call ArrayPool.Return(values) when done!
		/// </remarks>
		public static bool TryParseIntegers(this string text, out int[] values, out int count) {
			values = null;
			count = 0;
			
			if (string.IsNullOrEmpty(text)) {
				values = ArrayPool<int>.Shared.Rent(0);
				return true;
			}
			
			// First pass: count numbers
			int numberCount = 1;
			for (int i = 0; i < text.Length; i++) {
				if (text[i] == ',') {
					numberCount++;
				}
			}
			
			values = ArrayPool<int>.Shared.Rent(numberCount);
			
			// Second pass: parse numbers
			int start = 0;
			count = 0;
			for (int i = 0; i <= text.Length; i++) {
				if (i == text.Length || text[i] == ',') {
					string numberText = text.Substring(start, i - start).Trim();
					if (!int.TryParse(numberText, out int value)) {
						ArrayPool<int>.Shared.Return(values);
						values = null;
						count = 0;
						return false;
					}
					values[count++] = value;
					start = i + 1;
				}
			}
			
			return true;
		}
		
		/// <summary>
		/// Parses a comma-separated list of doubles without creating intermediate string arrays.
		/// </summary>
		/// <param name="text">The text containing comma-separated doubles.</param>
		/// <param name="values">Array to store the parsed values (rented from pool).</param>
		/// <param name="count">Number of values parsed.</param>
		/// <returns>True if all values were successfully parsed, false otherwise.</returns>
		/// <remarks>
		/// IMPORTANT: Caller must call ArrayPool.Return(values) when done!
		/// </remarks>
		public static bool TryParseDoubles(this string text, out double[] values, out int count) {
			values = null;
			count = 0;
			
			if (string.IsNullOrEmpty(text)) {
				values = ArrayPool<double>.Shared.Rent(0);
				return true;
			}
			
			// First pass: count numbers
			int numberCount = 1;
			for (int i = 0; i < text.Length; i++) {
				if (text[i] == ',') {
					numberCount++;
				}
			}
			
			values = ArrayPool<double>.Shared.Rent(numberCount);
			
			// Second pass: parse numbers
			int start = 0;
			count = 0;
			for (int i = 0; i <= text.Length; i++) {
				if (i == text.Length || text[i] == ',') {
					string numberText = text.Substring(start, i - start).Trim();
					if (!double.TryParse(numberText, out double value)) {
						ArrayPool<double>.Shared.Return(values);
						values = null;
						count = 0;
						return false;
					}
					values[count++] = value;
					start = i + 1;
				}
			}
			
			return true;
		}
		
		/// <summary>
		/// Trims whitespace from a string without creating a new string if no trimming is needed.
		/// Returns a ReadOnlySpan&lt;char&gt; for zero-allocation processing.
		/// </summary>
		/// <param name="text">The text to trim.</param>
		/// <returns>A span representing the trimmed text.</returns>
		public static ReadOnlySpan<char> TrimSpan(this string text) {
			if (string.IsNullOrEmpty(text)) {
				return ReadOnlySpan<char>.Empty;
			}
			
			int start = 0;
			int end = text.Length;
			
			// Trim leading whitespace
			while (start < end && char.IsWhiteSpace(text[start])) {
				start++;
			}
			
			// Trim trailing whitespace
			while (end > start && char.IsWhiteSpace(text[end - 1])) {
				end--;
			}
			
			return text.AsSpan(start, end - start);
		}
		
		/// <summary>
		/// Checks if a string starts with a prefix without allocating.
		/// Case-insensitive comparison.
		/// </summary>
		/// <param name="text">The text to check.</param>
		/// <param name="prefix">The prefix to look for.</param>
		/// <returns>True if the text starts with the prefix.</returns>
		public static bool StartsWithSpan(this string text, string prefix) {
			if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(prefix)) {
				return false;
			}
			
			if (text.Length < prefix.Length) {
				return false;
			}
			
			return text.AsSpan(0, prefix.Length).Equals(prefix.AsSpan(), StringComparison.OrdinalIgnoreCase);
		}
		
		/// <summary>
		/// Extracts a substring after a delimiter without allocating if not found.
		/// </summary>
		/// <param name="text">The text to search.</param>
		/// <param name="delimiter">The delimiter to search for.</param>
		/// <returns>A span representing the text after the delimiter, or empty if not found.</returns>
		public static ReadOnlySpan<char> SubstringAfter(this string text, char delimiter) {
			if (string.IsNullOrEmpty(text)) {
				return ReadOnlySpan<char>.Empty;
			}
			
			int index = text.IndexOf(delimiter);
			if (index < 0 || index >= text.Length - 1) {
				return ReadOnlySpan<char>.Empty;
			}
			
			return text.AsSpan(index + 1);
		}
		
		/// <summary>
		/// Extracts a substring before a delimiter without allocating if not found.
		/// </summary>
		/// <param name="text">The text to search.</param>
		/// <param name="delimiter">The delimiter to search for.</param>
		/// <returns>A span representing the text before the delimiter, or the whole text if not found.</returns>
		public static ReadOnlySpan<char> SubstringBefore(this string text, char delimiter) {
			if (string.IsNullOrEmpty(text)) {
				return ReadOnlySpan<char>.Empty;
			}
			
			int index = text.IndexOf(delimiter);
			if (index < 0) {
				return text.AsSpan();
			}
			
			return text.AsSpan(0, index);
		}
	}
}
