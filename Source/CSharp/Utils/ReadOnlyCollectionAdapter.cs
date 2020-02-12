namespace System.Collections.Generic
{
    /// <summary>
    /// Adapter for an <see cref="ICollection{T}"/> to adapt the <see cref="IReadOnlyCollection{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the elements.
    /// </typeparam>
    internal sealed class ReadOnlyCollectionAdapter<T> : IReadOnlyCollection<T>
    {
        private readonly ICollection<T> _collection;

        /// <summary>
        /// Initializes a new <see cref="ReadOnlyCollectionAdapter{T}"/> for an <see cref="ICollection{T}"/>.
        /// </summary>
        /// <param name="collection">
        /// The <see cref="ICollection{T}"/> to be used to adapt to a <see cref="IReadOnlyCollection{T}"/>.
        /// </param>
        public ReadOnlyCollectionAdapter(ICollection<T> collection)
        {
            _collection = collection;
        }

        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        public int Count => _collection.Count;

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator() => _collection.GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator() => (_collection as IEnumerable).GetEnumerator();
    }
}
