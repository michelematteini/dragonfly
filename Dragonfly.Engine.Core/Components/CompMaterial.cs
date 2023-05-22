using Dragonfly.Graphics;
using Dragonfly.Graphics.Resources;
using Dragonfly.Utils;
using System;
using System.Collections.Generic;

namespace Dragonfly.Engine.Core
{
    public abstract class CompMaterial : Component, ICompAllocator, ICompUpdatable
    {
        private long renderOrder;
        private ShaderStates shaderStates;
        private bool paramsInvalidated;
        private string defaultTemplate;

        public CompMaterial(Component parent) : base(parent)
        {
            LoadingRequired = true;
            Ready = false;
            UsedBy = new List<CompDrawable>();
            ObservableSet<string> classes = new ObservableSet<string>();
            classes.Changed += () => ComManager.UpdateMaterialQueries(this);
            Class = classes;
            shaderStates = ShaderStates.Default;
            FillMode = FillMode.Solid;
            EffectVariants = new KeyValuePair<string, string>[0];
            Shaders = new Dictionary<int, Shader>();

            // monitored parameters setup
            ObservableList<Component> obsParams = new ObservableList<Component>();
            obsParams.ItemAdded += (Component obj) => paramsInvalidated = true;
            MonitoredParams = obsParams;
            paramsInvalidated = true;
            DefaultTemplate = "";
        }

        /// <summary>
        /// List of shader loaded for this material, indexed by the template name hash
        /// </summary>
        internal Dictionary<int, Shader> Shaders;

        /// <summary>
        /// Templates forced as disabled on this material. If set to null, all available templates are enabled
        /// </summary>
        internal HashSet<string> DisabledTemplates;

        /// <summary>
        /// Main shader resource for this material. Could be null while loading.
        /// </summary>
        protected internal Shader Shader { get; private set; }

        /// <summary>
        /// Enable or disable a particular shader template for this material
        /// </summary>
        public void SetTemplateEnabled(string templateName, bool enabled)
        {
            if(enabled)
            {
                if (DisabledTemplates == null)
                    return;
                if (DisabledTemplates.Contains(templateName))
                    DisabledTemplates.Remove(templateName);
            }
            else
            {
                if (DisabledTemplates == null)
                    DisabledTemplates = new HashSet<string>();
                DisabledTemplates.Add(templateName);
            }
        }

        public bool IsTemplateEnabled(string templateName)
        {
            return DisabledTemplates == null || !DisabledTemplates.Contains(templateName);
        }

        public bool IsTemplateAvailable(string templateName)
        {
            return Shaders.ContainsKey(templateName.GetHashCode());
        }

        internal bool IsTemplateAvailable(int templateNameHash)
        {
            return Shaders.ContainsKey(templateNameHash);
        }

        internal int DefaultTemplateHash { get; private set; }

        /// <summary>
        /// If a material implements more than one template, this property defines which one is used when the material is drawn in a pass that do not override it.
        /// </summary>
        public string DefaultTemplate
        {
            get { return defaultTemplate; }
            set
            {
                defaultTemplate = value;
                DefaultTemplateHash = value.GetHashCode();
            }
        }
        /// <summary>
        /// List of drawables that use this material.
        /// </summary>
        internal List<CompDrawable> UsedBy { get; private set; }

        /// <summary>
        /// Specify a list of tags exposed by this material, that are then used to decide in which pass is rendered.
        /// </summary>
        public ISet<string> Class { get; private set; }

        /// <summary>
        /// List of components that will be used as shader parameters.  
        /// <para/> A component added to this list will trigger a parameter update call automatically when its value changes.
        /// </summary>
        protected internal IList<Component> MonitoredParams { get; private set; }

        /// <summary>
        /// List of modules containing additional material parameters.
        /// </summary>
        internal IList<MaterialModule> Modules { get; private set; }

        internal void AddModule(MaterialModule m)
        {
            if (Modules == null)
                Modules = new List<MaterialModule>();
            Modules.Add(m);
        }

        /// <summary>
        /// Returns the specified module if available, or null otherwise.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetModule<T>() where T : MaterialModule
        {
            if (Modules == null)
                return null;

            foreach (MaterialModule m in Modules)
                if (m is T requiredModule)
                    return requiredModule;
            return null;
        }

        /// <summary>
        /// If set to true UpdateParams() will be called each frame, regardless of material parameters changes.
        /// </summary>
        protected bool UpdateEachFrame { get; set; }

        /// <summary>
        /// Force  a call to UpdateParams() on the next frame.
        /// </summary>
        protected void InvalidateParams()
        {
            paramsInvalidated = true;
        }

        public UpdateType NeededUpdates
        {
            get
            {
                if (LoadingRequired)
                    return UpdateType.None;

                if(paramsInvalidated || UpdateEachFrame)
                    return UpdateType.FrameStart1;

                // if any of the monitored components have changed, update the shader params
                for (int i = 0; i < MonitoredParams.Count; i++)
                {
                    if (MonitoredParams[i].ValueChanged)
                    {
                        paramsInvalidated = true;
                        return UpdateType.FrameStart1;
                    }
                }

                // nothing changed
                return UpdateType.None;
            }
        }

        public void Update(UpdateType updateType)
        {
            UpdateParamsInternal();
            paramsInvalidated = false;          
        }

        private void UpdateParamsInternal()
        {
            if(Modules != null)
            {
                for(int i = 0; i < Modules.Count; i++)
                    Modules[i].UpdateAdditionalParams(Shader);
            }

            UpdateParams();
        }

        protected abstract void UpdateParams();

        /// <summary>
        /// Setup the material class so that is displayed in the specified pass.
        /// </summary>
        /// <returns>Returns this material, for inline functional usage.</returns>
        public CompMaterial DisplayIn(CompRenderPass renderPass)
        {
            Class.Add(renderPass.MainClass);
            return this;
        }

        /// <summary>
        /// Setup the material class so that is only displayed in the specified pass.
        /// </summary>
        /// <returns>Returns this material, for inline functional usage.</returns>
        public CompMaterial DisplayOnlyIn(CompRenderPass renderPass)
        {
            Class.Clear();
            return DisplayIn(renderPass);
        }

        /// <summary>
        /// Add the specified class to this material.
        /// </summary>
        /// <returns>Returns this material, for inline functional usage.</returns>
        public CompMaterial OfClass(string matClass)
        {
            Class.Add(matClass);
            return this;
        }

        /// <summary>
        /// If set to a valid camera, this material will only be draw to the specified camera.
        /// NB: this is not an optimized path and should only be used on a limited number of materials, use material classes to organize material in render passes instead.
        /// </summary>
        public CompCamera VisibleOnlyForCamera { get; set; }

        /// <summary>
        /// A value indicating the drawing order of a material inside a pass.
        /// <para/> Lower values are drawn first.
        /// </summary>
        public long RenderOrder
        {
            get { return renderOrder; }
            set
            {
                renderOrder = value;
                ComManager.UpdateMaterialQueries(this);
            }
        }

        protected bool DepthBufferEnable
        {
            get
            {
                return shaderStates.DepthBufferEnable;
            }
            set
            {
                LoadingRequired |= shaderStates.DepthBufferEnable != value;
                shaderStates.DepthBufferEnable = value;
            }
        }

        protected bool DepthBufferWriteEnable
        {
            get
            {
                return shaderStates.DepthBufferWriteEnable;
            }
            set
            {
                LoadingRequired |= shaderStates.DepthBufferWriteEnable != value;
                shaderStates.DepthBufferWriteEnable = value;
            }
        }

        protected BlendMode BlendMode
        {
            get
            {
                return shaderStates.BlendMode;
            }
            set
            {
                LoadingRequired |= shaderStates.BlendMode != value;
                shaderStates.BlendMode = value;
            }
        }

        public FillMode FillMode
        {
            get
            {
                return shaderStates.FillMode;
            }
            set
            {
                LoadingRequired |= shaderStates.FillMode != value;
                shaderStates.FillMode = value;
            }
        }

        public CullMode CullMode
        {
            get
            {
                return shaderStates.CullMode;
            }
            set
            {
                LoadingRequired |= shaderStates.CullMode != value;
                shaderStates.CullMode = value;
            }
        }
        
        public abstract string EffectName { get; }

        public virtual KeyValuePair<string, string>[] EffectVariants { get; private set; }

        public void SetVariantValue(string name, string value)
        {
            int variantID = Array.FindIndex(EffectVariants, vstate => vstate.Key == name);
            if (variantID < 0)
            {
                KeyValuePair<string, string>[] variantStates = new KeyValuePair<string, string>[EffectVariants.Length + 1];
                Array.Copy(EffectVariants, variantStates, EffectVariants.Length);
                variantID = EffectVariants.Length;
                EffectVariants = variantStates;
            }
            EffectVariants[variantID] = new KeyValuePair<string, string>(name, value);
            LoadingRequired = true;
        }

        public void SetVariantValue(string name, bool value)
        {
            SetVariantValue(name, value ? "True" : "False");
        }


        public bool LoadingRequired { get; protected set; }

        public override bool Ready { get; protected set; }

        public virtual void LoadGraphicResources(EngineResourceAllocator g)
        {
            // release current shaders (loading could be required multiple times when some properties are changed).
            ReleaseGraphicResources();

            // create main shader
            Shader = g.CreateShader(EffectName, shaderStates, EffectVariants, DefaultTemplate);
            DefaultTemplate = Shader.TemplateName;
            Shaders.Add(Shader.TemplateName.GetHashCode(), Shader);

            // create additional templates
            foreach(string templateName in g.GetShaderTemplates(EffectName))
            {
                if (templateName == Shader.TemplateName)
                    continue; // skip main template

                Shader childShader = g.CreateShader(Shader, shaderStates, templateName);
                Shaders.Add(templateName.GetHashCode(), childShader);
            }

            // update params
            UpdateParamsInternal();
            LoadingRequired = false;
            Ready = true;
        }

        public virtual void ReleaseGraphicResources()
        {
            foreach (Shader s in Shaders.Values)
                s.Release();
            Shaders.Clear();
            Shader = null;

            LoadingRequired = true;
            Ready = false;
        }

        protected Param<T> MakeParam<T>(T value)
        {
            return new Param<T>(this, value);
        }

        /// <summary>
        /// A parameter to be used inside a CompMaterial. Automatically invalidates the parent material on changes
        /// </summary>
        public class Param<T>
        {
            private T value;
            private CompMaterial parent;

            internal Param(CompMaterial parent, T initialValue)
            {
                this.value = initialValue;
                this.parent = parent;
            }

            public T Value
            {
                get { return value; }
                set
                {
                    this.value = value;
                    parent.InvalidateParams();
                }
            }

            public static implicit operator T(Param<T> p)
            {
                return p.value;
            }

        }

    }
}
