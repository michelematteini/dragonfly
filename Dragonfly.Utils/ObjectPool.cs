using System;
using System.Collections.Generic;

namespace Dragonfly.Utils
{
    /// <summary>
    /// Manages a pool of objects of the same type, allowing for re-use of the same.
    /// </summary>
    public class ObjectPool<ObjType>
    {
        private Stack<ObjType> unused;
        private Dictionary<int, ObjType> used;
        private Func<ObjType> typeConstructor;
        private Action<ObjType> typeReset;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeConstructor">A function called to construct a new instance of the type.</param>
        /// <param name="typeReset">A function used to reset an instance of the type before reusing it.</param>
        public ObjectPool(Func<ObjType> typeConstructor, Action<ObjType> typeReset)
        {
            unused = new Stack<ObjType>();
            used = new Dictionary<int, ObjType>();
            this.typeConstructor = typeConstructor;
            this.typeReset = typeReset;
            ObjectHashFunction = o => o.GetHashCode();
        }

        public Func<ObjType, int> ObjectHashFunction { get; set; }

        public ObjType CreateNew()
        {
            if (unused.Count == 0)
                unused.Push(typeConstructor());
            ObjType newObj = unused.Pop();
            typeReset(newObj);
            used.Add(ObjectHashFunction(newObj), newObj);
            return newObj;
        }

        public void Free(ObjType obj)
        {
            if(used.Remove(ObjectHashFunction(obj)))
                unused.Push(obj);
        }

        /// <summary>
        /// Free all the objects allocated by this pool
        /// </summary>
        public void FreeAll()
        {
            foreach (ObjType obj in used.Values)
                unused.Push(obj);
            used.Clear();
        }

    }
}
