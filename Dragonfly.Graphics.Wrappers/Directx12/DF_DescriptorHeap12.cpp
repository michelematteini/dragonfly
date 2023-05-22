#include "DF_DescriptorHeap12.h"


namespace DragonflyGraphicsWrappers {
	namespace DX12 {
		
		DF_DescriptorHeap12::DF_DescriptorHeap12(ID3D12Device* device, D3D12_DESCRIPTOR_HEAP_TYPE type, UINT descriptorCount)
		{
			// Describe and create a render target view (RTV) descriptor heap.
			{
				D3D12_DESCRIPTOR_HEAP_DESC rtvHeapDesc = {};
				rtvHeapDesc.NumDescriptors = descriptorCount;
				rtvHeapDesc.Type = type;
				rtvHeapDesc.Flags = type == D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV ? D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE : D3D12_DESCRIPTOR_HEAP_FLAG_NONE;
				descrHeapPtr = MakeComPtr(ID3D12DescriptorHeap);
				DF_D3DErrors::Throw(device->CreateDescriptorHeap(&rtvHeapDesc, IID_PPV_ARGS(descrHeapPtr)));

				descriptorSize = device->GetDescriptorHandleIncrementSize(type);
			}

			nextRTVFreeSlot = 0;
			capacity = descriptorCount;
			freeRTVSlots = gcnew Stack<int>();
		}
		
		int DF_DescriptorHeap12::ReserveSlot()
		{
			if (freeRTVSlots->Count > 0)
				return freeRTVSlots->Pop();

#ifdef _DEBUG
			if(nextRTVFreeSlot >= capacity)
				throw gcnew System::Exception("Descriptor heap capacity reached!");
#endif

			return nextRTVFreeSlot++;
		}
		
		void DF_DescriptorHeap12::FreeSlot(int index)
		{
			freeRTVSlots->Push(index);
		}
		
		D3D12_CPU_DESCRIPTOR_HANDLE DF_DescriptorHeap12::GetDescriptorAt(int index)
		{
			return CD3DX12_CPU_DESCRIPTOR_HANDLE(descrHeapPtr[0]->GetCPUDescriptorHandleForHeapStart(), index, descriptorSize);
		}
		
		D3D12_GPU_DESCRIPTOR_HANDLE DF_DescriptorHeap12::GetGPUDescriptorAt(int index)
		{
			return CD3DX12_GPU_DESCRIPTOR_HANDLE(descrHeapPtr[0]->GetGPUDescriptorHandleForHeapStart(), index, descriptorSize);
		}
		
		ID3D12DescriptorHeap* DF_DescriptorHeap12::GetHeap()
		{
			return descrHeapPtr[0];
		}
		
		ID3D12DescriptorHeap** DF_DescriptorHeap12::GetHeapPtr()
		{
			return descrHeapPtr;
		}
	}
}
