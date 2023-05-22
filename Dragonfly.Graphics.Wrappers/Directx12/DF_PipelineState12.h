#pragma once

#include "DX12.h"

namespace DragonflyGraphicsWrappers {
	namespace DX12 {

		/// <summary>
		/// Directx12 Pipeline state
		/// </summary>
		public ref class DF_PipelineState12 : DF_Resource
		{
		private:
			ID3D12PipelineState** psoPtr;

		public:
			DF_PipelineState12(ID3D12PipelineState* pipelineState);

		internal:
			ID3D12PipelineState * GetPSO();

		};

	}
}