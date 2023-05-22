#pragma once

#include "DX12.h"

namespace DragonflyGraphicsWrappers {
	namespace DX12 {

		ref class DF_Resource12;
		ref class DF_DescriptorHeap12;
		ref class DF_D3D12Device;
		ref class DF_VersionedCBuffer12;
		ref class DF_PipelineState12;

		/// <summary>
		/// Directx12 command list with the associated command allocators
		/// </summary>
		public ref class DF_CommandList12 : public DF_Resource
		{
		private:
			DF_D3D12Device^ device;
			ID3D12GraphicsCommandList** cmdListPtr;
			ID3D12CommandAllocator** cmdAllocatorList;
			int allocatorCount;
			int curAllocatorIndex; 
			cli::array<int>^ frameToAllocatorOffset;
			DF_DescriptorHeap12^ srvDescrHeap;
			cli::array<DF_Resource12^>^ curRenderTargets; // store the last used render targets for state transition purposes
			UINT curRenderTargetCount;

		internal:
			/// <summary>
			/// Create a new command list with the associated allocators.
			/// </summary>
			/// <param name="allocatorPadding">Number of additional allocators created to allow the command list usage to extend for more than a single frame.</param>
			DF_CommandList12(DF_D3D12Device^ device, DF_DescriptorHeap12^ srvDescrHeap, int framePadding);

			ID3D12GraphicsCommandList* GetList();

			ID3D12GraphicsCommandList** GetListPtr();

			int CalcAllocatorIndex();

			void UpdateAllocatorIndex();

			void TransitionOutRenderTargets();

		public:

			void Reset();

			void SetRootConstants(cli::array<System::Byte>^ data);

			void SetGlobalConstantBuffer(DF_Resource12^ cbuffer, int byteOffset);

			void SetLocalConstantBuffer(DF_Resource12^ cbuffer, int byteOffset);

			void SetRenderTargets(cli::array<DF_Resource12^>^ renderTargets, DF_Resource12^ depthStencil);

			void SetPipelineState(DF_PipelineState12^ pso);

			void SetViewport(UINT x, UINT y, UINT width, UINT height);

			void SetScissor(UINT x, UINT y, UINT width, UINT height);

			void SetVertexBuffer(DF_Resource12^ vertexBuffer);

			void SetInstanceBuffer(DF_Resource12^ instanceBuffer, UINT instanceByteOffset, UINT instanceByteSize, UINT instanceBufferByteSize);

			void SetIndexBuffer(DF_Resource12^ indexBuffer);

			void ClearRenderTargetView(DF_Resource12^ renderTarget, float r, float g, float b, float a);

			void ClearRenderTargetView(DF_Resource12^ renderTarget, float r, float g, float b, float a, int fromX, int fromY, int toX, int toY);

			void ClearDepthStencilView(DF_Resource12^ depthStencil, float depth);

			void DrawInstanced(UINT vertexCount, UINT instanceCount);

			void DrawIndexedInstanced(UINT indexCount, UINT instanceCount);

			void CopyResource(DF_Resource12^ dest, DF_Resource12^ src);

			void CopyBufferRegion(DF_Resource12^ dest, UINT64 destOffset, DF_Resource12^ src, UINT64 srcOffset, UINT64 byteSize);

			void CopyTextureRegion(DF_Resource12^ destTexture, DF_Resource12^ srcTexture);

			void ResourceBarrier(DF_Resource12^ res, DF_ResourceState afterState);

			void Close();

			void BeginEvent(System::String^ eventName, System::Byte r, System::Byte g, System::Byte b);

			void EndEvent();

			void OnSwapChainUpdated();

		}; // DF_CommandList12

	}
}