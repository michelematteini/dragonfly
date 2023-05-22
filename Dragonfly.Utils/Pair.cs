using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Utils
{
    public class Pair<T1, T2>
    {
        public T1 First { get; set; }
        public T2 Second { get; set; }

        public Pair()
        {

        }

        public Pair(T1 first, T2 second)
        {
            First = first;
            Second = second;
        }
    }
}
