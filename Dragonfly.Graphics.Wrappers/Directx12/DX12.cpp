#include "DX12.h"

namespace DragonflyGraphicsWrappers {
	namespace DX12 {
		
		DXGI_FORMAT DX12Conv::SurfaceToDXGIFORMAT(DF_SurfaceFormat format)
		{
			switch (format)
			{
			default:
			case DragonflyGraphicsWrappers::DF_SurfaceFormat::A8R8G8B8:
				return DXGI_FORMAT_B8G8R8A8_UNORM;
			case DragonflyGraphicsWrappers::DF_SurfaceFormat::R16F:
				return DXGI_FORMAT_R16_FLOAT;
			case DragonflyGraphicsWrappers::DF_SurfaceFormat::G16R16F:
				return DXGI_FORMAT_R16G16_FLOAT;
			case DragonflyGraphicsWrappers::DF_SurfaceFormat::A16B16G16R16F:
				return DXGI_FORMAT_R16G16B16A16_FLOAT;
			case DragonflyGraphicsWrappers::DF_SurfaceFormat::R32F:
				return DXGI_FORMAT_R32_FLOAT;
			case DragonflyGraphicsWrappers::DF_SurfaceFormat::G32R32F:
				return DXGI_FORMAT_R32G32_FLOAT;
			case DragonflyGraphicsWrappers::DF_SurfaceFormat::A32B32G32R32F:
				return DXGI_FORMAT_R32G32B32A32_FLOAT;
			case DragonflyGraphicsWrappers::DF_SurfaceFormat::DEFAULT_DEPTH_FORMAT:
				return DXGI_FORMAT_D32_FLOAT;
			}
		}

		BarrierGroup::BarrierGroup(ID3D12GraphicsCommandList* cmdList)
		{
			this->cmdList = cmdList;
		}

		void BarrierGroup::Add(const D3D12_RESOURCE_BARRIER& barrier)
		{
			barriers.push_back(barrier);
		}

		void BarrierGroup::Clear()
		{
			barriers.clear();
		}

		void BarrierGroup::Commit()
		{
			if (!barriers.size())
				return;

			cmdList->ResourceBarrier((UINT)barriers.size(), barriers.data());
			Clear();
		}

	}
}