using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Graphics
{
    public abstract class GraphicResource
    {
        private GraphicResourceID id;

        protected GraphicResource(GraphicResourceID id)
        {
            this.id = id;
            ResourceName = GetType().Name;
        }

        public GraphicResourceID ResourceID
        {
            get
            {
                return id;
            }
        }

        public abstract void Release();

        /// <summary>
        /// A user define name for this reource, used for debug and tracing purposes.
        /// </summary>
        public virtual string ResourceName { get; set; }

        public override string ToString()
        {
            return ResourceName;
        }
    }
}
