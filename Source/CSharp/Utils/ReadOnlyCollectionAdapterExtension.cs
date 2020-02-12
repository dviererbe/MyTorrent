using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    /// <summary>
    /// Extension method for <see cref="ICollection{T}"/>.
    /// </summary>
    public static class ReadOnlyCollectionAdapterExtension
    {
        /// <summary>
        /// Adapts a <see cref="ICollection{T}"/> to an <see cref="IReadOnlyCollection{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements.
        /// </typeparam>
        /// <param name="collection">
        /// The <see cref="ICollection{T}"/> to be used to adapt to a <see cref="IReadOnlyCollection{T}"/>.
        /// </param>
        /// <returns>
        /// Adapter for an <see cref="ICollection{T}"/> to adapt the <see cref="IReadOnlyCollection{T}"/> interface.
        /// </returns>
        public static IReadOnlyCollection<T> AsReadOnly<T>(this ICollection<T> collection)
        {
            return new ReadOnlyCollectionAdapter<T>(collection);
        }
    }
}
