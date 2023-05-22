using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A material that fill the mesh with a specified color, ignoring lighting.
    /// <para/> Vertices can be specified both in local space or in screen space, ignoring tranformations.
    /// <para/> An additional texture with alpha channel can be specified to mask out part of the geometry, making them transparent.
    /// </summary>
    public class CompMtlMasking : CompMaterial
    {
        public CompMtlMasking(Component owner) : this(owner, new FRandom().NextSatColor()) { }

        public CompMtlMasking(Component owner, Float3 color) : base(owner)
        {
            Color = MakeParam(color);
            ShadingEnabled = MakeParam(true);
            TextureCoords = new MtlModTextureCoords(this);
            Displacement = new MtlModDisplacement(this, null);
            CullMode = Graphics.CullMode.None;
            ScreenSpaceAlphaMask = MakeParam(false);
            AlphaMasking = new MtlModAlphaMasking(this, new CompTextureRef(owner, Graphics.Math.Color.Black));
        }

        public Param<Float3> Color { get; private set; }

        public Param<bool> ShadingEnabled { get; private set; }

        public Param<bool> ScreenSpaceAlphaMask { get; private set; }

        public MtlModAlphaMasking AlphaMasking { get; private set; }

        public override string EffectName
        {
            get
            {
                return "MaskingMaterial";
            }
        }

        /// <summary>
        /// Texture coords modifiers.
        /// </summary>
        public MtlModTextureCoords TextureCoords { get; private set; }

        public MtlModDisplacement Displacement { get; private set; }

        protected override void UpdateParams()
        {
            Shader.SetParam("diffuse_color", Color);
            Shader.SetParam("shading_ammount", ShadingEnabled ? 1.0f : 0.0f);
            Shader.SetParam("mask_screen_space", ScreenSpaceAlphaMask);
            Shader.SetParam("alphaMask", AlphaMasking.Map);
        }

        public class Factory : MaterialFactory
        {
            protected override CompMaterial CreateMaterialFromDescr(MaterialDescription matDescr, Component parent)
            {
                return new CompMtlMasking(parent);
            }
        }

    }
}
