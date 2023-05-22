using System;
using System.Collections;
using System.Collections.Generic;

namespace Dragonfly.Utils
{
    /// <summary>
    /// A generic list which wrap an inner collection that is rebuild on each modification, so that its previous state will not be modified.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class InvariantList
    {
        /// <summary>
        /// Create an invariant list based on a generic list.
        /// </summary>
        public static InvariantList FromGenericList<T>()
        {
            return FromGenericList<T>(new List<T>());
        }

        /// <summary>
        /// Create an invariant list based on a generic list.
        /// </summary>
        public static InvariantList FromGenericList<T>(List<T> initialValue)
        {
            Func<IList, IList> cloneList = (IList list) => { return new List<T>(list as List<T>); };
            return new InvariantList(initialValue, cloneList);
        }

        private Func<IList, IList> cloneList;

        public InvariantList(IList initialValue, Func<IList, IList> cloneList)
        {
            List = initialValue;
            this.cloneList = cloneList;
        }

        /// <summary>
        /// Retrieve the inner list. This value will not be modified.
        /// </summary>
        public IList List
        {
            get; private set;
        }

        public void Add(object item)
        {
            List = cloneList(List);
            List.Add(item);
        }

        public void Remove(object item)
        {
            List = cloneList(List);
            List.Remove(item);
        }
    }
}
