using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;

namespace Dragonfly.Engine.Core
{
    public class Component : IComponent, IEquatable<Component>
    {
        private static int NEXT_ID = 0;

        private Component parent;
        private List<Component> children;
        private bool active;

        public Component(Component parent) : this(parent.Context, parent.ComManager)
        {
            Parent = parent;
            Active = parent.active;
        }

        /// <summary>
        /// A user-defined name for this component.
        /// </summary>
        public string Name { get; set; }

        internal Component(EngineContext context, ComponentManager compManager)
        {
            ID = NEXT_ID++;
            active = true;
            Ready = true;
            Context = context;
            ComManager = compManager;
            Name = this.GetType().Name;
            ComManager.Add(this);
        }

        ~Component()
        {
            // to avoid memory leaks, but can fail if the engine environment has not been correctly disposed.
            try { Dispose(); } catch { }
        }

        internal bool IsRoot { get; set; }

        public int ID { get; private set; }

        /// <summary>
        /// The component that own this. All components must have a (not null) parent, which can be changed setting this property.
        /// </summary>
        public Component Parent
        {
            get
            {
                return parent;
            }
            set
            {
                if (IsRoot)
                    throw new InvalidOperationException("The root component cannot be moved!");

                if (value == null)
                    throw new ArgumentNullException();

                if (Context != value.Context)
                    throw new InvalidOperationException("A component cannot be moved to a parent from another engine instance!");

                if(parent != null)
                    parent.children.Remove(this);

                if (value.children == null)
                    value.children = new List<Component>();
                value.children.Add(this);
                parent = value;
            }
        }

        /// <summary>
        /// True if the component is completely initialized and can be used, false otherwise. 
        /// If the value of this property is false, the engine will not take this component into account when rendering.
        /// Checked on each frame.
        /// </summary>
        public virtual bool Ready { get; protected set; }

        /// <summary>
        /// If set to false, the engine will not take this component into account when rendering.
        /// Checked on each frame.
        /// Differently from Ready, this is a user value that can be used to enable / disable the component.
        /// The value set to this property, will propagate to child components.
        /// </summary>
        public bool Active
        {
            get
            {
                return active;
            }
            set
            {
                // propagate to children
                if (children != null)
                    for (int i = 0; i < children.Count; i++)
                        children[i].Active = value;

                // update active state in the comp manager
                if (value != active)
                    ComManager.SetActive(this, value);

                active = value;
            }
        }

        public EngineContext Context { get; internal set; }

        public TiledFloat4x4 GetTransform()
        {
            CompTransform transform = GetFirstAncestor<CompTransform>();
            if (transform != null)
                return transform.GetValue();

            return TiledFloat4x4.Identity;
        }
        
        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? base.ToString() : string.Format("{0} ({1})", Name, base.ToString());
        }

#region Access to other components

        internal ComponentManager ComManager { get; set; }

        /// <summary>
        /// Returns a list of components of the specified type, searching among all the active components.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected IReadOnlyList<T> GetComponents<T>() where T : IComponent
        {
            return ComManager.Query<T>();
        }

        protected T GetComponent<T>() where T : IComponent
        {
            return ComManager.QueryFirst<T>();
        }

        /// <summary>
        /// Returns the list of childrens components of this node that match the specified type.
        /// </summary>
        /// <param name="queryResult">
        /// An optional list used to save the results. 
        /// If null is passed, a new one is created. 
        /// Passing a valid list will avoid the creation of a new one each time this function is called, improving performances.
        /// </param>
        /// <returns></returns>
        public List<T> GetChildren<T>(List<T> queryResult = null) where T : Component
        {
            // prepare results list
            if (queryResult == null)
                queryResult = new List<T>();
            else
                queryResult.Clear();

            // fill the list wiht 
            if (children != null)
            {
                foreach (Component c in children)
                {
                    T match = c as T;
                    if (match != null)
                        queryResult.Add(match);
                }
            }

            return queryResult;
        }

        /// <summary>
        /// Returns the first direct (non-recursive) child of this component of the speficied type.
        /// Returns null if no component of the specified type is found.
        /// </summary>
        public T GetFirstChild<T>() where T : Component
        {
            if (children != null)
            {
                foreach (Component c in children)
                {
                    T match = c as T;
                    if (match != null)
                        return match;
                }
            }

            return null;
        }

        /// <summary>
        /// Walk up the parent components until one of the specified type is found
        /// </summary>
        /// <returns>The ancestor component of the specified type, or null if none is found.</returns>
        public T GetFirstAncestor<T>() where T : Component
        {
            for (Component ancestor = Parent; ancestor != null; ancestor = ancestor.Parent)
            {
                if (ancestor is T reqTypeAncestor)
                    return reqTypeAncestor;
            }

            return null;
        }

#endregion

#region IEquatable<Component>

        public override bool Equals(object obj)
        {
            Component other = obj as Component;
            return other != null && other.ID == ID;
        }

        public override int GetHashCode()
        {
            return ID;
        }

        public bool Equals(Component other)
        {
            return other != null && other.ID == ID;
        }

#endregion

#region Tracing

        protected void StartTracedSection(Byte4 markerColor, string name)
        {
#if TRACING && !SKIP_USER_EVENTS
            if (Context.Scene.Initialized)
                Context.Scene.Graphics.StartTracedSection(markerColor, name);
#endif
        }

        protected void EndTracedSection()
        {
#if TRACING && !SKIP_USER_EVENTS
            if (Context.Scene.Initialized)
                Context.Scene.Graphics.EndTracedSection();
#endif
        }

#endregion

        public virtual bool ValueChanged { get { return false; } }

        public bool IsBeingDisposed { get; private set; }

        public bool Disposed { get; private set; }

        public void Dispose()
        {
            if (!IsBeingDisposed)
            {
                DisposeChildren();
                ComManager.QueueDisposal(this);
                IsBeingDisposed = true;
            }
        }

        public void DisposeChildren()
        {
            if(children != null)
                for (int i = children.Count - 1; i >= 0; i--)
                    children[i].Dispose();
        }

        protected internal virtual void OnDispose()
        {
            ICompPausable cp = this as ICompPausable;
            if (cp != null)
                cp.Pause();
            ICompAllocator ca = this as ICompAllocator;
            if (ca != null)
                ca.ReleaseGraphicResources();

            ComManager.Remove(this);
            Context = null;
            if (!IsRoot)
                parent.children.Remove(this);
            parent = null;
            Disposed = true;
        }
    }

    public abstract class Component<OutT> : Component, IComponent<OutT>
    {
        private int lastUpdateID;
        private OutT cachedValue, prevValue;
        private object valueLock;
        private readonly string EVENTNAME_GETVALUE;

        public Component(Component owner) : base(owner)
        {
            valueLock = new object();
#if TRACING
            EVENTNAME_GETVALUE = GetType().Name.Replace('`', '-') + ".getValue()";
#else
            EVENTNAME_GETVALUE = GetType().Name; // gc optimized
#endif
        }

        /// <summary>
        /// Gets a value for this component. 
        /// Other components can the expose properties of type Component<OutT> the can be assigned with another component.
        /// </summary>
        /// <returns></returns>
        public OutT GetValue()
        {
            lock (valueLock)
            {
                if (lastUpdateID != ComManager.UpdateID)
                {
                    StartTracedSection(Color.White, EVENTNAME_GETVALUE);
                    prevValue = cachedValue;
                    cachedValue = getValue();
                    lastUpdateID = ComManager.UpdateID; 
                    EndTracedSection();
                }
            }
          
            return cachedValue;
        }

        /// <summary>
        /// Returns true if the value of this component changed (compared to the value as it was in the previous frame).
        /// </summary>
        public override bool ValueChanged
        {
            get
            {
                OutT curValue = GetValue();
                return !EqualityComparer<OutT>.Default.Equals(curValue, prevValue);
            }
        }

        protected abstract OutT getValue();

        protected internal override void OnDispose()
        {
            cachedValue = default(OutT);
            prevValue = default(OutT);
            base.OnDispose();
        }
    }

}
