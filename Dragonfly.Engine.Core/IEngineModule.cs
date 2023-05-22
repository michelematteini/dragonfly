using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Engine.Core
{
    public interface IEngineModule
    {
        /// <summary>
        /// Called by the engine when this module is added to a context.
        /// </summary>
        void OnModuleAdded();
    }
}
