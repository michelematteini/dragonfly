using System;

namespace Dragonfly.Utils
{
    public struct Range<T>
    {
        public T From, To;

        public Range(T from, T to)
        {
            From = from;
            To = to;
        }

    }
}
