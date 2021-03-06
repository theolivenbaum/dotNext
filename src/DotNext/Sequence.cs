using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DotNext
{
    /// <summary>
    /// Various methods to work with classes implementing <see cref="IEnumerable{T}"/> interface.
    /// </summary>
    public static class Sequence
    {
        private sealed class NotNullEnumerable<T> : IEnumerable<T>
            where T : class
        {
            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly IEnumerator<T?> enumerator;
                private T? current;

                internal Enumerator(IEnumerable<T?> enumerable)
                    => enumerator = enumerable.GetEnumerator();

                public T Current => current ?? throw new InvalidOperationException();

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    for (T? current; enumerator.MoveNext();)
                    {
                        current = enumerator.Current;
                        if (current != null)
                        {
                            this.current = current;
                            return true;
                        }
                    }
                    return false;
                }

                public void Reset() => enumerator.Reset();

                void IDisposable.Dispose()
                {
                    current = null;
                    enumerator.Dispose();
                }
            }

            private readonly IEnumerable<T?> enumerable;

            internal NotNullEnumerable(IEnumerable<T?> enumerable)
                => this.enumerable = enumerable;

            public IEnumerator<T> GetEnumerator() => new Enumerator(enumerable);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private const int HashSalt = -1521134295;

        private static int GetHashCode(int hash, object? obj) => hash * HashSalt + obj?.GetHashCode() ?? 0;

        /// <summary>
        /// Computes hash code for the sequence of objects.
        /// </summary>
        /// <param name="sequence">The sequence of elements.</param>
		/// <param name="salted"><see langword="true"/> to include randomized salt data into hashing; <see langword="false"/> to use data from memory only.</param>
        /// <returns>The hash code computed from each element in the sequence.</returns>
        public static int SequenceHashCode(this IEnumerable<object?> sequence, bool salted = true)
        {
            var hashCode = sequence.Aggregate(-910176598, GetHashCode);
            return salted ? hashCode * HashSalt + RandomExtensions.BitwiseHashSalt : hashCode;
        }

        internal static bool SequenceEqual(IEnumerable<object>? first, IEnumerable<object>? second)
            => first is null || second is null ? ReferenceEquals(first, second) : Enumerable.SequenceEqual(first, second);

        /// <summary>
        /// Apply specified action to each collection element.
        /// </summary>
        /// <typeparam name="T">Type of elements in the collection.</typeparam>
        /// <param name="collection">A collection to enumerate. Cannot be <see langword="null"/>.</param>
        /// <param name="action">An action to applied for each element.</param>
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action) => ForEach(collection, new ValueAction<T>(action, true));

        /// <summary>
        /// Apply specified action to each collection element.
        /// </summary>
        /// <typeparam name="T">Type of elements in the collection.</typeparam>
        /// <param name="collection">A collection to enumerate. Cannot be <see langword="null"/>.</param>
        /// <param name="action">An action to applied for each element.</param>
        public static void ForEach<T>(this IEnumerable<T> collection, in ValueAction<T> action)
        {
            foreach (var item in collection)
                action.Invoke(item);
        }

        /// <summary>
        /// Obtains first value type in the sequence; or <see langword="null"/>
        /// if sequence is empty.
        /// </summary>
        /// <typeparam name="T">Type of elements in the sequence.</typeparam>
        /// <param name="seq">A sequence to check. Cannot be <see langword="null"/>.</param>
        /// <returns>First element in the sequence; or <see langword="null"/> if sequence is empty. </returns>
        public static T? FirstOrNull<T>(this IEnumerable<T> seq)
            where T : struct
        {
            using var enumerator = seq.GetEnumerator();
            return enumerator.MoveNext() ? enumerator.Current : new T?();
        }

        /// <summary>
        /// Obtains first value in the sequence; or <see cref="Optional{T}.Empty"/>
        /// if sequence is empty.
        /// </summary>
        /// <typeparam name="T">Type of elements in the sequence.</typeparam>
        /// <param name="seq">A sequence to check. Cannot be <see langword="null"/>.</param>
        /// <returns>The first element in the sequence; or <see cref="Optional{T}.Empty"/> if sequence is empty. </returns>
        public static Optional<T> FirstOrEmpty<T>(this IEnumerable<T> seq)
        {
            using var enumerator = seq.GetEnumerator();
            return enumerator.MoveNext() ? enumerator.Current : Optional<T>.Empty;
        }

        /// <summary>
        /// Returns the first element in a sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="T">The type of the elements of source.</typeparam>
        /// <param name="seq">A collection to return an element from.</param>
        /// <param name="filter">A function to test each element for a condition.</param>
        /// <returns>The first element in the sequence that matches to the specified filter; or empty value.</returns>
        public static Optional<T> FirstOrEmpty<T>(this IEnumerable<T> seq, in ValueFunc<T, bool> filter)
        {
            foreach (var item in seq)
                if (filter.Invoke(item))
                    return item;
            return Optional<T>.Empty;
        }

        /// <summary>
        /// Returns the first element in a sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="T">The type of the elements of source.</typeparam>
        /// <param name="seq">A collection to return an element from.</param>
        /// <param name="filter">A function to test each element for a condition.</param>
        /// <returns>The first element in the sequence that matches to the specified filter; or empty value.</returns>
        public static Optional<T> FirstOrEmpty<T>(this IEnumerable<T> seq, Predicate<T> filter)
            => FirstOrEmpty(seq, filter.AsValueFunc(true));

        /// <summary>
        /// Bypasses a specified number of elements in a sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerator">Enumerator to modify. Cannot be <see langword="null"/>.</param>
        /// <param name="count">The number of elements to skip.</param>
        /// <returns><see langword="true"/>, if current element is available; otherwise, <see langword="false"/>.</returns>
        public static bool Skip<T>(this IEnumerator<T> enumerator, int count)
        {
            while (count > 0)
                if (enumerator.MoveNext())
                    count--;
                else
                    return false;
            return true;
        }

        private static bool ElementAt<T>(this IList<T> list, int index, [NotNullWhen(true)]out T element)
        {
            if (index >= 0 && index < list.Count)
            {
                element = list[index];
                return true;
            }
            else
            {
                element = default!;
                return false;
            }
        }

        private static bool ElementAt<T>(this IReadOnlyList<T> list, int index, [NotNullWhen(true)]out T element)
        {
            if (index >= 0 && index < list.Count)
            {
                element = list[index];
                return true;
            }
            else
            {
                element = default!;
                return false;
            }
        }

        /// <summary>
        /// Obtains elements at the specified index in the sequence.
        /// </summary>
        /// <remarks>
        /// This method is optimized for types <see cref="IList{T}"/>
        /// and <see cref="IReadOnlyList{T}"/>.
        /// </remarks>
        /// <typeparam name="T">Type of elements in the sequence.</typeparam>
        /// <param name="collection">Source collection.</param>
        /// <param name="index">Index of the element to read.</param>
        /// <param name="element">Obtained element.</param>
        /// <returns><see langword="true"/>, if element is available in the collection and obtained successfully; otherwise, <see langword="false"/>.</returns>
        public static bool ElementAt<T>(this IEnumerable<T> collection, int index, [NotNullWhen(true)]out T element)
        {
            switch (collection)
            {
                case IList<T> list:
                    return ElementAt(list, index, out element);
                case IReadOnlyList<T> readOnlyList:
                    return ElementAt(readOnlyList, index, out element);
                default:
                    using (var enumerator = collection.GetEnumerator())
                    {
                        enumerator.Skip(index);
                        if (enumerator.MoveNext())
                        {
                            element = enumerator.Current;
                            return true;
                        }
                        else
                        {
                            element = default!;
                            return false;
                        }
                    }
            }
        }

        /// <summary>
        /// Skip <see langword="null"/> values in the collection.
        /// </summary>
        /// <typeparam name="T">Type of elements in the collection.</typeparam>
        /// <param name="collection">A collection to check. Cannot be <see langword="null"/>.</param>
        /// <returns>Modified lazy collection without <see langword="null"/> values.</returns>
        public static IEnumerable<T> SkipNulls<T>(this IEnumerable<T?> collection)
            where T : class
            => new NotNullEnumerable<T>(collection);

        /// <summary>
        /// Concatenates each element from the collection into single string.
        /// </summary>
        /// <typeparam name="T">Type of array elements.</typeparam>
        /// <param name="collection">Collection to convert. Cannot be <see langword="null"/>.</param>
        /// <param name="delimiter">Delimiter between elements in the final string.</param>
        /// <param name="ifEmpty">A string to be returned if collection has no elements.</param>
        /// <returns>Converted collection into string.</returns>
        public static string ToString<T>(this IEnumerable<T> collection, string delimiter, string ifEmpty = "")
            => string.Join(delimiter, collection).IfNullOrEmpty(ifEmpty);

        /// <summary>
        /// Constructs a sequence from the single element.
        /// </summary>
        /// <typeparam name="T">Type of element.</typeparam>
        /// <param name="item">An item to be placed into sequence.</param>
        /// <returns>Sequence of single element.</returns>
        public static IEnumerable<T> Singleton<T>(T item)
            => Collections.Generic.List.Singleton(item);

        /// <summary>
        /// Adds <paramref name="items"/> to the beginning of <paramref name="collection"/>.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="collection">The collection to be concatenated with the items.</param>
        /// <param name="items">The items to be added to the beginning of the collection.</param>
        /// <returns>The concatenated collection.</returns>
        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> collection, params T[] items)
            => items.Concat(collection);

        /// <summary>
        /// Adds <paramref name="items"/> to the end of <paramref name="collection"/>.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="collection">The collection to be concatenated with the items.</param>
        /// <param name="items">The items to be added to the end of the collection.</param>
        /// <returns>The concatenated collection.</returns>
        public static IEnumerable<T> Append<T>(this IEnumerable<T> collection, params T[] items)
            => collection.Concat(items);
    }
}