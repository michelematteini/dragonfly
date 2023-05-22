using System.Collections.Generic;

namespace Dragonfly.Utils
{
    public static class ListEx
    {
        public static T Pop<T>(this List<T> list)
        {
            T lastElem = default(T);
            if (list.Count > 0)
            {
                lastElem = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
            }

            return lastElem;
        }

    }
}
