using Dragonfly.Graphics.Resources;
using Dragonfly.Graphics.Shaders;
using Dragonfly.Utils;
using System;

namespace Dragonfly.Graphics.API
{
    public class BlendModeField : ObservableRecord.Field<BlendMode>
    {
        public BlendModeField(ObservableRecord parent, BlendMode initialValue) : base(parent, initialValue)
        {
        }

        public override BlendMode Value
        {
            get
            {
                return InnerValue;
            }
            set
            {
                if (InnerValue != value)
                    InvalidateParent();
                InnerValue = value;
            }
        }

        public override int GetHashCode()
        {
            return (int)InnerValue;
        }
    }

    public class CullModeField : ObservableRecord.Field<CullMode>
    {
        public CullModeField(ObservableRecord parent, CullMode initialValue) : base(parent, initialValue)
        {
        }

        public override CullMode Value
        {
            get
            {
                return InnerValue;
            }
            set
            {
                if (InnerValue != value)
                    InvalidateParent();
                InnerValue = value;
            }
        }

        public override int GetHashCode()
        {
            return (int)InnerValue;
        }
    }

    public class FillModeField : ObservableRecord.Field<FillMode>
    {
        public FillModeField(ObservableRecord parent, FillMode initialValue) : base(parent, initialValue)
        {
        }

        public override FillMode Value
        {
            get
            {
                return InnerValue;
            }
            set
            {
                if (InnerValue != value)
                    InvalidateParent();
                InnerValue = value;
            }
        }

        public override int GetHashCode()
        {
            return (int)InnerValue;
        }
    }

    public class SurfaceFormatField : ObservableRecord.Field<SurfaceFormat>
    {
        public SurfaceFormatField(ObservableRecord parent, SurfaceFormat initialValue) : base(parent, initialValue)
        {
        }

        public override SurfaceFormat Value
        {
            get
            {
                return InnerValue;
            }
            set
            {
                if (InnerValue != value)
                    InvalidateParent();
                InnerValue = value;
            }
        }

        public override int GetHashCode()
        {
            return (int)InnerValue;
        }
    }

    public class VertexTypeField : ObservableRecord.Field<VertexType>
    {
        public VertexTypeField(ObservableRecord parent, VertexType initialValue) : base(parent, initialValue)
        {
        }

        public override VertexType Value
        {
            get
            {
                return InnerValue;
            }
            set
            {
                if (InnerValue != value)
                    InvalidateParent();
                InnerValue = value;
            }
        }
    }

    public class TextureBindingOptionsField : ObservableRecord.Field<TextureBindingOptions>
    {
        public TextureBindingOptionsField(ObservableRecord parent, TextureBindingOptions initialValue) : base(parent, initialValue)
        {
        }

        public override TextureBindingOptions Value
        {
            get
            {
                return InnerValue;
            }
            set
            {
                if (InnerValue != value)
                    InvalidateParent();
                InnerValue = value;
            }
        }

        public override int GetHashCode()
        {
            return (int)InnerValue;
        }
    }
    

}
