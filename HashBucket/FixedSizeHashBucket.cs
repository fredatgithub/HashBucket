﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Theraot.Threading
{
    /// <summary>
    /// Represent a fixed size thread-safe wait-free hash based dictionary.
    /// </summary>
    public class FixedSizeHashBucket<TKey, TValue>
    {
        private readonly int _capacity;
        private readonly IEqualityComparer<TKey> _keyComparer;

        private Bucket<KeyValuePair<TKey, TValue>> _entries;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixedSizeHashBucket" /> class.
        /// </summary>
        /// <param name="capacity">The initial capacity.</param>
        /// <param name="keyComparer">The key comparer.</param>
        public FixedSizeHashBucket(int capacity, IEqualityComparer<TKey> keyComparer)
        {
            _capacity = IntHelper.NextPowerOf2(capacity);
            _entries = new Bucket<KeyValuePair<TKey, TValue>>(_capacity);
            _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
        }

        /// <summary>
        /// Gets the capacity.
        /// </summary>
        public int Capacity
        {
            get
            {
                return _capacity;
            }
        }

        /// <summary>
        /// Gets the number of items actually contained.
        /// </summary>
        public int Count
        {
            get
            {
                return _entries.Count;
            }
        }

        /// <summary>
        /// Attempts to add the specified key and associated value at the default index.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="isCollision">if set to <c>true</c> the attempt resulted in a collision.</param>
        /// <returns>The index where the key and associated value were added.</returns>
        public int Add(TKey key, TValue value, out bool isCollision)
        {
            return Add(key, value, 0, out isCollision);
        }

        /// <summary>
        /// Attempts to add the specified key and associated value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="offset">The offset from the default index.</param>
        /// <param name="isCollision">if set to <c>true</c> the attempt resulted in a collision.</param>
        /// <returns>The index where the key and associated value were added.</returns>
        public int Add(TKey key, TValue value, int offset, out bool isCollision)
        {
            int index = Index(key, offset);
            var entry = new KeyValuePair<TKey, TValue>(key, value);
            KeyValuePair<TKey, TValue> previous;
            if (_entries.Insert(index, entry, out previous))
            {
                isCollision = false;
                return index;
            }
            else
            {
                isCollision = !_keyComparer.Equals(previous.Key, key);
                return -1;
            }
        }

        /// <summary>
        /// Determines whether the specified key is contained at the default index.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The index where the value was set; -1 otherwise.</returns>
        public int ContainsKey(TKey key)
        {
            return ContainsKey(key, 0);
        }

        /// <summary>
        /// Determines whether the specified key is contained.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="offset">The offset from the default index.</param>
        /// <returns>The index where the value was set; -1 otherwise.</returns>
        public int ContainsKey(TKey key, int offset)
        {
            int index = Index(key, offset);
            KeyValuePair<TKey, TValue> entry;
            if (_entries.TryGet(index, out entry))
            {
                if (_keyComparer.Equals(entry.Key, key))
                {
                    return index;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Gets the an <see cref="IEnumerable{TKey}" /> that allows to iterate over the contained keys.
        /// </summary>
        public IEnumerable<TKey> GetKeyEnumerable()
        {
            for (int index = 0; index < _capacity; index++)
            {
                KeyValuePair<TKey, TValue> entry;
                if (_entries.TryGet(index, out entry))
                {
                    yield return entry.Key;
                }
            }
        }

        /// <summary>
        /// Gets the an <see cref="IEnumerable{KeyValuePair{TKey, TValue}}" /> that allows to iterate over the contained keys and associated values.
        /// </summary>
        public IEnumerable<KeyValuePair<TKey, TValue>> GetKeyValuePairEnumerable()
        {
            for (int index = 0; index < _capacity; index++)
            {
                KeyValuePair<TKey, TValue> entry;
                if (_entries.TryGet(index, out entry))
                {
                    yield return entry;
                }
            }
        }

        /// <summary>
        /// Gets the an <see cref="IEnumerable{TValue}" /> that allows to iterate over the contained values.
        /// </summary>
        public IEnumerable<TValue> GetValueEnumerable()
        {
            for (int index = 0; index < _capacity; index++)
            {
                KeyValuePair<TKey, TValue> entry;
                if (_entries.TryGet(index, out entry))
                {
                    yield return entry.Value;
                }
            }
        }

        /// <summary>
        /// Determinates the index for a given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="offset">The offset from the default index.</param>
        public int Index(TKey key, int offset)
        {
            var hash = _keyComparer.GetHashCode(key);
            var index = (hash + offset) & (_capacity - 1);
            return index;
        }

        /// <summary>
        /// Attempts to removes the specified key at the default index.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The index where the value was set; -1 otherwise.</returns>
        public int Remove(TKey key)
        {
            return Remove(key, 0);
        }

        /// <summary>
        /// Attempts to removes the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="offset">The offset from the default index.</param>
        /// <returns>The index where the value was set; -1 otherwise.</returns>
        public int Remove(TKey key, int offset)
        {
            int index = Index(key, offset);
            KeyValuePair<TKey, TValue> entry;
            if (_entries.TryGet(index, out entry))
            {
                if (_keyComparer.Equals(entry.Key, key))
                {
                    if (_entries.RemoveAt(index))
                    {
                        return index;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="offset">The offset from the default index.</param>
        /// <param name="isNew">if set to <c>true</c> the key was not present before.</param>
        /// <returns>The index where the value was set; -1 otherwise.</returns>
        public int Set(TKey key, TValue value, int offset, out bool isNew)
        {
            int index = Index(key, offset);
            KeyValuePair<TKey, TValue> oldEntry;
            isNew = !_entries.TryGet(index, out oldEntry);
            if ((isNew || _keyComparer.Equals(key, oldEntry.Key)) && _entries.Set(index, new KeyValuePair<TKey, TValue>(key, value), out isNew))
            {
                return index;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Sets the value associated with the specified key at the default index.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="isNew">if set to <c>true</c> the key was not present before.</param>
        /// <returns>The index where the value was set; otherwise -1.</returns>
        public int Set(TKey key, TValue value, out bool isNew)
        {
            return Set(key, value, 0, out isNew);
        }

        /// <summary>
        /// Tries the retrieve the key and value at an specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if the value was retrieved; otherwise, <c>false</c>.
        /// </returns>
        public bool TryGet(int index, out TKey key, out TValue value)
        {
            KeyValuePair<TKey, TValue> entry;
            if (_entries.TryGet(index, out entry))
            {
                key = entry.Key;
                value = entry.Value;
                return true;
            }
            else
            {
                key = default(TKey);
                value = default(TValue);
                return false;
            }
        }

        /// <summary>
        /// Tries the retrieve the value associeted with the specified key at the default index.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>The index where the value was set; otherwise -1.</returns>
        public int TryGetValue(TKey key, out TValue value)
        {
            return TryGetValue(key, 0, out value);
        }

        /// <summary>
        /// Tries the retrieve the value associeted with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="offset">The offset from the default index.</param>
        /// <param name="value">The value.</param>
        /// <returns>The index where the value was set; otherwise -1.</returns>
        public int TryGetValue(TKey key, int offset, out TValue value)
        {
            int index = Index(key, offset);
            KeyValuePair<TKey, TValue> entry;
            if (_entries.TryGet(index, out entry))
            {
                if (_keyComparer.Equals(entry.Key, key))
                {
                    value = entry.Value;
                    return index;
                }
                else
                {
                    value = default(TValue);
                    return -1;
                }
            }
            else
            {
                value = default(TValue);
                return -1;
            }
        }

        /// <summary>
        /// Gets the values contained in this object.
        /// </summary>
        public IList<KeyValuePair<TKey, TValue>> Values
        {
            get
            {
                return _entries.Values;
            }
        }
    }
}