using System;
using System.Collections;
using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public class ReadOnlyListAdapter
    {
        public static IReadOnlyList<TTo> From<TFrom,TTo>(IReadOnlyList<TFrom> from, Func<TFrom, TTo> proj)
        {
            return new ReadOnlyListAdapter<TFrom, TTo>(from, proj);
        }
    }

    public class ReadOnlyListAdapter<TFrom, TTo> : ReadOnlyListAdapter, IReadOnlyList<TTo>
    {
        private IReadOnlyList<TFrom> _from;
        private Func<TFrom, TTo> _proj;

        public ReadOnlyListAdapter(IReadOnlyList<TFrom> from, Func<TFrom, TTo> proj)
        {
            _from = from;
            _proj = proj;
        }

        public TTo this[int index] => _proj(_from[index]);

        public int Count => _from.Count;

        public IEnumerator<TTo> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}
