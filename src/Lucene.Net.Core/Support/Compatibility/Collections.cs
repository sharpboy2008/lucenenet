﻿using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace Lucene.Net
{
    public static class Collections
    {
        public static bool AddAll<T>(ISet<T> set, IEnumerable<T> elements)
        {
            bool result = false;
            foreach (T element in elements)
            {
                result |= set.Add(element);
            }
            return result;
        }

        public static IList<T> EmptyList<T>()
        {
            return (IList<T>)Enumerable.Empty<T>();
        }

        public static IDictionary<TKey, TValue> EmptyMap<TKey, TValue>()
        {
            return new Dictionary<TKey, TValue>();
        }

        public static ISet<T> NewSetFromMap<T, S>(IDictionary<T, bool?> map)
        {
            return new SetFromMap<T>(map);
        }

        public static IComparer<T> ReverseOrder<T>()
        {
            return (IComparer<T>)ReverseComparer<T>.REVERSE_ORDER;
        }

        public static IComparer<T> ReverseOrder<T>(IComparer<T> cmp)
        {
            if (cmp == null)
                return ReverseOrder<T>();

            if (cmp is ReverseComparer2<T>)
                return ((ReverseComparer2<T>)cmp).cmp;

            return new ReverseComparer2<T>(cmp);
        }

        public static void Shuffle<T>(IList<T> list)
        {
            Shuffle(list, new Random());
        }

        // Method found here http://stackoverflow.com/a/2301091/181087
        // This shuffles the list in place without using LINQ, which is fast and efficient.
        public static void Shuffle<T>(IList<T> list, Random random)
        {
            for (int i = list.Count; i > 1; i--)
            {
                int pos = random.Next(i);
                var x = list[i - 1];
                list[i - 1] = list[pos];
                list[pos] = x;
            }
        }

        public static ISet<T> Singleton<T>(T o)
        {
            return new HashSet<T>(new T[] { o });
        }

        public static IDictionary<TKey, TValue> SingletonMap<TKey, TValue>(TKey key, TValue value)
        {
            return new Dictionary<TKey, TValue> { { key, value } };
        }

        public static void Swap<T>(IList<T> list, int index1, int index2)
        {
            T tmp = list[index1];
            list[index1] = list[index2];
            list[index2] = tmp;
        }

        public static IList<T> UnmodifiableList<T>(IList<T> list)
        {
            return new UnmodifiableListImpl<T>(list);
        }

        public static IDictionary<TKey, TValue> UnmodifiableMap<TKey, TValue>(IDictionary<TKey, TValue> d)
        {
            return new UnmodifiableDictionary<TKey, TValue>(d);
        }

        public static ICollection<T> UnmodifiableSet<T>(ICollection<T> list)
        {
            return new UnmodifiableSetImpl<T>(list);
        }

        #region Nested Types

        #region SetFromMap
        internal class SetFromMap<T> : ICollection<T>, IEnumerable<T>, IEnumerable, ISet<T>, IReadOnlyCollection<T>
#if FEATURE_SERIALIZABLE
            , ISerializable, IDeserializationCallback
#endif
        {
            private readonly IDictionary<T, bool?> m; // The backing map
#if FEATURE_SERIALIZABLE
            [NonSerialized]
#endif
            private ICollection<T> s;

            internal SetFromMap(IDictionary<T, bool?> map)
            {
                if (map.Any())
                    throw new ArgumentException("Map is not empty");
                m = map;
                s = map.Keys;
            }

            public void Clear()
            {
                m.Clear();
            }

            public int Count
            {
                get
                {
                    return m.Count;
                }
            }

            // LUCENENET: IsEmpty doesn't exist here

            public bool Contains(T item)
            {
                return m.ContainsKey(item);
            }

            public bool Remove(T item)
            {
                return m.Remove(item);
            }

            public bool Add(T item)
            {
                m.Add(item, true);
                return m.ContainsKey(item);
            }

            void ICollection<T>.Add(T item)
            {
                m.Add(item, true);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return s.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return s.GetEnumerator();
            }

            // LUCENENET: ToArray() is part of LINQ

            public override string ToString()
            {
                return s.ToString();
            }

            public override int GetHashCode()
            {
                return s.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj == this || s.Equals(obj);
            }

            public virtual bool ContainsAll(IEnumerable<T> other)
            {
                // we don't care about order, so sort both sequences before comparing
                return this.OrderBy(x => x).SequenceEqual(other.OrderBy(x => x));
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                m.Keys.CopyTo(array, arrayIndex);
            }


            public bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public bool SetEquals(IEnumerable<T> other)
            {
                if (other == null)
                {
                    throw new ArgumentNullException("other");
                }
                SetFromMap<T> set = other as SetFromMap<T>;
                if (set != null)
                {
                    if (this.m.Count != set.Count)
                    {
                        return false;
                    }
                    return this.ContainsAll(set);
                }
                ICollection<T> is2 = other as ICollection<T>;
                if (((is2 != null) && (this.m.Count == 0)) && (is2.Count > 0))
                {
                    return false;
                }
                foreach (var item in this)
                {
                    if (!is2.Contains(item))
                    {
                        return false;
                    }
                }
                return true;
            }

            #region Not Implemented Members
            public void ExceptWith(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public void IntersectWith(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public bool IsProperSubsetOf(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public bool IsProperSupersetOf(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public bool IsSubsetOf(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public bool IsSupersetOf(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public bool Overlaps(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public void SymmetricExceptWith(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public void UnionWith(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

#if FEATURE_SERIALIZABLE
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                throw new NotImplementedException();
            }
#endif

            public void OnDeserialization(object sender)
            {
                throw new NotImplementedException();
            }
            #endregion
        }
        #endregion SetFromMap

        #region ReverseComparer

        //private class ReverseComparer : IComparer<IComparable>
        //{
        //    internal static readonly ReverseComparer REVERSE_ORDER = new ReverseComparer();


        //    public int Compare(IComparable c1, IComparable c2)
        //    {
        //        return c2.CompareTo(c1);
        //    }
        //}

        // LUCENENET NOTE: When consolidating this, it turns out that only the 
        // CaseInsensitiveComparer works correctly in .NET (not sure why).
        // So, this hybrid was made from the original Java implementation and the
        // original implemenation (above) that used CaseInsensitiveComparer.
        private class ReverseComparer<T> : IComparer<T>
        {
            internal static readonly ReverseComparer<T> REVERSE_ORDER = new ReverseComparer<T>();

            public int Compare(T x, T y)
            {
                return (new CaseInsensitiveComparer()).Compare(y, x);
            }
        }

        #endregion ReverseComparer

        #region ReverseComparer2

        private class ReverseComparer2<T> : IComparer<T>

        {
            /**
             * The comparer specified in the static factory.  This will never
             * be null, as the static factory returns a ReverseComparer
             * instance if its argument is null.
             *
             * @serial
             */
            internal readonly IComparer<T> cmp;

            public ReverseComparer2(IComparer<T> cmp)
            {
                Debug.Assert(cmp != null);
                this.cmp = cmp;
            }

            public int Compare(T t1, T t2)
            {
                return cmp.Compare(t2, t1);
            }

            public override bool Equals(object o)
            {
                return (o == this) ||
                    (o is ReverseComparer2<T> &&
                     cmp.Equals(((ReverseComparer2<T>)o).cmp));
            }

            public override int GetHashCode()
            {
                return cmp.GetHashCode() ^ int.MinValue;
            }

            public IComparer<T> Reversed()
            {
                return cmp;
            }
        }

        #endregion ReverseComparer2

        #region UnmodifiableListImpl

        private class UnmodifiableListImpl<T> : IList<T>
        {
            private readonly IList<T> list;

            public UnmodifiableListImpl(IList<T> list)
            {
                if (list == null)
                    throw new ArgumentNullException("list");
                this.list = list;
            }

            public T this[int index]
            {
                get
                {
                    return list[index];
                }
                set
                {
                    throw new InvalidOperationException("Unable to modify this list.");
                }
            }

            public int Count
            {
                get
                {
                    return list.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            public void Add(T item)
            {
                throw new InvalidOperationException("Unable to modify this list.");
            }

            public void Clear()
            {
                throw new InvalidOperationException("Unable to modify this list.");
            }

            public bool Contains(T item)
            {
                return list.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                list.CopyTo(array, arrayIndex);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return list.GetEnumerator();
            }

            public int IndexOf(T item)
            {
                return list.IndexOf(item);
            }

            public void Insert(int index, T item)
            {
                throw new InvalidOperationException("Unable to modify this list.");
            }

            public bool Remove(T item)
            {
                throw new InvalidOperationException("Unable to modify this list.");
            }

            public void RemoveAt(int index)
            {
                throw new InvalidOperationException("Unable to modify this list.");
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        #endregion UnmodifiableListImpl

        #region UnmodifiableDictionary

        private class UnmodifiableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
        {
            private IDictionary<TKey, TValue> _dict;

            public UnmodifiableDictionary(IDictionary<TKey, TValue> dict)
            {
                _dict = dict;
            }

            public UnmodifiableDictionary()
            {
                _dict = new Dictionary<TKey, TValue>();
            }

            public void Add(TKey key, TValue value)
            {
                throw new InvalidOperationException("Unable to modify this dictionary.");
            }

            public bool ContainsKey(TKey key)
            {
                return _dict.ContainsKey(key);
            }

            public ICollection<TKey> Keys
            {
                get { return _dict.Keys; }
            }

            public bool Remove(TKey key)
            {
                throw new InvalidOperationException("Unable to modify this dictionary.");
            }

            public bool TryGetValue(TKey key, out TValue value)
            {
                return _dict.TryGetValue(key, out value);
            }

            public ICollection<TValue> Values
            {
                get { return _dict.Values; }
            }

            public TValue this[TKey key]
            {
                get
                {
                    TValue ret;
                    _dict.TryGetValue(key, out ret);
                    return ret;
                }
                set
                {
                    throw new InvalidOperationException("Unable to modify this dictionary.");
                }
            }

            public void Add(KeyValuePair<TKey, TValue> item)
            {
                throw new InvalidOperationException("Unable to modify this dictionary.");
            }

            public void Clear()
            {
                throw new InvalidOperationException("Unable to modify this dictionary.");
            }

            public bool Contains(KeyValuePair<TKey, TValue> item)
            {
                return _dict.Contains(item);
            }

            public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
            {
                _dict.CopyTo(array, arrayIndex);
            }

            public int Count
            {
                get { return _dict.Count; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public bool Remove(KeyValuePair<TKey, TValue> item)
            {
                throw new InvalidOperationException("Unable to modify this dictionary.");
            }

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            {
                return _dict.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _dict.GetEnumerator();
            }
        }

        #endregion UnmodifiableDictionary

        #region UnmodifiableSetImpl

        private class UnmodifiableSetImpl<T> : ICollection<T>
        {
            private readonly ICollection<T> set;
            public UnmodifiableSetImpl(ICollection<T> set)
            {
                if (set == null)
                    throw new ArgumentNullException("set");
                this.set = set;
            }
            public int Count
            {
                get
                {
                    return set.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            public void Add(T item)
            {
                throw new InvalidOperationException("Unable to modify this set.");
            }

            public void Clear()
            {
                throw new InvalidOperationException("Unable to modify this set.");
            }

            public bool Contains(T item)
            {
                return set.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                set.CopyTo(array, arrayIndex);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return set.GetEnumerator();
            }

            public bool Remove(T item)
            {
                throw new InvalidOperationException("Unable to modify this set.");
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        #endregion

        #endregion Nested Types
    }
}
