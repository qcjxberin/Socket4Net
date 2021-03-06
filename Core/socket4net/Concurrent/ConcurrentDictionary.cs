﻿#region MIT
//  /*The MIT License (MIT)
// 
//  Copyright 2016 lizs lizs4ever@163.com
//  
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//   * */
#endregion

#if NET35
using System;
using System.Collections;
using System.Collections.Generic;

namespace socket4net
{
    /// <summary>
    /// 并发字典
    /// 兼容.net3.5
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class ConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();
        private readonly object _syncRoot = new object();
       
        public int Count
        {
            get
            {
                lock (_syncRoot)
                {
                    return _dictionary.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                return TryGetValue(key, out value) ? value : default(TValue);
            }
            set { TryAdd(key, value); }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                lock (_syncRoot)
                {
                    return _dictionary.Keys;
                }
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                lock (_syncRoot)
                {
                    return _dictionary.Values;
                }
            }
        }

        public void Clear()
        {
            lock (_syncRoot)
            {
                _dictionary.Clear();
            }
        }

        public bool ContainsKey(TKey key)
        {
            lock (_syncRoot)
            {
                return _dictionary.ContainsKey(key);
            }
        }

        public void Add(TKey key, TValue value)
        {
            lock (_syncRoot)
            {
                _dictionary.Add(key, value);
            }
        }

        public bool Remove(TKey key)
        {
            lock (_syncRoot)
            {
                return _dictionary.Remove(key);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_syncRoot)
            {
                return _dictionary.TryGetValue(key, out value);
            }
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            lock (_syncRoot)
            {
                if (_dictionary.ContainsKey(key))
                {
                    value = _dictionary[key];
                    return _dictionary.Remove(key);
                }

                value = default(TValue);
                return false;
            }
        }


        public bool TryAdd(TKey key, TValue value)
        {
            lock (_syncRoot)
            {
                if (_dictionary.ContainsKey(key))
                    return false;

                _dictionary[key] = value;
                return true;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            lock (_syncRoot)
            {
                foreach (var kv in _dictionary)
                {
                    yield return kv;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#region not implemented

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException("");
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException("");
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException("");
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException("");
        }

        #endregion
    }
}
#endif