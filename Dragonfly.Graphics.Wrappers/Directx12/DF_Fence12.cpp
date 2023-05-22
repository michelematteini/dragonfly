#include "DF_Fence12.h"

namespace DragonflyGraphicsWrappers {
	namespace DX12 {

		DF_Fence12::DF_Fence12(ID3D12Device* device, UINT64 initialValue)
		{
			fencePtr = MakeComPtr(ID3D12Fence);
			DF_D3DErrors::Throw(device->CreateFence(initialValue, D3D12_FENCE_FLAG_NONE, IID_PPV_ARGS(fencePtr)));

			fenceEvent = CreateEvent(nullptr, FALSE, FALSE, nullptr);
			if (!fenceEvent)
				DF_D3DErrors::Throw(HRESULT_FROM_WIN32(GetLastError()));
			lastSignaledID = lastWaitedID = initialValue;
		}
		
		UINT64 DF_Fence12::GetLastSignalID()
		{
			return lastSignaledID;
		}
		
		UINT64 DF_Fence12::GetCompletedValue()
		{
			return fencePtr[0]->GetCompletedValue();
		}
		
		void DF_Fence12::Wait()
		{
			Wait(lastSignaledID);
		}
		
		void DF_Fence12::Wait(UINT64 forValue)
		{
			DF_D3DErrors::Throw(fencePtr[0]->SetEventOnCompletion(forValue, fenceEvent));
			WaitForSingleObjectEx(fenceEvent, INFINITE, FALSE);
			lastWaitedID = forValue;
		}
		
		void DF_Fence12::QueueSignal(ID3D12CommandQueue* cmdQueue)
		{
			DF_D3DErrors::Throw(cmdQueue->Signal(fencePtr[0], ++lastSignaledID));
		}
	}
}
