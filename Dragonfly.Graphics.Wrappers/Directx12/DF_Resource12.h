#pragma once

#include "DX12.h"


namespace DragonflyGraphicsWrappers {
	namespace DX12 {

		ref class DF_DescriptorHeap12;
		ref class DF_D3D12Device;
		ref class DF_CommandList12;
		ref class DF_Resource12;

		/// <summary>
		/// A directx 12 managed resource, which store its native pointer and help manage views.
		/// </summary>
		public ref class DF_Resource12 : DF_Resource
		{
		private:
			ID3D12Resource** resourcePtr;
			int rtvID; // index of the render target view descriptor for this resource, -1 if not available
			DF_DescriptorHeap12^ rtvHeap;
			int srvID; // index of the shader resource view descriptor for this resource, -1 if not available
			DF_DescriptorHeap12^ srvHeap;
			int dsvID; // index of the depth stencil view descriptor for this resource, -1 if not available
			DF_DescriptorHeap12^ cbvHeap;
			int cbvID; // index of the constant buffer view descriptor for this resource, -1 if not available
			DF_DescriptorHeap12^ dsvHeap;
			D3D12_RESOURCE_STATES state; // last state for this resource
			UINT8** mappedData;
			D3D12_VERTEX_BUFFER_VIEW *vbv;
			D3D12_INDEX_BUFFER_VIEW* ibv;

		public:

			/// <summary>
			/// Update a buffer resource with the specified data.
			/// </summary>
			generic <typename T> void SetData(cli::array<T>^ data, int dataStartOffset, int destByteOffset, int length, bool keepMapped);

			/// <summary>
			/// Copy the resource content to a buffer.
			/// </summary>
			generic <typename T> void GetData(cli::array<T>^ destBuffer, int destOffset);

			/// <summary>
			/// Copy the resource content to a buffer, when the buffer contains texture data.
			/// </summary>
			generic <typename T> void GetTextureData(cli::array<T>^ destBuffer, UINT width, UINT height);

			/// <summary>
			/// Update a buffer resource with the specified data.
			/// </summary>
			/// <typeparam name="T"></typeparam>
			/// <param name="data"></param>
			/// <param name="copyCommandList"></param>
			/// <param name="uploadResource">An upload resource to upload the specified data to the GPU. If none is specified, a new one will be created.</param>
			generic <typename T> void UploadData(DF_D3D12Device^ device, cli::array<T>^ data, DF_CommandList12^ copyCommandList, DF_Resource12^% uploadResource);

			/// <summary>
			/// Copy data from a default resource to the specified cpu accessible resource. If not download resource is specified, a new one is created.
			/// </summary>
			void DownloadData(DF_D3D12Device^ device, DF_CommandList12^ copyCommandList, DF_Resource12^% downloadResource);

			UINT64 GetSizeInBytes();

			int GetSrvIndex();

			virtual void Release() override;

		internal:
			void Unmap();

			DF_Resource12Flags Flags;

			DF_Resource12(ID3D12Resource* resource, D3D12_RESOURCE_STATES initialState);

			ID3D12Resource* GetResource();

			ID3D12Resource** GetResourcePtr();

			void PrepareRTV(ID3D12Device* device, DF_DescriptorHeap12^ descrHeap);

			void PrepareDSV(ID3D12Device* device, DF_DescriptorHeap12^ descrHeap);

			void PrepareSRV(ID3D12Device* device, DF_DescriptorHeap12^ descrHeap);

			void PrepareCBV(ID3D12Device* device, DF_DescriptorHeap12^ descrHeap);

			void PrepareVBV(UINT vertexByteSize, UINT bufferByteSize);

			void PrepareIBV(UINT bufferByteSize);

			D3D12_CPU_DESCRIPTOR_HANDLE GetRTV();

			D3D12_CPU_DESCRIPTOR_HANDLE GetDSV();

			D3D12_VERTEX_BUFFER_VIEW* GetVBV();

			D3D12_INDEX_BUFFER_VIEW* GetIBV();

			D3D12_RESOURCE_STATES GetState();

			/// <summary>
			/// Add a transition to the current resource if needed, and update its current state.
			/// </summary>
			/// <returns>True if the transition was needed and was filled in, false otherwise</returns>
			BOOL AddTransitionIfNeeded(D3D12_RESOURCE_STATES afterState, BarrierGroup & barriers);

			/// <summary>
			/// Add a transition to the current resource and update its current state.
			/// </summary>
			void AddTransition(D3D12_RESOURCE_STATES beforeState, D3D12_RESOURCE_STATES afterState, BarrierGroup& barriers);

		}; // DF_Resource12




}
}