using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Dragonfly.BaseModule
{
    public class CompUiContainer : Component, IUiCanvas, ICompResizable, ICompUpdatable
    {
        private CompTransformStack toParentTransform;
        private bool updateNeeded;
        private string skinName;
        private CompMesh skinMesh;
        private CompMtlBasic skinMaterial;
        private TextSpriteIndex fontIndex;
        private CompMesh textMesh;
        private CompMtlText textMaterial;
        private uint zIndex;
        CompTaskScheduler.ITask geomUpdateTask;
        UiControlUpdateArgs geomUpdateArgs;
        private List<CompUiControl> controlsCache; // list of children controls in the last search (just for GC optimization)

        public CompUiContainer(Component parent, IUiCanvas canvas, UiSize size, UiCoords position, PositionOrigin positionPivot) : base(parent)
        {
            BaseMod baseMod = Context.GetModule<BaseMod>();

            Coords = new CoordContext(this, baseMod.Settings.UI.FontPixSize);
            FontPackageName = "arial";
            SkinName = "base";
            ParentCanvas = canvas;
            PositionPivot = positionPivot;
            Size = new CompValue<UiSize>(this, size);
            Position = new CompValue<UiCoords>(this, position);

            // use a transform node as the list of controls (so that they're affected by the transform)
            toParentTransform = new CompTransformStack(this);
            toParentTransform.Push(new CompFunction<Float4x4>(this, () => CalcLocalToParentTransform()));
            Controls = toParentTransform;
            controlsCache = new List<CompUiControl>();

            // text renderering
            fontIndex = new TextSpriteIndex();
            string fontFolderPath = Context.GetResourcePath(Path.Combine("fonts", FontPackageName));
            AsyncFileLoader.LoadAllFiles(Directory.GetFiles(fontFolderPath, "*.fnt"), new TextFileHandler(OnFontIndexLoaded), false);
            textMesh = CompUiControl.CreateMesh(Controls);
            textMesh.Active = false; // skip draw until text is available
            textMaterial = new CompMtlText(this, Path.Combine(fontFolderPath, FontPackageName + ".dds"));
            textMaterial.Class.Add(MaterialClass);
            textMesh.Materials.Add(textMaterial);

            // sking rendering
            skinMesh = CompUiControl.CreateMesh(Controls);
            skinMaterial = new CompMtlBasic(skinMesh, SkinAtlas);
            skinMaterial.Class.Clear();
            skinMaterial.Class.Add(MaterialClass);
            skinMaterial.CullMode = Graphics.CullMode.None;
            skinMesh.Materials.Add(skinMaterial);

            // compute the next free z-index
            {
                zIndex = 0;
                foreach (CompUiContainer c in GetComponents<CompUiContainer>())
                {
                    if (c.ParentCanvas != canvas)
                        continue;
                    zIndex = Math.Max(zIndex, c.ZIndex + 1);
                }
                ZIndex = zIndex; // updates skin and other meshes render orders
            }

            // focus events
            HasFocus = new CompEvent(this, () => ZIndex == 0);
            FocusChanged = new CompEvent(HasFocus, () => HasFocus.ValueChanged);

            // update task
            geomUpdateTask = GetComponent<CompTaskScheduler>().CreateTask(Name + "_GeomUpdate", AsyncUpdateTask);
            geomUpdateArgs = new UiControlUpdateArgs(this);

            updateNeeded = true;
        }

        private void OnFontIndexLoaded(string loadedText)
        {
            lock (fontIndex)
            {
                fontIndex.AddToIndex(loadedText);
                updateNeeded = true;
            }
        }

        public TextRenderMode TextRenderMode
        {
            get { return textMaterial.RenderMode; }
            set { textMaterial.RenderMode.Value = value; }
        }

        public uint ZIndex
        {
            get
            {
                return zIndex;
            }
            set
            {
                zIndex = value;
                textMaterial.RenderOrder = UiZIndex.ToTextRenderOrder(ZIndex);
                skinMaterial.RenderOrder = UiZIndex.ToWndSkinRenderOrder(ZIndex);

                // update controls
                foreach (CompUiControl ctrl in Controls.GetChildren<CompUiControl>(controlsCache))
                    ctrl.OnZIndexChanged();
            }
        }

        public CompEvent HasFocus { get; private set; }

        public CompEvent FocusChanged { get; private set; }

        /// <summary>
        /// Return the container area in screen-space coordinates relative to the parent canvas.
        /// </summary>
        public AARect GetScreenArea()
        {
            return AARect.Bounding(new Float2(-1, 1), new Float2(1, -1)) * ToParent;
        }

        public UpdateType NeededUpdates
        {
            get { return (updateNeeded || Size.ValueChanged || geomUpdateTask.State == CompTaskScheduler.TaskState.Completed) ? UpdateType.FrameStart1 : UpdateType.None; }
        }

        public CompValue<UiSize> Size { get; private set; }

        public CompValue<UiCoords> Position { get; private set; }

        public PositionOrigin PositionPivot { get; set; }

        public IUiCanvas ParentCanvas { get; set; }

        public CoordContext Coords { get; private set; }

        public Component Controls { get; private set; }

        public string FontPackageName { get; set; }

        /// <summary>
        /// Texture Atlas used by the child controls that contains all the graphic elements for the UI.
        /// </summary>
        public CompTextureRef SkinAtlas { get; private set; }

        public string SkinName
        {
            get
            {
                return skinName;
            }
            set
            {
                if (SkinAtlas == null)
                    SkinAtlas = new CompTextureRef(this);
                SkinAtlas.SetSource(Path.Combine("textures", "ui", string.Format("ui_{0}_skin.dds", value)));
                skinName = value;
            }
        }

        /// <summary>
        /// Return the size of this container in pixels
        /// </summary>
        public Int2 PixelSize { get { return (Int2)Size.GetValue().ConvertTo(UiUnit.Pixels, ParentCanvas.Coords).XY; } }

        public void ScreenResized(int width, int height)
        {
            updateNeeded = true;
        }

        public void Update(UpdateType updateType)
        {
            if (!textMaterial.FontTexture.Available || fontIndex.Faces.Count == 0)
                return; // delay ui updates until text can be rendered
      
            if (updateNeeded && geomUpdateTask.State == CompTaskScheduler.TaskState.Idle) // start a new update if required
            {
                updateNeeded = false;

                // prepare a new update section
                geomUpdateArgs.Reset(this);

                // update container and controls
                UpdateContainerGeometry(geomUpdateArgs);
                foreach (CompUiControl ctrl in Controls.GetChildren<CompUiControl>(controlsCache))
                    if (ctrl.Visible) ctrl.UpdateControl(geomUpdateArgs);

                // update geometry asynchronously
                geomUpdateTask.QueueExecution();
            }
            else if (geomUpdateTask.State == CompTaskScheduler.TaskState.Completed) // sumbit the updated resources if the task completed
            {
                // completed, commit geometry update
                geomUpdateArgs.UpdateGeometry();
                textMesh.Active = true;
                geomUpdateTask.Reset();
            }
        }

        public void AsyncUpdateTask()
        {
            geomUpdateArgs.ProcessTextGeometry();
        }

        protected virtual void UpdateContainerGeometry(IUiControlUpdateArgs args) { }

        protected Float4x4 CalcLocalToParentTransform()
        {
            Float4x4 toParentTransform = Float4x4.Identity;

            // scale to parent area
            {
                Float2 size = PixelSize;
                Float2 containerSize = ParentCanvas.PixelSize;
                toParentTransform *= Float4x4.Scale((size / containerSize).ToFloat3(1));
            }

            // translate to the specified position
            {
                CoordContext.Push(ParentCanvas.Coords);
                UiCoords pos = Position.GetValue();
                if (PositionPivot == PositionOrigin.TopLeft)
                    pos = pos + Size.GetValue() * 0.5f;

                Float3 translation = Float3.Zero;
                translation.XY = pos.ConvertTo(UiUnit.ScreenSpace, ParentCanvas.Coords).XY;
                toParentTransform *= Float4x4.Translation(translation);
                CoordContext.Pop();
            }

            return toParentTransform;
        }

        public Float4x4 ToParent
        {
            get
            {
                return toParentTransform.GetValue().ToFloat4x4();
            }
        }

        public string MaterialClass { get { return ParentCanvas.MaterialClass; } }

        /// <summary>
        /// Signal this container that something in th UI state has changed and causes an update.
        /// </summary>
        public void Invalidate(CompUiControl sender)
        {
            updateNeeded = true;
        }

        struct TextGeometryArgs
        {
            public string Text;
            public char[] CharText;
            public int CharTextLen;
            public Float3 TextColor;
            public UiHeight FontSize;
            public UiCoords Position;
            public string FontFace;
        }

        private void AddTextGeometry(IObject3D texGeom, TextGeometryArgs args)
        {
            CoordContext.Push(Coords);

            // select font and size
            FaceSizeInfo curFont;
            float fontScale;
            {
                FaceIndex face;
                lock (fontIndex)
                {
                    if (!fontIndex.Faces.TryGetValue(args.FontFace, out face))
                        face = fontIndex.DefaultFace;
                }
                float fontSizePix = args.FontSize.ConvertTo(UiUnit.Pixels).Value;
                curFont = face.GetCloserSize(fontSizePix);
                fontScale = fontSizePix / curFont.Size;
            }
          
            // compute screen space position   
            UiCoords charStartPos;
            {
                UiCoords labelStartPos = args.Position.ConvertTo(UiUnit.Pixels);

                // snap position to pixel (to avoid blurriness)
                labelStartPos.XY = labelStartPos.XY.Round();
                charStartPos = labelStartPos;
            }

            // iterate over each character of the string and generate its geometry
            char prevChar = (char)0;
            int textLen = args.CharText != null ? args.CharTextLen : args.Text.Length;
            FaceCharInfo missingChar = curFont.Chars['?']; // default placeholder for missing chars in the font
            for (int i = 0; i < textLen; i++)
            {
                char c = args.CharText != null ? args.CharText[i] : args.Text[i];
                FaceCharInfo ci;
                if (!curFont.Chars.TryGetValue(c, out ci))
                    ci = missingChar;

                Float2 charStartPix = new Float2(ci.X + curFont.XOffset, ci.Y + curFont.YOffset);
                UiSize charPixSize = new UiSize(ci.Width, ci.Height, UiUnit.Pixels);

                // retrieve kerning
                int kerning = ci.GetKerningFor(prevChar);

                // quad vertices
                UiCoords charPos = charStartPos + new UiSize(ci.XOffset + kerning, ci.YOffset, UiUnit.Pixels) * fontScale;

                int charIndex = texGeom.VertexCount;
                texGeom.AddVertex(charPos.ConvertTo(UiUnit.ScreenSpace).XY.ToFloat3(0));
                texGeom.AddVertex((charPos + charPixSize.Width * fontScale).ConvertTo(UiUnit.ScreenSpace).XY.ToFloat3(0));
                texGeom.AddVertex((charPos + charPixSize * fontScale).ConvertTo(UiUnit.ScreenSpace).XY.ToFloat3(0));
                texGeom.AddVertex((charPos + charPixSize.Height * fontScale).ConvertTo(UiUnit.ScreenSpace).XY.ToFloat3(0));

                // tex coords
                texGeom.AddTexCoord(charStartPix);
                texGeom.AddTexCoord(charStartPix + new Float2(charPixSize.Width.Value, 0));
                texGeom.AddTexCoord(charStartPix + charPixSize.XY);
                texGeom.AddTexCoord(charStartPix + new Float2(0, charPixSize.Height.Value));

                // indices
                texGeom.AddIndex((ushort)(charIndex + 0));
                texGeom.AddIndex((ushort)(charIndex + 1));
                texGeom.AddIndex((ushort)(charIndex + 2));

                texGeom.AddIndex((ushort)(charIndex + 0));
                texGeom.AddIndex((ushort)(charIndex + 2));
                texGeom.AddIndex((ushort)(charIndex + 3));

                // color (save in normal slot)
                texGeom.AddNormal(args.TextColor);
                texGeom.AddNormal(args.TextColor);
                texGeom.AddNormal(args.TextColor);
                texGeom.AddNormal(args.TextColor);

                // update start position for the next character
                charStartPos = charStartPos + new UiWidth(ci.XAdvance + kerning, UiUnit.Pixels) * fontScale;
                prevChar = c;
            }

            CoordContext.Pop();
        }

        private class UiControlUpdateArgs : IUiControlUpdateArgs
        {
            private CompUiContainer container;
            private IObject3D textGeometry;
            private List<TextGeometryArgs> addedTextList;

            public UiControlUpdateArgs(CompUiContainer container)
            {
                addedTextList = new List<TextGeometryArgs>();
                Reset(container);
            }

            public IObject3D SkinGeometry { get; private set; }

            public void Reset(CompUiContainer container)
            {
                this.container = container;
                SkinGeometry = container.skinMesh.AsObject3D();
                SkinGeometry.ClearGeometry();
                textGeometry = container.textMesh.AsObject3D();
                textGeometry.ClearGeometry();
                addedTextList.Clear();
            }

            public void AddText(string text, Float3 textColor, UiHeight fontSize, UiCoords position, string fontFace)
            {
                addedTextList.Add(new TextGeometryArgs() { Text = text, TextColor = textColor, FontSize = fontSize, Position = position, FontFace = fontFace });
            }

            public void AddText(char[] text, int textLen, Float3 textColor, UiHeight fontSize, UiCoords position, string fontFace)
            {
                addedTextList.Add(new TextGeometryArgs() { CharText = text, CharTextLen = textLen, TextColor = textColor, FontSize = fontSize, Position = position, FontFace = fontFace });
            }

            public void ProcessTextGeometry()
            {
                foreach (TextGeometryArgs t in addedTextList)
                {
                    container.AddTextGeometry(textGeometry, t);
                }
            }

            public UiWidth MeasureText(string text, UiHeight fontSize, string fontFace)
            {
                Object3D textBuffer = new Object3D();
                container.AddTextGeometry(textBuffer, new TextGeometryArgs() { Text = text, FontSize = fontSize, Position = "0ss 0ss", FontFace = fontFace });
                UiWidth textWidth = "0ss";
                foreach(Float3 vertex in textBuffer.Vertices)
                    textWidth.Value = Math.Max(textWidth.Value, vertex.X);
                return textWidth;
            }

            public void UpdateGeometry()
            {
                textGeometry.UpdateGeometry();
                SkinGeometry.UpdateGeometry();
            }
        }


    }

}
