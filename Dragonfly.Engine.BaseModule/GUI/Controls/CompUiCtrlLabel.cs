using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;

namespace Dragonfly.BaseModule
{
    public class CompUiCtrlLabel : CompUiControl
    {
        public CompUiCtrlLabel(CompUiContainer container, string text, UiCoords position) : base(container, position, "100 100")
        {
            Text = new MutableString(text, (str) => Container.Invalidate(this));
            Font = FontParams.GetDefaultLight(Ui);
            Font.FontChanged += OnFontChanged;
            OnFontChanged();
        }

        private void OnFontChanged()
        {
            Size = new UiSize(Size.Width, Font.Size);
            Container.Invalidate(this);
        }

        public CompUiCtrlLabel(CompUiContainer container, string text) : this(container, text, UiCoords.Zero) { }

        public MutableString Text{ get; private set; }
        
        public FontParams Font { get; private set; }

        public override void UpdateControl(IUiControlUpdateArgs args)
        {
            args.AddText(Text.CharArray, Text.Length, Font.Color, Font.Size, Position, Font.FontFace);
        }
    }

}
