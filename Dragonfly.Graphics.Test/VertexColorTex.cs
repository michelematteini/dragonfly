using Dragonfly.Graphics.Math;

public struct VertexColorTex
{
    public Float3 Position;
    public Float3 Color;
    public Float2 TexCoords;

    public VertexColorTex(Float3 position, Float3 color) : this(position, color, Float2.Zero) { }

    public VertexColorTex(Float3 position, Float3 color, Float2 texCoords)
    {
        this.Position = position;
        this.Color = color;
        this.TexCoords = texCoords;
    }

}