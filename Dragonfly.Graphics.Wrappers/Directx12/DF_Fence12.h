#pragma once

#include "DX12.h"

namespace DragonflyGraphicsWrappers {
	namespace DX12 {

		/// <summary>
		/// Directx12 Fence that implements synchronization events between cpu and gpu
		/// </summary>
		public ref class DF_Fence12 : DF_Resource
		{
		private:
			HANDLE fenceEvent;
			UINT64 lastSignaledID, lastWaitedID;
			ID3D12Fence** fencePtr;

		public:
			DF_Fence12(ID3D12Device* device, UINT64 initialValue);

			UINT64 GetLastSignalID();

			UINT64 GetCompletedValue();

			void Wait();

			void Wait(UINT64 forValue);

		internal:
			void QueueSignal(ID3D12CommandQueue* cmdQueue);

		};

	}
}