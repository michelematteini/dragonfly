using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    public class CompTransformStack : CompTransform
    {
        private enum TransformType
        {
            Static,
            Translation,
            RotationY,
            DirRotation,
            Scale,
            Scale3D,
            Dynamic
        }

        private struct TransformRecord
        {
            public TransformType Type;
            public Float4x4 ParamMatrix_1;
            public Component<Float3> ParamCFloat3_1;
            public Component<float> ParamCFloat_1;
            public float ParamFloat_1;
            public Float3 ParamFloat3_1, ParamFloat3_2;
            public Component<Float4x4> ParamCMatrix_1;
        }

        private List<TransformRecord> trList;
        private Int3 worldTile;

        public CompTransformStack(Component parent) : base(parent)
        {
            trList = new List<TransformRecord>();
        }

        public CompTransformStack(Component parent, Float4x4 value) : this(parent)
        {
            Push(value);
        }

        public static CompTransformStack FromLookAt(Component parent, Float3 position, Float3 target)
        {
            return new CompTransformStack(parent, Float4x4.LookAt(position, target - position, Float3.UnitY));
        }

        public static CompTransformStack FromPosition(Component parent, Float3 position)
        {
            return new CompTransformStack(parent, Float4x4.Translation(position));
        }

        public static CompTransformStack FromDirection(Component parent, Float3 direction)
        {
            return new CompTransformStack(parent, Float4x4.Rotation(Float3.UnitZ, direction));
        }

        public static CompTransformStack FromPosAndDir(Component parent, Float3 position, Float3 direction)
        {
            return new CompTransformStack(parent, Float4x4.Rotation(Float3.UnitZ, direction) * Float4x4.Translation(position));
        }

        public void PushTranslation(Component<Float3> value)
        {
            TransformRecord t = new TransformRecord();
            t.Type = TransformType.Translation;
            t.ParamCFloat3_1 = value;
            trList.Add(t);
        }

        public void PushRotationY(Component<float> value, float multiplier)
        {
            TransformRecord t = new TransformRecord();
            t.Type = TransformType.RotationY;
            t.ParamCFloat_1 = value;
            t.ParamFloat_1 = multiplier;
            trList.Add(t);
        }

        public void PushDirectionalRotation(Float3 fromDirection, Component<Float3> toDirection, Float3 upDirection)
        {
            TransformRecord t = new TransformRecord();
            t.Type = TransformType.DirRotation;
            t.ParamFloat3_1 = fromDirection;
            t.ParamCFloat3_1 = toDirection;
            t.ParamFloat3_2 = upDirection;
            trList.Add(t);
        }

        public void PushScale(Component<float> scale)
        {
            TransformRecord t = new TransformRecord();
            t.Type = TransformType.Scale;
            t.ParamCFloat_1 = scale;
            trList.Add(t);
        }

        public void PushScale(Component<Float3> scale)
        {
            TransformRecord t = new TransformRecord();
            t.Type = TransformType.Scale3D;
            t.ParamCFloat3_1 = scale;
            trList.Add(t);
        }

        public void Push(Float4x4 value)
        {
            TransformRecord t = new TransformRecord();
            t.Type = TransformType.Static;
            t.ParamMatrix_1 = value;
            trList.Add(t);
        }

        public void Push(Component<Float4x4> value)
        {
            TransformRecord t = new TransformRecord();
            t.Type = TransformType.Dynamic;
            t.ParamCMatrix_1 = value;
            trList.Add(t);
        }

        public void Clear()
        {
            trList.Clear();
            worldTile = Int3.Zero;
        }

        public void Set(Float4x4 value)
        {
            Clear();
            Push(value);
        }

        public void Set(TiledFloat4x4 value)
        {
            Clear();
            Push(value.Value);
            worldTile = value.Tile;
        }

        public override TiledFloat4x4 GetLocalTransform()
        {
            Float4x4 m = Float4x4.Identity;

            for (int i = 0; i < trList.Count; i++)
            {
                TransformRecord t = trList[i];
                switch (trList[i].Type)
                {
                    case TransformType.Static:
                        m *= t.ParamMatrix_1;
                        break;
                    case TransformType.Translation:
                        m *= Float4x4.Translation(t.ParamCFloat3_1.GetValue());
                        break;

                    case TransformType.RotationY:
                        m *= Float4x4.RotationY(t.ParamCFloat_1.GetValue() * t.ParamFloat_1);
                        break;

                    case TransformType.DirRotation:
                        m *= Float4x4.Rotation(t.ParamFloat3_1, t.ParamCFloat3_1.GetValue(), t.ParamFloat3_2);
                        break;

                    case TransformType.Dynamic:
                        m *= t.ParamCMatrix_1.GetValue();
                        break;

                    case TransformType.Scale:
                        m *= Float4x4.Scale(t.ParamCFloat_1.GetValue());
                        break;

                    case TransformType.Scale3D:
                        m *= Float4x4.Scale(t.ParamCFloat3_1.GetValue());
                        break;
                }
            }

            return new TiledFloat4x4() { Value = m, Tile = worldTile };
        }
    }
}
