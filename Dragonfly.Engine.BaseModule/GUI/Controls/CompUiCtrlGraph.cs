using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System;

namespace Dragonfly.BaseModule
{
    public class CompUiCtrlGraph : CompUiControl
    {
        public const int MaxDataPoints = 32;

        private CompMtlGraph graphMaterial;
        private int dataPointCount;

        public CompUiCtrlGraph(CompUiContainer parent, UiCoords position, UiSize size) : base(parent, position, size)
        {
            Data = new Float4[MaxDataPoints];
            graphMaterial = new CompMtlGraph(this);
            CompMesh graphMesh = AddCustomMesh(graphMaterial);
            CustomMeshTransform.Push(new CompFunction<Float4x4>(CustomMeshTransform, GetLocalToParentTransform));
            Primitives.ScreenQuad(graphMesh.AsObject3D());
            AutoRange = true;
            Color1 = Color.Green.ToFloat4();
            Color2 = Color.Red.ToFloat4();
            Color3 = Color.Blue.ToFloat4();
            Width1 = Width2 = Width3 = 1.0f;
            BackgroundColor = Color.TransparentBlack.ToFloat4();
            PaddingPercent = new Float2(0.05f, 0.05f);
            RangePaddingPercent = 0.1f;
            TracesAlpha = new Float3(1.0f, 0, 0);
            FillAlpha = new Float3(0.4f, 0, 0);
        }

        public CompUiCtrlGraph(CompUiContainer parent, UiCoords position) : this(parent, position, "12em 8em") { }

        #region Graph Data

        public Float4[] Data { get; private set; }

        public int DataPointCount
        {
            get
            {
                return dataPointCount;
            }
            set
            {
                dataPointCount = value < 0 ? 0 : (value > MaxDataPoints ? MaxDataPoints : value);
            }
        }

        public void ClearData()
        {
            DataPointCount = 0;
        }

        public void UpdateDataRanges()
        {
            DisplayedRange = new AARect(Data[0].XY, 0, 0);

            for (int i = 1; i < DataPointCount; i++)
            {
                DisplayedRange = DisplayedRange.Add(Data[i].XY);
                if (TracesAlpha.Y > 0 || FillAlpha.Y > 0)
                    DisplayedRange = DisplayedRange.Add(Data[i].XZ);
                if (TracesAlpha.Z > 0 || FillAlpha.Z > 0)
                    DisplayedRange = DisplayedRange.Add(Data[i].XW);
            }
        }

        /// <summary>
        /// Discards the number of specified points, moving the graph to the left
        /// </summary>
        public void ShiftDataLeft(int pointCount)
        {
            Array.Copy(Data, pointCount, Data, 0, Data.Length - pointCount);
            DataPointCount -= pointCount;

            if (AutoRange)
            {
                UpdateDataRanges();
            }
        }

        public void AddDataPoint(float x, float y1, float y2 = 0, float y3 = 0)
        {
            if (DataPointCount == MaxDataPoints)
            {
                ShiftDataLeft(1);
            }

            Float4 pnt = new Float4(x, y1, y2, y3);

            if (AutoRange)
            {
                UpdateDataRanges();
            }

            Data[DataPointCount] = pnt;
            Invalidate();
            DataPointCount++;
        }

        #endregion

        #region Apparence Settings

        public bool AutoRange { get; set; }

        public AARect DisplayedRange { get; set; }

        /// <summary>
        /// Padding left empty around the graph control area.
        /// </summary>
        public Float2 PaddingPercent { get; set; }

        /// <summary>
        /// Vertical range added as a percent of DisplayRange
        /// </summary>
        public float RangePaddingPercent { get; set; }

        public Float2 YRange { get; set; }

        public Float4 Color1 { get; set; }

        public Float4 Color2 { get; set; }

        public Float4 Color3 { get; set; }

        public Float4 BackgroundColor { get; set; }

        public float Width1 { get; set; }
        
        public float Width2 { get; set; }

        public float Width3 { get; set; }

        public Float3 TracesAlpha { get; set; }

        public Float3 FillAlpha { get; set; }

        #endregion


        public override void UpdateControl(IUiControlUpdateArgs args)
        {
            
        }

        private class CompMtlGraph : CompMaterial
        {
            private CompUiCtrlGraph graph;
            
            public CompMtlGraph(CompUiCtrlGraph parent) : base(parent)
            {
                graph = parent;
                BlendMode = BlendMode.AlphaBlend;
                CullMode = Graphics.CullMode.None;
                UpdateEachFrame = true;
            }

            public override string EffectName => "Graph";

            protected override void UpdateParams()
            {
                Shader.SetParam("data", graph.Data);

                Float2 size = graph.Size.ConvertTo(UiUnit.Pixels, graph.Container.Coords).XY;
                Shader.SetParam("graphSizePixels", size);
                Shader.SetParam("rangeMin", new Float2(graph.DisplayedRange.Min.X, graph.DisplayedRange.Min.Y - 0.5f * graph.RangePaddingPercent * graph.DisplayedRange.Size.Y));
                Shader.SetParam("rangeSize", new Float2(graph.DisplayedRange.Width, graph.DisplayedRange.Height * (1.0f + graph.RangePaddingPercent)));
                Shader.SetParam("dataPointCount", graph.DataPointCount);
                Shader.SetParam("color1", graph.Color1);
                Shader.SetParam("color2", graph.Color2);
                Shader.SetParam("color3", graph.Color3);
                Shader.SetParam("tracesWidth", new Float3(graph.Width1, graph.Width2, graph.Width3));
                Shader.SetParam("backgroundColor", graph.BackgroundColor);
                Shader.SetParam("paddingPercent", graph.PaddingPercent);
                Shader.SetParam("traceAlpha", graph.TracesAlpha);
                Shader.SetParam("fillAlpha", graph.FillAlpha);
            }
        }
    }
}
