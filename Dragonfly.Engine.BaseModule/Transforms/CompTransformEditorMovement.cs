using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    public class CompTransformEditorMovement : CompTransform
    {
        public CompTransformKeyMovement Movement { get; private set; }

        public CompTransformMouseLook Look { get; private set; }

        public CompValue<Float3> UpVector { get; private set; }

        public CompTransformEditorMovement(Component owner, TiledFloat3 initialPosition, TiledFloat3 initialTarget, float smoothingSeconds = 0.5f) : base(owner)
        {
            UpVector = new CompValue<Float3>(this, Float3.UnitY);
            Movement = new CompTransformKeyMovement(this, initialPosition, UpVector, smoothingSeconds);
            Look = new CompTransformMouseLook(this, (initialTarget - initialPosition).ToFloat3().Normal(), UpVector, smoothingSeconds);
            Movement.Direction.Set(Look.Direction);
        }

        public override TiledFloat4x4 GetLocalTransform()
        {
            TiledFloat4x4 localTransform;
            localTransform.Value = Float4x4.Translation(-Movement.Position.GetValue().Value) * Look.GetLocalTransform().Value;
            localTransform.Tile = Movement.Position.GetValue().Tile;
            return localTransform;
        }
    }
}
