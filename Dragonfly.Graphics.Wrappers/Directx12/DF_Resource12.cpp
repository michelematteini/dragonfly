#include "DF_Resource12.h"
#include "DF_DescriptorHeap12.h"
#include "DF_CommandList12.h"
#include "DF_D3D12Device.h"

namespace DragonflyGraphicsWrappers {
	namespace DX12 {
		

		generic<typename T> void DF_Resource12::SetData(cli::array<T>^ data, int dataStartOffset, int destByteOffset, int length, bool keepMapped)
		{
			pin_ptr<T> pinPtrData = &data[dataStartOffset];

			// map if stil not mapped
			if (!mappedData[0])
			{
				CD3DX12_RANGE readRange(0, 0);
				DF_D3DErrors::Throw(resourcePtr[0]->Map(0, &readRange, reinterpret_cast<void**>(mappedData)));
			}

			memcpy(mappedData[0] + destByteOffset, pinPtrData, length * sizeof(T));

			if (!keepMapped)
			{
				Unmap();
			}
		}

		generic<typename T>
		void DF_Resource12::GetData(cli::array<T>^ destBuffer, int destOffset)
		{
			pin_ptr<T> pinPtrData = &destBuffer[destOffset];
			UINT copySize = (UINT)GetSizeInBytes();
			DF_D3DErrors::Throw(resourcePtr[0]->Map(0, NULL, reinterpret_cast<void**>(mappedData)));
			memcpy(pinPtrData, mappedData[0], copySize);
			Unmap();
		}

		generic<typename T>
		void DF_Resource12::GetTextureData(cli::array<T>^ destBuffer, UINT width, UINT height)
		{
			pin_ptr<T> pinPtrData = &destBuffer[0];
			
			DF_D3DErrors::Throw(resourcePtr[0]->Map(0, NULL, reinterpret_cast<void**>(mappedData)));
			
			// retrieve texture memory sizes
			UINT bufferSize = (UINT)GetSizeInBytes();
			UINT rowPitch = bufferSize / height;
			UINT bufferWidth = destBuffer->GetLength(0) / height;
			UINT rowSize = bufferWidth * sizeof(T);
			
			// copy row by row
			for (UINT rowID = 0; rowID < height; rowID++)
			{
				pin_ptr<T> pinRowData = &destBuffer[bufferWidth * rowID];
				memcpy(pinRowData, mappedData[0] + rowPitch * rowID, rowSize);
			}

			Unmap();
		}

		generic<typename T>
		void DF_Resource12::UploadData(DF_D3D12Device^ device, cli::array<T>^ data, DF_CommandList12^ copyCommandList, DF_Resource12^% uploadResource)
		{
			pin_ptr<T> dataPtr = &data[0];
			BarrierGroup barriers(copyCommandList->GetList());

			// sync for copy
			AddTransitionIfNeeded(D3D12_RESOURCE_STATE_COPY_DEST, barriers);
			barriers.Commit();

			// create an updload resource to copy the texture data to the default resource
			const UINT64 uploadBufferSize = GetSizeInBytes();
			if (uploadResource == nullptr)
				uploadResource = device->CreateCommittedRes(CD3DX12_RESOURCE_DESC::Buffer(uploadBufferSize), nullptr, D3D12_RESOURCE_STATE_GENERIC_READ, D3D12_HEAP_TYPE_UPLOAD, ResourceViews::None);

			// use the specified command list to update the texture data
			D3D12_SUBRESOURCE_DATA textureData = {};
			{
				D3D12_RESOURCE_DESC textureDesc = GetResource()->GetDesc();
				UINT64 rowSizeInBytes;
				UINT numRows;
				device->GetDevice()->GetCopyableFootprints(&textureDesc, 0, 1, 0, NULL, &numRows, &rowSizeInBytes, NULL);
				textureData.pData = reinterpret_cast<UINT8*>(dataPtr);
				textureData.RowPitch = static_cast<LONG_PTR>(rowSizeInBytes);
				textureData.SlicePitch = static_cast<LONG_PTR>(textureData.RowPitch * numRows);
			}
			UpdateSubresources(copyCommandList->GetList(), GetResource(), uploadResource->GetResource(), 0, 0, 1U, &textureData);

			// sync for update completion before usage
			AddTransitionIfNeeded(D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE | D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE, barriers);
			barriers.Commit();

		}

		void DF_Resource12::DownloadData(DF_D3D12Device^ device, DF_CommandList12^ copyCommandList, DF_Resource12^% downloadResource)
		{
			D3D12_RESOURCE_STATES curState = this->state;
			BarrierGroup barriers(copyCommandList->GetList());

			// sync for copy
			AddTransitionIfNeeded(D3D12_RESOURCE_STATE_COPY_SOURCE, barriers);
			if(downloadResource)
				downloadResource->AddTransitionIfNeeded(D3D12_RESOURCE_STATE_COPY_DEST, barriers);
			barriers.Commit();

			// use the specified command list to copy texture data to the readback resource
			{
				D3D12_TEXTURE_COPY_LOCATION srcLocation, destLocation;

				// prepare copy params and resources
				{
					// fill dest buffer location params
					{
						D3D12_RESOURCE_DESC srcDesc = GetResource()->GetDesc();
						device->GetDevice()->GetCopyableFootprints(&srcDesc, 0, 1, 0, &destLocation.PlacedFootprint, NULL, NULL, NULL);
						destLocation.Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT;

						// create an readback resource to copy the texture data from the default resource if not available
						if (downloadResource == nullptr)
						{
							UINT64 downloadBuffSize = destLocation.PlacedFootprint.Footprint.RowPitch * destLocation.PlacedFootprint.Footprint.Height;
							downloadResource = device->CreateCommittedRes(CD3DX12_RESOURCE_DESC::Buffer(downloadBuffSize), nullptr, D3D12_RESOURCE_STATE_COPY_DEST, D3D12_HEAP_TYPE_READBACK, ResourceViews::None);
						}
						destLocation.pResource = downloadResource->GetResource();
					}

					// fill source location params
					srcLocation.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
					srcLocation.SubresourceIndex = 0;
					srcLocation.pResource = GetResource();
				}

				// copy texture data to the readback resource
				copyCommandList->GetList()->CopyTextureRegion(&destLocation, 0, 0, 0, &srcLocation, NULL);
			}
			

			// sync for update completion before usage
			AddTransitionIfNeeded(curState, barriers);
			barriers.Commit();
		}

		UINT64 DF_Resource12::GetSizeInBytes()
		{
			return GetRequiredIntermediateSize(GetResource(), 0, 1);
		}

		int DF_Resource12::GetSrvIndex()
		{
			return srvID;
		}

		void DF_Resource12::Unmap()
		{
			if (mappedData[0])
			{
				resourcePtr[0]->Unmap(0, NULL);
				mappedData[0] = NULL;
			}
		}
		
		/// <summary>
		/// Add a transition to the current resource if needed, and update its current state.
		/// </summary>
		/// <returns>True if the transition was needed and was filled in, false otherwise</returns>
		DF_Resource12::DF_Resource12(ID3D12Resource* resource, D3D12_RESOURCE_STATES initialState)
		{
			resourcePtr = TrackComPtr(ID3D12Resource, resource);
			rtvID = -1;
			srvID = -1;
			dsvID = -1;
			cbvID = -1;
			state = initialState;
			Flags = DF_Resource12Flags::None;
			mappedData = new UINT8*[1];
			mappedData[0] = NULL;
			vbv = NULL;
			ibv = NULL;
		}

		ID3D12Resource* DF_Resource12::GetResource()
		{
			return resourcePtr[0];
		}

		ID3D12Resource** DF_Resource12::GetResourcePtr()
		{
			return resourcePtr;
		}

		void DF_Resource12::PrepareRTV(ID3D12Device* device, DF_DescriptorHeap12^ descrHeap)
		{
			rtvID = descrHeap->ReserveSlot();
			device->CreateRenderTargetView(resourcePtr[0], nullptr, descrHeap->GetDescriptorAt(rtvID));
			rtvHeap = descrHeap;
		}

		void DF_Resource12::PrepareDSV(ID3D12Device* device, DF_DescriptorHeap12^ descrHeap)
		{
			dsvID = descrHeap->ReserveSlot();
			D3D12_DEPTH_STENCIL_VIEW_DESC depthStencilDesc = {};
			depthStencilDesc.Format = DXGI_FORMAT_D32_FLOAT;
			depthStencilDesc.ViewDimension = D3D12_DSV_DIMENSION_TEXTURE2D;
			depthStencilDesc.Flags = D3D12_DSV_FLAG_NONE;
			device->CreateDepthStencilView(resourcePtr[0], &depthStencilDesc, descrHeap->GetDescriptorAt(dsvID));
			dsvHeap = descrHeap;
		}

		void DF_Resource12::PrepareSRV(ID3D12Device* device, DF_DescriptorHeap12^ descrHeap)
		{
			srvID = descrHeap->ReserveSlot();
			device->CreateShaderResourceView(resourcePtr[0], nullptr, descrHeap->GetDescriptorAt(srvID));
			srvHeap = descrHeap;
		}

		void DF_Resource12::PrepareCBV(ID3D12Device* device, DF_DescriptorHeap12^ descrHeap)
		{
			cbvID = descrHeap->ReserveSlot();
			device->CreateShaderResourceView(resourcePtr[0], nullptr, descrHeap->GetDescriptorAt(cbvID));
			cbvHeap = descrHeap;
		}

		void DF_Resource12::PrepareVBV(UINT vertexByteSize, UINT bufferByteSize)
		{
			vbv = new D3D12_VERTEX_BUFFER_VIEW();
			vbv->BufferLocation = resourcePtr[0]->GetGPUVirtualAddress();
			vbv->StrideInBytes = vertexByteSize;
			vbv->SizeInBytes = bufferByteSize;
		}

		void DF_Resource12::PrepareIBV(UINT bufferByteSize)
		{
			ibv = new D3D12_INDEX_BUFFER_VIEW();
			ibv->BufferLocation = resourcePtr[0]->GetGPUVirtualAddress();
			ibv->SizeInBytes = bufferByteSize;
			ibv->Format = DXGI_FORMAT_R16_UINT;
		}

		D3D12_CPU_DESCRIPTOR_HANDLE DF_Resource12::GetRTV()
		{
			return rtvHeap->GetDescriptorAt(rtvID);
		}

		D3D12_CPU_DESCRIPTOR_HANDLE DF_Resource12::GetDSV()
		{
			return dsvHeap->GetDescriptorAt(dsvID);
		}

		D3D12_VERTEX_BUFFER_VIEW* DF_Resource12::GetVBV()
		{
			return vbv;
		}

		D3D12_INDEX_BUFFER_VIEW* DF_Resource12::GetIBV()
		{
			return ibv;
		}

		D3D12_RESOURCE_STATES DF_Resource12::GetState()
		{
			return state;
		}

		BOOL DF_Resource12::AddTransitionIfNeeded(D3D12_RESOURCE_STATES afterState, BarrierGroup& barriers)
		{
			if (state & afterState)
				return FALSE; // not needed of equal, or compatible (states are flags, some of them are collections of the others
			barriers.Add(CD3DX12_RESOURCE_BARRIER::Transition(resourcePtr[0], state, afterState));
			state = afterState;
			return TRUE;
		}

		void DF_Resource12::AddTransition(D3D12_RESOURCE_STATES beforeState, D3D12_RESOURCE_STATES afterState, BarrierGroup& barriers)
		{
			barriers.Add(CD3DX12_RESOURCE_BARRIER::Transition(resourcePtr[0], beforeState, afterState));
			state = afterState;
		}

		void DF_Resource12::Release()
		{
			if (!IsReleased())
			{
				Unmap();
				delete[] mappedData;
				if (vbv)
					delete vbv;
				if (ibv)
					delete ibv;

				// release descriptor heap slots
				if (rtvID >= 0)
					rtvHeap->FreeSlot(rtvID);
				if (srvID >= 0)
					srvHeap->FreeSlot(srvID);
				if (cbvID >= 0)
					cbvHeap->FreeSlot(cbvID);
				if (dsvID >= 0)
					dsvHeap->FreeSlot(dsvID);
			}
			DF_Resource::Release();
		}

	}
}
