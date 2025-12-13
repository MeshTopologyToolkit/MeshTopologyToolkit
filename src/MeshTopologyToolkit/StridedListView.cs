using System;
using System.Collections;
using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    internal class StridedListView<T> : IReadOnlyList<T> where T : notnull
    {
        private readonly IReadOnlyList<T> _list;
        private readonly int _start;
        private readonly int _stride;
        private readonly int _count;
        public StridedListView(IReadOnlyList<T> list, int start, int stride, int count)
        {
            _list = list;
            _start = start;
            _stride = stride;
            _count = count;
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _list[_start + index * _stride];
            }
        }
        public int Count => _count;

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; ++i)
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
