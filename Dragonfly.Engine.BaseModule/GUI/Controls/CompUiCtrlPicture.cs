using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System;

namespace Dragonfly.BaseModule
{
    public class CompUiCtrlPicture : CompUiControl
    {
        public CompUiCtrlPicture(CompUiContainer parent, UiCoords position, CompMtlImage imgMaterial, Float2 topLeftCoords, Float2 bottomRightCoords) : base(parent, position, "100% 100%")
        {
            ImageMaterial = imgMaterial;
            ImageMesh = AddCustomMesh(imgMaterial);
            CustomMeshTransform.Push(new CompFunction<Float4x4>(CustomMeshTransform, CalcCurrentTransform));
            Primitives.ScreenQuad(ImageMesh.AsObject3D(), topLeftCoords, bottomRightCoords);
        }

        public CompUiCtrlPicture(CompUiContainer parent, UiCoords position, CompMtlImage material) : this(parent, position, material, Float2.Zero, Float2.One) { }

        public CompUiCtrlPicture(CompUiContainer parent, CompMtlImage material) : this(parent, "0", material, Float2.Zero, Float2.One) { }

        public CompUiCtrlPicture(CompUiContainer parent) : this(parent, "0", new CompMtlImgCopy(parent), Float2.Zero, Float2.One) { }

        public CompUiCtrlPicture(CompUiContainer parent, string imagePath, UiCoords position, Float2 topLeftCoords, Float2 bottomRightCoords) : this(parent, position, new CompMtlImgCopy(parent, true), topLeftCoords, bottomRightCoords) 
        {
            ImageMaterial.Image.SetSource(imagePath);
        }  

        public CompUiCtrlPicture(CompUiContainer parent, string imagePath) : this(parent, imagePath, "0", Float2.Zero, Float2.One) { }

        public CompUiCtrlPicture(CompUiContainer parent, string imagePath, UiCoords position) : this(parent, imagePath, position, Float2.Zero, Float2.One) { }

        public ImageSizingStyle SizingStyle { get; set; }

        public CompMesh ImageMesh { get; private set; }

        public CompMtlImage ImageMaterial { get; private set; }

        public CompTextureRef Image
        {
            get
            {
                return ImageMaterial.Image;
            }
        }

        private Float4x4 CalcCurrentTransform()
        {
            CoordContext.Push(Container.Coords);

            UiSize screenScale = Size;
            // resize to proportions if required
            if (ImageMaterial.Image.Available && SizingStyle != ImageSizingStyle.StretchToSize)
            {
                float imgAspect = (float)ImageMaterial.Image.Resolution.X / ImageMaterial.Image.Resolution.Y;

                switch (SizingStyle)
                {
                    case ImageSizingStyle.AutoHeight:
                        {
                            screenScale.Height = Size.Width.ToHeight() * (1 / imgAspect);
                        }
                        break;

                    case ImageSizingStyle.AutoWidth:
                        {
                            screenScale.Width = Size.Height.ToWidth() * imgAspect;
                        }
                        break;

                    case ImageSizingStyle.FillScreen:
                        {
                            screenScale = "100% 100%";
                            float screenAspect = Container.Coords.ScreenAspectRatio;       
                            if (screenAspect > imgAspect)
                                screenScale.Height = new UiHeight(screenAspect / imgAspect, UiUnit.Percent);
                            else
                                screenScale.Width = new UiWidth(imgAspect / screenAspect, UiUnit.Percent);
                        }
                        break;

                }
            }
            Float2 screenOffset = (Position + screenScale * 0.5f).ConvertTo(UiUnit.ScreenSpace).XY;

            CoordContext.Pop();
            
            return Float4x4.Scale(screenScale.ConvertTo(UiUnit.Percent, Container.Coords).XY.ToFloat3(1)) * Float4x4.Translation(screenOffset.X, screenOffset.Y, 0);
        }

        public override void UpdateControl(IUiControlUpdateArgs args)
        {

        }
    }

    public enum ImageSizingStyle
    {
        /// <summary>
        /// The image is stretched to match the specified Size
        /// </summary>
        StretchToSize = 0,
        /// <summary>
        /// The height is calculated to preserve the original image aspect ratio
        /// </summary>
        AutoHeight,
        /// <summary>
        /// The width is calculated to preserve the original image aspect ratio
        /// </summary>
        AutoWidth,
        /// <summary>
        /// The image is resized to completely cover the screen but preserving aspect ratio. The Size parameter is ignored.
        /// </summary>
        FillScreen
    }


}
