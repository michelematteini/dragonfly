#pragma once

#include "DX12.h"

using namespace System::Runtime::InteropServices;

namespace DragonflyGraphicsWrappers {
	namespace DX12 {
		
		ref class DF_Fence12;
		ref class DF_DescriptorHeap12;
		ref class DF_Resource12;
		ref class DF_CommandList12;
		ref class DF_PipelineState12;

		/// <summary>
		/// The main directx11 device context. 
		/// </summary>
		public ref class DF_D3D12Device : public DF_Resource
		{
		private:
			ID3D12Device** devicePtr;
			IDXGISwapChain3** swapChainPtr;
			ID3D12CommandQueue** cmdQueuePtr;
			ID3D12RootSignature** rootSignaturePtr;
			bool offlineMode;
			int frameIndex; // index of the frame buffer this device is currently rendering to
			DF_Fence12^ frameBufferFence; // fence used to sinc with frame completion events
			cli::array<UINT64>^ frameFenceValues; // last index signal queued for each frame index
			DF_DescriptorHeap12^ rtvHeap; // descriptor heap that stores RTVs
			DF_DescriptorHeap12^ dsvHeap; // descriptor heap that stores DSVs
			DF_DescriptorHeap12^ srvHeap; // descriptor heap that stores SRVs
			cli::array<DF_Resource12^>^ frameBuffers; // frame buffer resources
			DF_Resource12^ defaultDepth; // default depth buffer 

		internal:
			ID3D12Device* GetDevice();

			IDXGISwapChain3* GetSwapChain();

			ID3D12CommandQueue* GetCmdQueue();

			ID3D12RootSignature* GetRootSignature();

		public:
			DF_D3D12Device(System::IntPtr targetHandle, bool fullScreen, int preferredWidth, int preferredHeight, bool antiAliasing, UINT rootConstantsByteSize, cli::array<DF_SamplerDesc12>^ samplerDescList);

			DF_CommandList12^ CreateCommandList(int framePadding);

			DF_Resource12^ CreateRenderTarget(UINT width, UINT height, DF_SurfaceFormat format);

			DF_Resource12^ CreateDepthBuffer(UINT width, UINT height);

			/// <summary>
			/// Create a committed buffer that fits constant buffers or a vertex buffer.
			/// </summary>
			/// <param name="byteSize">The byte size of the buffer. If this is created to fit a constant buffer, this size have to be aligned using DF_Directx3D12::PadCBufferSize().</param>
			/// <param name="cpuAccess">Determine the type of heap used to create the buffer.</param>
			DF_Resource12^ CreateBuffer(int byteSize, DF_CPUAccess cpuAccess);

			DF_Resource12^ CreateVertexBuffer(int vertexByteSize, int vertexCount, DF_CPUAccess cpuAccess);

			DF_Resource12^ CreateIndexBuffer(int indexCount, DF_CPUAccess cpuAccess);

			DF_PipelineState12^ CreatePSO(DF_PSODesc12 psoDesc);

			/// <summary>
			/// Create a texture on a default heap, from the bytes loaded from a DDS or other image file.
			/// </summary>
			/// <param name="fileBytes">The file bytes.</param>
			/// <param name="isDDS">True if the specified bytes are from a dds file. Only DDS can load mipmaps.</param>
			/// <param name="copyCommandList">The command list used to upload the texture to gpu.</param>
			/// <param name="uploadResource">The resource created by this call to upload texture data, the called can then release it when the copy added to the command list is completed.</param>
			/// <param name="width">The width of the created texture.</param>
			/// <param name="height">The height if the created texture.</param>
			/// <returns>The default heap resource of the created texture.</returns>
			DF_Resource12^ CreateTexture(cli::array<System::Byte>^ fileBytes, bool isDDS, DF_CommandList12^ copyCommandList, [Out] DF_Resource12^% uploadResource, [Out] int% width, [Out] int% height);

			/// <summary>
			/// Create a texture on a default heap, from the specified sizes and format.
			/// </summary>
			DF_Resource12^ CreateTexture(UINT width, UINT height, DF_SurfaceFormat format);

			void ExecuteCommandLists(cli::array<DF_CommandList12^>^ lists, int count);

			void ExecuteCommandList(DF_CommandList12^ list);

			/// <summary>
			/// Returns the back buffer resource for the current frame.
			/// </summary>
			DF_Resource12^ GetBackBuffer();

			/// <summary>
			/// Returns the default depth buffer resource for the current frame.
			/// </summary>
			DF_Resource12^ GetBackBufferDepth();

			/// <summary>
			/// Returns the index of currently used backbuffer. 
			/// </summary>
			int GetBackBufferIndex();

			void Present();

			/// <summary>
			/// Queue a signal to be execute on the gpu when all the previously submitted commands are executed.
			/// </summary>
			/// <param name="fence">The Fence that will get signaled.</param>
			void QueueGpuSignalTo(DF_Fence12^ fence);

			/// <summary>
			/// Waits all the commands on the command queue to be executed.
			/// </summary>
			void WaitForGPU();

			/// <summary>
			/// Resize the swapchaing, keeping the current target window.
			/// </summary>
			/// <param name="width">New width</param>
			/// <param name="height">New height</param>
			void Resize(UINT width, UINT height);

			/// <summary>
			/// Rebuild the swapchain from the specified parameters.
			/// </summary>
			void UpdateSwapChain(System::IntPtr targetHandle, bool fullScreen, int width, int height);

			void SetFullscreen(bool enabled);

			virtual void Release() override;

			System::Collections::Generic::List<DF_DisplayMode>^ GetDisplayModes();

		internal:

			DF_Resource12^ CreateCommittedRes(const D3D12_RESOURCE_DESC& desc, const D3D12_CLEAR_VALUE* clearValue, D3D12_RESOURCE_STATES initialState, D3D12_HEAP_TYPE heapType, ResourceViews requiredViews);

		private:

			void CreateDXGI(IDXGIFactory4** dxgi);

			void CreateDevice(IDXGIFactory4* dxgi);

			void CreateCommandQueue();

			void CreateSwapChain(IDXGIFactory4* dxgi, System::IntPtr targetHandle, int width, int height);

			void CreateDefaultHeaps();

			void CreateRootSignature(UINT rootConstantsByteSize, cli::array<DF_SamplerDesc12>^ samplerDescList);

			void ReleaseSwapChain();

			void PrepareFrameBuffers();

			void ReleaseFrameBuffers();


		}; // DF_D3D11Device


	}
}