using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using Dragonfly.Utils;
using System;
using System.Collections.Generic;

namespace Dragonfly.Graphics.API.Directx9
{   
	
	/// <summary>
	/// Used to record a set of commands that can be then played on the DX9 immediate context
	/// </summary>
	internal class Directx9CmdList
    {
        private const int MAX_RECORDABLE_INSTANCE_COUNT = 16384;

        /// <summary>
        /// ID if the command list resource.
        /// </summary>
        public GraphicResourceID ResourceID;

        /// <summary>
        /// List of commands recorded on this list.
        /// </summary>
		public List<Directx9CmdType> Cmds { get; set; }

		// command args
		public List<GraphicResourceID> ResIDs;
		public List<bool> Bools;
        public List<RenderTarget> RTs;
        public List<int> Ints;
        public List<Int3> Int3s;
        public Float4x4[] Insts;
        public List<VertexBuffer> VBs;
        public List<Shader> Shaders;
        public List<AARect> Rects;
        public List<IndexBuffer> IBs;
        public List<string> Strings;
        public List<Texture> Textures;
        public List<float> Floats;
        public List<Float2> Float2s;
        public List<Float3> Float3s;
        public List<Float4> Float4s;
        public List<Float4x4> Float4x4s;
        public List<Float3x3> Float3x3s;
        public List<float[]> FloatAs;
        public List<Float2[]> Float2As;
        public List<Float3[]> Float3As;
        public List<Float4[]> Float4As;
        public List<Float4x4[]> Float4x4As;
        public List<int[]> IntAs;

        // state
        public int NextFreeInstanceId;

        public Directx9CmdList(GraphicResourceID resID)
        {
            this.ResourceID = resID;
            Cmds = new List<Directx9CmdType>();

			ResIDs = new List<GraphicResourceID>();
			Bools = new List<bool>();
            RTs = new List<RenderTarget>();
            Ints = new List<int>();
            Int3s = new List<Int3>();
            Insts = new Float4x4[256];
            VBs = new List<VertexBuffer>();
            Shaders = new List<Shader>();
            Rects = new List<AARect>();
            IBs = new List<IndexBuffer>();
            Strings = new List<string>();
            Textures = new List<Texture>();
            Floats = new List<float>();
            Float2s = new List<Float2>();
            Float3s = new List<Float3>();
            Float4s = new List<Float4>();
            Float4x4s = new List<Float4x4>();
            Float3x3s = new List<Float3x3>();
            FloatAs = new List<float[]>();
            Float2As = new List<Float2[]>();
            Float3As = new List<Float3[]>();
            Float4As = new List<Float4[]>();
            Float4x4As = new List<Float4x4[]>();
            IntAs = new List<int[]>();
        }

        public void Reset()
        {
            Cmds.Clear();

            ResIDs.Clear();
            Bools.Clear();
            RTs.Clear();
            Ints.Clear();
            Int3s.Clear();
            NextFreeInstanceId = 0;
            VBs.Clear();
            Shaders.Clear();
            Rects.Clear();
            IBs.Clear();
            Strings.Clear();
            Textures.Clear();
            Floats.Clear();
            Float2s.Clear();
            Float3s.Clear();
            Float4s.Clear();
            Float4x4s.Clear();
            Float3x3s.Clear();
            FloatAs.Clear();
            Float2As.Clear();
            Float3As.Clear();
            Float4As.Clear();
            Float4x4As.Clear();
            IntAs.Clear();
        }

        public void SaveInstances(ArrayRange<Float4x4> instances)
        {
            // re-create the buffer if the specified instanced cannot be fitted
            if(Insts.Length - NextFreeInstanceId < instances.Count)
            {
                Float4x4[] newInstBuffer = new Float4x4[Insts.Length + System.Math.Max(Insts.Length, 2 * instances.Count)];
                Array.Copy(Insts, newInstBuffer, Insts.Length);
                Insts = newInstBuffer;
            }

            // copy the new instances to the buffer and save their start index and count
            instances.CopyTo(Insts, NextFreeInstanceId);
            Ints.Add(NextFreeInstanceId);
            Ints.Add(instances.Count);
            NextFreeInstanceId += instances.Count;
        }
	}

	internal enum Directx9CmdType
    {
        ClearSurfaces,
        Draw,
        SetRenderTarget,
        DrawIndexed,
        DrawIndexedInstanced,
        DisableRenderTarget,
        SetVertices,
        SetShader,
        ResetRenderTargets,
        SetViewport,
        SetIndices,
        SetParamBool,
        SetParamFloat,
        SetParamFloat2,
        SetParamFloat3,
        SetParamFloat4,
        SetParamFloat4x4,
        SetParamFloat3x3,
        SetParamInt3,
        SetParamFloat2Array,
        SetParamFloat3Array,
        SetParamFloat4Array,
        SetParamFloat4x4Array,
        SetParamIntArray,
        SetParamFloatArray,
        SetParamTexture,
        SetParamRT,
        SetParamInt,
    }
}
