using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Engine.Core
{
    public static class EngineFactory
    {
        public static EngineContext CreateContext(EngineParams ep)
        {
            return new EngineContext(ep);
        }

    }
}
