#pragma once

#include "DX12.h"

using namespace System::Collections::Generic;

namespace DragonflyGraphicsWrappers {
	namespace DX12 {

		/// <summary>
		/// A descriptor heap used to manage a list of descriptors of the specified type
		/// </summary>
		ref class DF_DescriptorHeap12 : DF_Resource
		{
		private:
			ID3D12DescriptorHeap** descrHeapPtr;
			UINT descriptorSize;
			int nextRTVFreeSlot;
			Stack<int>^ freeRTVSlots;
			int capacity;

		internal:
			DF_DescriptorHeap12(ID3D12Device* device, D3D12_DESCRIPTOR_HEAP_TYPE type, UINT descriptorCount);

			int ReserveSlot();

			void FreeSlot(int index);

			D3D12_CPU_DESCRIPTOR_HANDLE GetDescriptorAt(int index);

			D3D12_GPU_DESCRIPTOR_HANDLE GetGPUDescriptorAt(int index);

			ID3D12DescriptorHeap* GetHeap();

			ID3D12DescriptorHeap** GetHeapPtr();
		};

	}
}