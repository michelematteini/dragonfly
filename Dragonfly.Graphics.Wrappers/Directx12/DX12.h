#pragma once

#include "atlbase.h"
#include "dxgi1_6.h"
#include "../DirectxCommon.h"
#include "d3dx12.h"
#include <msclr/marshal.h>
#include <vector>
#include "../Pix.h"

#define DF_12_FEATURE_LEVEL D3D_FEATURE_LEVEL_12_0 // feature level required by this api
#define DF_12_FRAME_COUNT 2 // number of frame buffer used for the swapchain
#define DF_12_MAX_RT_COUNT 128 // maximum number of render targets that can be created by an app
#define DF_12_MAX_DS_COUNT 128 // maximum number of depth stencil that can be created by an app
#define DF_12_MAX_TEX_COUNT 100000 // maximum number of textures that can be created by an app
#define DX12_CBUFF_BLOCK_SIZE 256 // cbuffer size must be multiple of this value
#define DF_12_SWAPCHAIN_FLAGS (DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH) 

namespace DragonflyGraphicsWrappers {
	namespace DX12 {

#pragma region ENUMS

#define HAS_FLAG(EnumValue, Flag) ((EnumValue & Flag) == Flag)

		enum RS_PARAMID
		{
			RS_PARAMID_CONSTANTS = 0,
			RS_PARAMID_CBV_GLOBALS,
			RS_PARAMID_CBV_MATERIAL,
			RS_PARAMID_UNBOUNDED_TEX_TABLE,
			RS_PARAMID_COUNT
		};

		public enum class DF_Resource12Flags
		{
			None = 0,
			FrameBuffer = 1 << 0
		};

		public enum class DF_TextureFilterType12
		{
			Point = D3D12_FILTER_MIN_MAG_MIP_POINT,
			MinMagPointMipLinear = D3D12_FILTER_MIN_MAG_POINT_MIP_LINEAR,
			Linear = D3D12_FILTER_MIN_MAG_MIP_LINEAR,
			Anisotropic = D3D12_FILTER_ANISOTROPIC,
		};

		public enum class DF_StaticBorderColor12
		{
			TransparentBlack = D3D12_STATIC_BORDER_COLOR_TRANSPARENT_BLACK,
			OpaqueBlack = D3D12_STATIC_BORDER_COLOR_OPAQUE_BLACK,
			OpaqueWhite = D3D12_STATIC_BORDER_COLOR_OPAQUE_WHITE
		};

		enum class ResourceViews
		{
			None = 0,
			SRV = 1 << 0,
			DSV = 1 << 1,
			RTV = 1 << 2,
			CBV = 1 << 3
		};
		DEFINE_ENUM_FLAG_OPERATORS(ResourceViews)

		public enum class DF_CPUAccess
		{
			None,
			Read, 
			Write
		};

		public enum class DF_ResourceState
		{
			Common  = D3D12_RESOURCE_STATE_COMMON,
			VertexAndConstantBuffer = D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER,
			IndexBuffer = D3D12_RESOURCE_STATE_INDEX_BUFFER,
			RenderTarget = D3D12_RESOURCE_STATE_RENDER_TARGET,
			NonPixelShaderResource = D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE,
			PixelShaderResource = D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE,
			CopyDest = D3D12_RESOURCE_STATE_COPY_DEST,
			CopySource = D3D12_RESOURCE_STATE_COPY_SOURCE,
			GenericRead = D3D12_RESOURCE_STATE_GENERIC_READ,
			Present = D3D12_RESOURCE_STATE_PRESENT
		};

#pragma endregion

#pragma region STRUCTURES

		public value struct DF_SamplerDesc12
		{
		public:
			DF_TextureAddress AddressType;
			DF_TextureFilterType12 Filter;
			DF_StaticBorderColor12 BorderColor;
			bool MipMaps;
		};

		public value struct DF_PSODesc12
		{
		public:
			cli::array<DF_VertexElement>^ InputLayout;
			cli::array<System::Byte>^ CompiledVS;
			int CompiledVSFirstByteIndex;
			int CompiledVSByteLength;
			cli::array<System::Byte>^ CompiledPS;
			int CompiledPSFirstByteIndex;
			int CompiledPSByteLength;
			DF_CullMode CullMode;
			DF_FillMode FillMode;
			bool BlendEnable;
			DF_BlendMode SrcBlend, DestBlend;
			bool DepthEnabled, DepthWriteEnabled;
			DF_CompareFunc DepthTest;
			int RenderTargetCount;
			cli::array<DF_SurfaceFormat>^ RenderTargetFormats;
		};

#pragma endregion


#pragma region Utils

		public ref class DX12Conv abstract sealed
		{
		public:
			static cli::array<DXGI_FORMAT>^ DeclTypeToDXGIFORMAT = gcnew cli::array<DXGI_FORMAT> {
				DXGI_FORMAT_UNKNOWN,
					DXGI_FORMAT_R32_FLOAT,
					DXGI_FORMAT_R32G32_FLOAT,
					DXGI_FORMAT_R32G32B32_FLOAT,
					DXGI_FORMAT_R32G32B32A32_FLOAT
			};


			static DXGI_FORMAT SurfaceToDXGIFORMAT(DF_SurfaceFormat format);

		};

		public class BarrierGroup
		{
		private:
			std::vector<D3D12_RESOURCE_BARRIER> barriers;
			ID3D12GraphicsCommandList* cmdList;
			
		public:
			BarrierGroup(ID3D12GraphicsCommandList* cmdList);

			void Add(const D3D12_RESOURCE_BARRIER & barrier);

			void Clear();

			void Commit();
		};
		 

#pragma endregion

	}
}