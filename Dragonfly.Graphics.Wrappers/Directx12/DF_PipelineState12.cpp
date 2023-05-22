#include "DF_PipelineState12.h"

namespace DragonflyGraphicsWrappers {
	namespace DX12 {

		DF_PipelineState12::DF_PipelineState12(ID3D12PipelineState* pipelineState)
		{
			this->psoPtr = TrackComPtr(ID3D12PipelineState, pipelineState);
		}

		ID3D12PipelineState * DF_PipelineState12::GetPSO()
		{
			return psoPtr[0];
		}

	}
}