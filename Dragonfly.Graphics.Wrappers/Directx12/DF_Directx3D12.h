#pragma once

#include "DX12.h"
#include "../DirectxCommon.h"

namespace DragonflyGraphicsWrappers {
	namespace DX12 {

		/// <summary>
		/// Directx12 Utility functions.
		/// </summary>
		public ref class DF_Directx3D12 abstract sealed
		{
		public:
			/// <summary>
			/// Check if directx 12 is available on this host.
			/// </summary>
			static bool IsAvailable();

			/// <summary>
			/// Helper function for acquiring the first available hardware adapter that supports Direct3D 12.
			/// If no such adapter can be found, *ppAdapter will be set to nullptr.
			/// </summary>
			static void GetHardwareAdapter(IDXGIFactory1* pFactory, IDXGIAdapter1** ppAdapter, bool requestHighPerformanceAdapter);

			/// <summary>
			/// Returns the number of backbuffer used by the swapchain.
			/// </summary>
			static int GetBackbufferCount();

			/// <summary>
			/// Given a CBuffer byte size, returns the minimum padded addressable size.
			/// </summary>
			static int PadCBufferSize(int requiredSize);

			/// <summary>
			/// Returns the list of valid display modes for the default output. 
			/// </summary>
			static System::Collections::Generic::List<DF_DisplayMode>^ GetDefaultDisplayModes();

		}; // DF_Directx3D12

	}
}