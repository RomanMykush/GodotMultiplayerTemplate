using System;
using System.Collections;
using System.Collections.Generic;

namespace SteampunkDnD.Shared;

public class BidirectionalDictionary<T, U> : IEnumerable<KeyValuePair<T, U>>
    where T : notnull
    where U : notnull
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

    public void Add(T firstKey, U secondKey)
    {
        if (firstKey == null)
            throw new ArgumentNullException(nameof(firstKey));
        if (secondKey == null)
            throw new ArgumentNullException(nameof(secondKey));

        if (_forward.ContainsKey(firstKey))
            throw new ArgumentException($"An item with the same first key has already been added. First key: {firstKey}");
        if (_reverse.ContainsKey(secondKey))
            throw new ArgumentException($"An item with the same second key has already been added. Second key: {secondKey}");

        _forward.Add(firstKey, secondKey);
        _reverse.Add(secondKey, firstKey);
    }

    public void RemoveByFirstKey(T firstKey)
    {
        U revKey = Forward[firstKey];
        _forward.Remove(firstKey);
        _reverse.Remove(revKey);
    }

    public void RemoveBySecondKey(U secondKey)
    {
        T forwardKey = Reverse[secondKey];
        _reverse.Remove(secondKey);
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
