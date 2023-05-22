using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    public abstract class CompUiControl : Component, ICompPausable
    {
        private bool hasFocusInWindow;

        public static CompMesh CreateMesh(Component parent)
        {
            CompMesh ctrlMesh = new CompMesh(parent);
            ctrlMesh.IsBounded = false;
            ctrlMesh.CastShadows = false;
            return ctrlMesh;
        }

        private UiCoords position;
        private UiSize size;
        private bool visible;
        private uint customMeshZIndex;

        public CompUiControl(CompUiContainer parent, UiCoords position, UiSize size) : base(parent.Controls)
        {
            Container = parent;
            Position = position;
            Size = size;
            visible = true;
            Ui = Context.GetModule<BaseMod>().Settings.UI;
            CustomMeshTransform = new CompTransformStack(this);
            customMeshZIndex = 10;

            Clicked = new CompEventClickInArea(this, new CompFunction<AARect>(this, GetParentScreenArea), Container.Coords).Event;
            Clicked = new CompEventAnd(this, Clicked, Container.HasFocus).Event;
            new CompActionOnEvent(Clicked, SetFocus);
            HasFocus = new CompEvent(this, () => hasFocusInWindow);
            new CompActionOnEvent(Container.FocusChanged, OnContainerFocusChanged);
        }

        private void SetFocus()
        {
            if (hasFocusInWindow)
                return;

            // remove focus from the currently focused control
            foreach(CompUiControl ctrl in Parent.GetChildren<CompUiControl>())
            {
                ctrl.hasFocusInWindow = false;
            }

            // set focus to this control
            hasFocusInWindow = true;
        }

        private void OnContainerFocusChanged()
        {
            if (!Container.HasFocus.GetValue())
                ResetFocus();
        }

        private void ResetFocus()
        {
            if (!hasFocusInWindow)
                return;

            hasFocusInWindow = false;
        }

        public void Invalidate()
        {
            Container.Invalidate(this);
        }

        public CompEvent HasFocus { get; private set; }

        public CompEvent Clicked { get; private set; }

        public CompMesh AddCustomMesh(CompMaterial material)
        {
            CompMesh customMesh = CreateMesh(CustomMeshTransform);
            material.Class.Add(Container.MaterialClass);
            material.RenderOrder = UiZIndex.ToCustomMeshRenderOrder(Container.ZIndex, CustomMeshZIndex);
            customMesh.Materials.Add(material);
            return customMesh;
        }

        public uint CustomMeshZIndex
        {
            get { return customMeshZIndex; }
            set
            {
                customMeshZIndex = value;
                OnZIndexChanged();
            }
        }

        internal void OnZIndexChanged()
        {
            long customMeshRenderOrder = UiZIndex.ToCustomMeshRenderOrder(Container.ZIndex, CustomMeshZIndex);
            foreach (CompMesh customMesh in CustomMeshList)
                customMesh.GetFirstMaterialOfClass(Container.MaterialClass).RenderOrder = customMeshRenderOrder;
        }

        public CompTransformStack CustomMeshTransform { get; private set; }

        public CompUiContainer Container { get; private set; }

        public BaseModUiSettings Ui { get; private set; }

        public UiCoords Position
        {
            get { return position; }
            set
            {
                position = value;
                Invalidate();
            }
        }

        public UiSize Size
        {
            get { return size; }
            set
            {
                size = value;
                Invalidate();
            }
        }

        public UiWidth Width
        {
            get { return Size.Width; }
            set
            {
                Size = new UiSize(value, Size.Height);
            }
        }

        public UiHeight Height
        {
            get { return Size.Height; }
            set
            {
                Size = new UiSize(Size.Width, value);
            }
        }

        public abstract void UpdateControl(IUiControlUpdateArgs args);

        public void Pause()
        {
            Invalidate();
        }

        public void Resume()
        {
            Invalidate();
        }

        public bool Visible
        {
            get
            {
                return visible;
            }
            set
            {
                visible = value;
                foreach(CompMesh customMesh in CustomMeshList)
                    customMesh.Active = value;
            }
        }

        public List<CompMesh> CustomMeshList
        {
            get
            {
                return CustomMeshTransform.GetChildren<CompMesh>();
            }
        }

        /// <summary>
        /// Returns the top-left corner position in parent-space coords
        /// </summary>
        public Float2 TopLeft
        {
            get { return ToScreen(Position); }
        }

        /// <summary>
        /// Returns the bottom-right corner position in parent-space coords
        /// </summary>
        public Float2 BottomRight
        {
            get { return ToScreen(Position + Size); }
        }

        /// <summary>
        /// The center of this control as defined from its position and size.s
        /// </summary>
        public UiCoords Center
        {
            get
            {
                CoordContext.Push(Container.Coords);
                UiCoords center = Position + Size * 0.5f;
                CoordContext.Pop();
                return center;
            }
            set
            {
                CoordContext.Push(Container.Coords);
                Position = value - (Size * 0.5f);
                CoordContext.Pop();
            }
        }

        /// <summary>
        /// The size that separate this control visual center from the actual center. Used for alignment purposes
        /// </summary>
        public virtual UiSize AlignmentOffset
        {
            get
            {
                return new UiSize();
            }
        }

        /// <summary>
        /// Return the control area in screen-space coordinates relative to the parent of the owning container.
        /// </summary>
        public AARect GetParentScreenArea()
        {
            CoordContext.Push(Container.Coords);
            UiCoords BottomRightCorner = Position + Size;
            CoordContext.Pop();
            return AARect.Bounding(ToParentScreen(Position), ToParentScreen(BottomRightCorner));
        }

        /// <summary>
        /// Returns a matrix that transform geometry from control-space to parent-space taking current size and position into account. 
        /// </summary>
        protected Float4x4 GetLocalToParentTransform()
        {
            Float2 screenOffset = Center.ConvertTo(UiUnit.ScreenSpace, Container.Coords).XY;
            return Float4x4.Scale(Size.ConvertTo(UiUnit.Percent, Container.Coords).XY.ToFloat3(1)) * Float4x4.Translation(screenOffset.X, screenOffset.Y, 0);
        }


        #region Quick coordinate to screen-space conversions

        /// <summary>
        /// Convert the given coordinates to screen-space and return their value.
        /// </summary>
        public Float2 ToScreen(UiCoords pos)
        {
            return pos.ConvertTo(UiUnit.ScreenSpace, Container.Coords).XY;
        }

        /// <summary>
        /// Convert the give size to screen-space and return its value.
        /// </summary>
        public Float2 ToScreen(UiSize size)
        {
            return size.ConvertTo(UiUnit.ScreenSpace, Container.Coords).XY;
        }

        /// <summary>
        /// Convert the give size to screen-space and return its value.
        /// </summary>
        public float ToScreen(UiWidth size)
        {
            return size.ConvertTo(UiUnit.ScreenSpace, Container.Coords).Value;
        }

        /// <summary>
        /// Convert the give size to screen-space and return its value.
        /// </summary>
        public float ToScreen(UiHeight size)
        {
            return size.ConvertTo(UiUnit.ScreenSpace, Container.Coords).Value;
        }

        /// <summary>
        /// Convert the given coordinates to the parent screen-space and return their value.
        /// </summary>
        public Float2 ToParentScreen(UiCoords pos)
        {
            return ToScreen(pos) * Container.ToParent;
        }

        /// <summary>
        /// Convert the give size to the parent screen-space and return its value.
        /// </summary>
        public Float2 ToParentScreen(UiSize size)
        {
            return ToScreen(size) * Container.ToParent - Container.ToParent.Position.XY;
        }

        /// <summary>
        /// Convert the give size to the parent screen-space and return its value.
        /// </summary>
        public float ToParentScreen(UiWidth size)
        {
            return (ToScreen(size) * Float2.UnitX * Container.ToParent).X - Container.ToParent.Position.X;
        }

        /// <summary>
        /// Convert the give size to the parent screen-space and return its value.
        /// </summary>
        public float ToParentScreen(UiHeight size)
        {
            return (ToScreen(size) * Float2.UnitY * Container.ToParent).Y - Container.ToParent.Position.Y;
        }

        #endregion
    }

    public interface IUiControlUpdateArgs
    {
        IObject3D SkinGeometry { get; }

        void AddText(string text, Float3 textColor, UiHeight fontSize, UiCoords position, string fontFace);

        void AddText(char[] text, int textLen, Float3 textColor, UiHeight fontSize, UiCoords position, string fontFace);

        UiWidth MeasureText(string text, UiHeight fontSize, string fontFace);
    }

}
