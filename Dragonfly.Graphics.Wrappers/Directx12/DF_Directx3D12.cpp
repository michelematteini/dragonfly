#include "DF_Directx3D12.h"

namespace DragonflyGraphicsWrappers {
	namespace DX12 {

		bool DF_Directx3D12::IsAvailable()
		{
			return DF_D3DErrors::Check(D3D12CreateDevice(nullptr, DF_12_FEATURE_LEVEL, _uuidof(ID3D12Device), nullptr));
		}

		int DF_Directx3D12::GetBackbufferCount()
		{
			return DF_12_FRAME_COUNT;
		}

		int DF_Directx3D12::PadCBufferSize(int requiredSize)
		{
			return (requiredSize + DX12_CBUFF_BLOCK_SIZE - 1) / DX12_CBUFF_BLOCK_SIZE * DX12_CBUFF_BLOCK_SIZE;
		}

		System::Collections::Generic::List<DF_DisplayMode>^ DF_Directx3D12::GetDefaultDisplayModes()
		{
			CComPtr<IDXGIOutput> defaultOutput;
			DF_Display::GetDefaultOutput(&defaultOutput);
			return DF_Display::GetDisplayModesFromOutput(defaultOutput);
		}

		void DF_Directx3D12::GetHardwareAdapter(IDXGIFactory1* pFactory, IDXGIAdapter1** ppAdapter, bool requestHighPerformanceAdapter)
		{
			*ppAdapter = nullptr;

			CComPtr<IDXGIAdapter1> adapter;

			// try to search by preference
			CComPtr<IDXGIFactory6> factory6;
			if (DF_D3DErrors::Check(pFactory->QueryInterface(IID_PPV_ARGS(&factory6))))
			{
				DXGI_GPU_PREFERENCE gpuPref = requestHighPerformanceAdapter == true ? DXGI_GPU_PREFERENCE_HIGH_PERFORMANCE : DXGI_GPU_PREFERENCE_UNSPECIFIED;
				for (UINT adapterIndex = 0; DF_D3DErrors::Check(factory6->EnumAdapterByGpuPreference(adapterIndex, gpuPref, IID_PPV_ARGS(&adapter))); ++adapterIndex)
				{
					DXGI_ADAPTER_DESC1 desc;
					adapter->GetDesc1(&desc);

					if (desc.Flags & DXGI_ADAPTER_FLAG_SOFTWARE)
						continue;// Don't select the Basic Render Driver adapter.

								 // Check to see whether the adapter supports Direct3D 12, but don't create the actual device yet.
					if (DF_D3DErrors::Check(D3D12CreateDevice(adapter, DF_12_FEATURE_LEVEL, _uuidof(ID3D12Device), nullptr)))
						break;
				}
			}

			// search normally if not found or searching by preference is not supported 
			if (!adapter)
			{
				for (UINT adapterIndex = 0; DF_D3DErrors::Check(pFactory->EnumAdapters1(adapterIndex, &adapter)); ++adapterIndex)
				{
					DXGI_ADAPTER_DESC1 desc;
					adapter->GetDesc1(&desc);

					if (desc.Flags & DXGI_ADAPTER_FLAG_SOFTWARE)
						continue; // Don't select the Basic Render Driver adapter.

								  // Check to see whether the adapter supports Direct3D 12, but don't create the actual device yet.
					if (DF_D3DErrors::Check(D3D12CreateDevice(adapter, DF_12_FEATURE_LEVEL, _uuidof(ID3D12Device), nullptr)))
						break;
				}
			}

			*ppAdapter = adapter.Detach();
		}

	}
}