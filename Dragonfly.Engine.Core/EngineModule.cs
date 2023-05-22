using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Engine.Core
{
    public abstract class EngineModule : IEngineModule
    {
        protected internal EngineContext Context { get; set; }

        public EngineModule() { }

        public abstract void OnModuleAdded();
        
    }
}
