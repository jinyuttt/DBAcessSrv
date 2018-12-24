using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DBServer
{
    public class LRUCache<TKey,TValue>
    {
        const int DEFAULT_CAPACITY = 255;

        int _capacity;
        ReaderWriterLockSlim _locker;
        IDictionary<TKey, TValue> _dictionary;
        LinkedList<TKey> _linkedList;
       IDictionary<TKey, LinkedListNode<TKey>> dicIndex=null;

        public LRUCache() : this(DEFAULT_CAPACITY) { }

        public LRUCache(int capacity)
        {
            _locker = new ReaderWriterLockSlim();
            _capacity = capacity > 0 ? capacity : DEFAULT_CAPACITY;
            _dictionary = new Dictionary<TKey, TValue>();
            _linkedList = new LinkedList<TKey>();
            dicIndex = new Dictionary<TKey, LinkedListNode<TKey>>();
        }

        public void Set(TKey key, TValue value)
        {
            _locker.EnterWriteLock();
            try
            {
                _dictionary[key] = value;
                LinkedListNode<TKey> node = null;
                if(dicIndex.TryGetValue(key,out node))
                {
                    _linkedList.Remove(node);
                }
               
                dicIndex[key]=_linkedList.AddFirst(key);
                if (_linkedList.Count > _capacity)
                {
                    _dictionary.Remove(_linkedList.Last.Value);
                    _linkedList.RemoveLast();
                    dicIndex.Remove(_linkedList.Last.Value);
                }
            }
            finally { _locker.ExitWriteLock(); }
        }

        public bool TryGet(TKey key, out TValue value)
        {
            _locker.EnterUpgradeableReadLock();
            try
            {
                bool b = _dictionary.TryGetValue(key, out value);
                if (b)
                {
                    _locker.EnterWriteLock();
                    try
                    {
                        LinkedListNode<TKey> node = null;
                        if (dicIndex.TryGetValue(key, out node))
                        {
                            _linkedList.Remove(node);
                        }

                        dicIndex[key]=_linkedList.AddFirst(key);
                    }
                    finally { _locker.ExitWriteLock(); }
                }
                return b;
            }
            catch { throw; }
            finally { _locker.ExitUpgradeableReadLock(); }
        }

        public bool TryRemove(TKey key)
        {

            _locker.EnterWriteLock();
            try
            {
                bool b = _dictionary.Remove(key);
                if (b)
                {
                     LinkedListNode<TKey> node = null;
                    if (dicIndex.TryGetValue(key, out node))
                    {
                        _linkedList.Remove(node);
                        dicIndex.Remove(key);
                    }
                }
                return b;
            }
            catch { throw; }
            finally { _locker.ExitWriteLock(); }
        }

        public void Clear()
        {
            _locker.EnterWriteLock();
            try
            {
                dicIndex.Clear();
                _dictionary.Clear();
                _linkedList.Clear();
            }
            finally { _locker.ExitWriteLock(); }
            
        }
        public bool ContainsKey(TKey key)
        {
            _locker.EnterReadLock();
            try
            {
                return _dictionary.ContainsKey(key);
            }
            finally { _locker.ExitReadLock(); }
        }

        public int Count
        {
            get
            {
                _locker.EnterReadLock();
                try
                {
                    return _dictionary.Count;
                }
                finally { _locker.ExitReadLock(); }
            }
        }

        public int Capacity
        {
            get
            {
                _locker.EnterReadLock();
                try
                {
                    return _capacity;
                }
                finally { _locker.ExitReadLock(); }
            }
            set
            {
                _locker.EnterUpgradeableReadLock();
                try
                {
                    if (value > 0 && _capacity != value)
                    {
                        _locker.EnterWriteLock();
                        try
                        {
                            _capacity = value;
                            while (_linkedList.Count > _capacity)
                            {
                                _linkedList.RemoveLast();
                            }
                        }
                        finally { _locker.ExitWriteLock(); }
                    }
                }
                finally { _locker.ExitUpgradeableReadLock(); }
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                _locker.EnterReadLock();
                try
                {
                    return _dictionary.Keys;
                }
                finally { _locker.ExitReadLock(); }
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                _locker.EnterReadLock();
                try
                {
                    return _dictionary.Values;
                }
                finally { _locker.ExitReadLock(); }
            }
        }
    }
}
