using Dragonfly.Utils;
using System;
using System.Collections.Generic;

namespace Dragonfly.Engine.Core
{
    /// <summary>
    /// A filter used to match a particular set of materials to a render pass, based on their class.
    /// </summary>
    public struct MaterialClassFilter
    {
        /// <summary>
        /// A placeholder for a filter, that when specified as ClassName, makes it apply to any class.
        /// </summary>
        public const string ANY_CLASS = "";

        public MaterialClassFilterType Type;
        public string ClassName;

        public MaterialClassFilter(MaterialClassFilterType type, string className)
        {
            Type = type;
            ClassName = className;
        }

        /// <summary>
        /// Check if the currect filter accept the specified class. Returns false if the specified class is excluded by this filter, true otherwise.
        /// </summary>
        public void Apply(CompMaterial m, ref bool accepted)
        {
            if(Type == MaterialClassFilterType.Include)
            {
                if (accepted) return; // already accepted...
                accepted = string.IsNullOrEmpty(ClassName) || m.Class.Contains(ClassName);
            }
            else
            {
                if (!accepted) return; // already excluded...
                accepted = !string.IsNullOrEmpty(ClassName) && !m.Class.Contains(ClassName);
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)Type, ClassName.GetHashCode());
        }

        public static int GetQueryHash(List<MaterialClassFilter> filterList)
        {
            return HashCode.Combine(filterList);
        }

        /// <summary>
        /// Check if the specified material is selected by a query made of a list of filters.
        /// </summary>
        public static bool ApplyList(List<MaterialClassFilter> filterList, CompMaterial m)
        {
            bool accepted = false;
            foreach (MaterialClassFilter filter in filterList)
                filter.Apply(m, ref accepted);
            return accepted;
        }
    }

    public enum MaterialClassFilterType
    {
        Include,
        Exclude
    }
}
