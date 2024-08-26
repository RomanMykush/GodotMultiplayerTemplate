using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SteampunkDnD.Shared;

public class BidirectionalDictionary<T, U> : IEnumerable<KeyValuePair<T, U>>
{
    private readonly Dictionary<T, U> _forward = new();
    private readonly Dictionary<U, T> _reverse = new();

    public Indexer<T, U> Forward { get; private set; }
    public Indexer<U, T> Reverse { get; private set; }

    public BidirectionalDictionary()
    {
        Forward = new Indexer<T, U>(_forward);
        Reverse = new Indexer<U, T>(_reverse);
    }

    public void Add(T t, U u)
    {
        if (t == null)
            throw new ArgumentNullException("Value cannot be null. (Parameter 'first key')");
        if (u == null)
            throw new ArgumentNullException("Value cannot be null. (Parameter 'second key')");

        if (_forward.ContainsKey(t))
            throw new ArgumentException($"An item with the same first key has already been added. First key: {t}");
        if (_reverse.ContainsKey(u))
            throw new ArgumentException($"An item with the same second key has already been added. Second key: {u}");

        _forward.Add(t, u);
        _reverse.Add(u, t);
    }

    public void RemoveByFirstKey(T t)
    {
        U revKey = Forward[t];
        _forward.Remove(t);
        _reverse.Remove(revKey);
    }

    public void RemoveBySecondKey(U u)
    {
        T forwardKey = Reverse[u];
        _reverse.Remove(u);
        _forward.Remove(forwardKey);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<KeyValuePair<T, U>> GetEnumerator() =>
        _forward.GetEnumerator();

    public class Indexer<E, R>
    {
        private readonly Dictionary<E, R> _dictionary;

        public Indexer(Dictionary<E, R> dictionary) =>
            _dictionary = dictionary;

        public R this[E index]
        {
            get => _dictionary[index];
            set => _dictionary[index] = value;
        }

        public bool Contains(E key) => _dictionary.ContainsKey(key);
    }
}
