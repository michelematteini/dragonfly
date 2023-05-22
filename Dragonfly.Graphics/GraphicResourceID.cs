using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Graphics
{
    public class GraphicResourceID : IEquatable<GraphicResourceID>
    { 
        private static int nextAutoID = 0;
        private int id;

        public GraphicResourceID(int id)
        {
            this.id = id;
        }

        public GraphicResourceID() : this(nextAutoID++) { }

        public bool Equals(GraphicResourceID other)
        {
            return other == this;
        }

        public override bool Equals(object obj)
        {
            GraphicResourceID gresObj = obj as GraphicResourceID;
            return gresObj == this;
        }

        public override int GetHashCode()
        {
            return id;
        }

        public static bool operator ==(GraphicResourceID id1, GraphicResourceID id2)
        {
            if (ReferenceEquals(id1, null) && ReferenceEquals(id2, null)) return true;
            if (ReferenceEquals(id1, null) || ReferenceEquals(id2, null)) return false;

            return id1.id == id2.id;
        }

        public static bool operator !=(GraphicResourceID id1, GraphicResourceID id2)
        {
            return !(id1 == id2); 
        }

        public override string ToString()
        {
            return GetHashCode().ToString();
        }
    }
}
