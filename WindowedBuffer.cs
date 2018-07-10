using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace gUSBampSyncDemoCS
{
    /// <summary>
    /// Represents a synchronized circular buffer with a fixed capacity (the window).
    /// New elements can be enqueued. When the capacity is reached, the oldest (first-in) enqueued element will be overwritten by the newest.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the buffer.</typeparam>
    public class WindowedBuffer<T>
    {
        #region Private Members...

        /// <summary>
        /// The actual buffer where the elements are contained.
        /// </summary>
        LinkedList<T> _buffer;

        #endregion

        #region Properties...

        int _capacity;

        /// <summary>
        /// Gets the total amount of elements that the <see cref="WindowedBuffer{T}"/> can contain.
        /// </summary>
        /// <remarks>If value equals zero, the buffer will be treated as its capacity is infinite. In this case it stores any incoming elements and will never remove an element automatically.</remarks>
        public int Capacity
        {
            get { return _capacity; }
        }

        /// <summary>
        /// Gets the amount of current enqueued elements.
        /// </summary>
        public int Count
        {
            get { return _buffer.Count; }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="WindowedBuffer{T}"/>.
        /// </summary>
        public object SyncRoot
        {
            get { return ((ICollection) _buffer).SyncRoot; }
        }

        /// <summary>
        /// Gets the element at the position specified in <paramref name="index"/> starting from the first/oldest enqueued element in the buffer.
        /// </summary>
        /// <param name="index">The zero-based index of the element starting from the first/oldest enqueued element in the buffer.</param>
        /// <returns>The element at the speciefied index.</returns>
        /// <exception cref="IndexOutOfRangeException">Will be thrown, if if <paramref name="index"/> is less than zero or if <paramref name="index"/> is greater than or equals <see cref="Count"/>.</exception>
        public T this[int index]
        {
            get
            {
                lock (SyncRoot)
                {
                    if (index < 0 || index >= this.Count)
                        throw new IndexOutOfRangeException("Index exceeds array dimensions or is less than zero.");

                    LinkedListNode<T> curNode = _buffer.First;
                    int count = 0;

                    while (count < index && curNode != null)
                    {
                        curNode = curNode.Next;
                        count++;
                    }

                    return curNode.Value;
                }
            }
        }

        #endregion

        /// <summary>
        /// Creates a new instance of the <see cref="WindowedBuffer{T}"/> class with the specified capacity.
        /// The capacity will remain fixed and can't be changed any more.
        /// </summary>
        /// <param name="capacity">
        /// The total amount of elements that the <see cref="WindowedBuffer{T}"/> can contain. The <paramref name="capacity"/> will remain fixed and can't be changed any more.
        /// If <paramref name="capacity"/> equals zero, the buffer will be treated as its capacity is infinite. In this case it stores any incoming elements and will never remove an element automatically.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Will be thrown if <paramref name="capacity"/> is less than zero.</exception>
        /// <remarks>If <paramref name="capacity"/> equals zero, the buffer will be treated as its capacity is infinite. In this case it stores any incoming elements and will never remove an element automatically.</remarks>
        public WindowedBuffer(int capacity)
        {
            //capacity has to be equal or greater than one
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity", capacity, "Capacity is less than zero.");

            //set capacity
            this._capacity = capacity;

            //initialize buffer
            _buffer = new LinkedList<T>();
        }

        /// <summary>
        /// Adds an object to the end of the list.
        /// </summary>
        /// <param name="item">The objects to add to the <see cref="WindowedBuffer{T}"/>.</param>
        /// <remarks>
        /// If <see cref="Capacity"/> does not equal zero and <see cref="Count"/> already equals the capacity, the oldest object at the beginning of the list will be removed from the list by calling <see cref="WindowedBuffer{T}.Dequeue()"/>.
        /// <para>If <see cref="Capacity"/> equals zero, item will just be enqueued.</para>
        /// </remarks>
        public void Enqueue(T item)
        {
            lock (SyncRoot)
            {
                //if buffer is completely filled, remove oldest element before adding the new one
                if (this.Capacity != 0 && _buffer.Count == this.Capacity)
                    Dequeue();

                //now enqueue new item
                _buffer.AddLast(item);
            }
        }

        /// <summary>
        /// Adds objects to the end of the list.
        /// </summary>
        /// <param name="items">The objects to add to the <see cref="WindowedBuffer{T}"/>.</param>
        /// <remarks>
        /// If <see cref="Capacity"/> does not equal zero and number of items to add exceeds the <see cref="Capacity"/>, oldest items will be dequeued by calling <see cref="WindowedBuffer{T}.Dequeue()"/>.
        /// <para>If <see cref="Capacity"/> equals zero, items will just be enqueued.</para>
        /// </remarks>
        public void Enqueue(params T[] items)
        {
            lock (SyncRoot)
            {
                //now enqueue new items
                foreach (T item in items)
                    Enqueue(item);
            }
        }

        /// <summary>
        /// Removes and returns the first/oldest object at the beginning of the list.
        /// </summary>
        /// <returns>The first/oldest object at the beginning of the list.</returns>
        /// <exception cref="InvalidOperationException">Will be thrown if the list is empty.</exception>
        public T Dequeue()
        {
            lock (SyncRoot)
            {
                if (_buffer.Count == 0)
                    throw new InvalidOperationException("The list is empty.");

                T result = _buffer.First.Value;
                _buffer.RemoveFirst();

                return result;
            }
        }

        /// <summary>
        /// Removes and returns a number of <paramref name="numItems"/> first/oldest elements from the beginning of the list.
        /// </summary>
        /// <param name="numItems">The number of elements that should be removed and returned.</param>
        /// <returns>An array with a maximum number of <paramref name="numItems"/> elements. If there are less than <paramref name="numItems"/> elements in the list, only the available elements will be removed and returned.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Will be thrown if <paramref name="numItems"/> is less than zero.</exception>
        /// <remarks>If there are less than <paramref name="numItems"/> elements in the list, only the available elements will be removed and returned.</remarks>
        public T[] Dequeue(int numItems)
        {
            lock (SyncRoot)
            {
                if (numItems < 0)
                    throw new ArgumentOutOfRangeException("numItems", numItems, "Parameter is less than zero.");

                T[] result;

                //create resulting array
                int numReturnableItems = Math.Min(numItems, _buffer.Count);
                result = new T[numReturnableItems];

                //successively remove and return items from the list
                for (int i = 0; i < numReturnableItems; i++)
                    result[i] = Dequeue();

                return result;
            }
        }

        /// <summary>
        /// Returns a number of <paramref name="numItems"/> latest/newest enqueued items without removing them.
        /// If there are less than <paramref name="numItems"/> elements in the list, only the available elements will be returned.
        /// </summary>
        /// <param name="numItems">The number of elements that should be returned.</param>
        /// <returns>A number of <paramref name="numItems"/> latest/newest enqueued items. If there are less than <paramref name="numItems"/> elements in the list, only the available elements will be returned.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Will be thrown, if <paramref name="numItems"/> is less than zero.</exception>
        public T[] PeekNewest(int numItems)
        {
            lock (SyncRoot)
            {
                if (numItems < 0)
                    throw new ArgumentOutOfRangeException("numItems", numItems, "Parameter is less than zero.");

                T[] result;

                //create resulting array
                int numReturnableItems = (numItems <= _buffer.Count) ? numItems : _buffer.Count;
                result = new T[numReturnableItems];

                //successively remove and return items from the list
                LinkedListNode<T> node = _buffer.Last;
                for (int i = (numReturnableItems - 1); i >= 0; i--)
                {
                    result[i] = node.Value;
                    node = node.Previous;
                }

                return result;
            }
        }

        /// <summary>
        /// Clears the buffer.
        /// </summary>
        public void Clear()
        {
            lock (SyncRoot)
            {
                _buffer.Clear();
            }
        }
    }
}