#include "DF_Resource12.h"
#include "DF_DescriptorHeap12.h"
#include "DF_CommandList12.h"
#include "DF_D3D12Device.h"
#include "DF_PipelineState12.h"

using namespace msclr::interop;

namespace DragonflyGraphicsWrappers {
	namespace DX12 {
		
		DF_CommandList12::DF_CommandList12(DF_D3D12Device^ device, DF_DescriptorHeap12^ srvDescrHeap, int framePadding)
		{
			frameToAllocatorOffset = gcnew cli::array<int>(DF_12_FRAME_COUNT);
			allocatorCount = DF_12_FRAME_COUNT + framePadding;

			this->srvDescrHeap = srvDescrHeap;
			this->device = device;
			OnSwapChainUpdated();
			curRenderTargets = gcnew cli::array<DF_Resource12^>(8);

			// create allocators
			cmdAllocatorList = MakeComPtrList(ID3D12CommandAllocator, allocatorCount);
			for (int i = 0; i < allocatorCount; i++)
				DF_D3DErrors::Throw(device->GetDevice()->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, IID_PPV_ARGS(&cmdAllocatorList[i])));

			// create command list
			cmdListPtr = MakeComPtr(ID3D12GraphicsCommandList);
			DF_D3DErrors::Throw(device->GetDevice()->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_DIRECT, cmdAllocatorList[device->GetSwapChain()->GetCurrentBackBufferIndex()], nullptr, IID_PPV_ARGS(cmdListPtr)));
			DF_D3DErrors::Throw(cmdListPtr[0]->Close()); // since it starts in a recording state...
		}
		
		ID3D12GraphicsCommandList* DF_CommandList12::GetList()
		{
			return *cmdListPtr;
		}
		
		ID3D12GraphicsCommandList** DF_CommandList12::GetListPtr()
		{
			return cmdListPtr;
		}

		int DF_CommandList12::CalcAllocatorIndex()
		{
			int bbFrame = device->GetBackBufferIndex();
			return (bbFrame + frameToAllocatorOffset[bbFrame]) % allocatorCount;
		}

		void DF_CommandList12::UpdateAllocatorIndex()
		{
			int bbFrame = device->GetBackBufferIndex();
			curAllocatorIndex = (bbFrame + frameToAllocatorOffset[bbFrame]) % allocatorCount;
			frameToAllocatorOffset[bbFrame] = (frameToAllocatorOffset[bbFrame] + DF_12_FRAME_COUNT) % allocatorCount;
		}

		void DF_CommandList12::TransitionOutRenderTargets()
		{
			if (!curRenderTargetCount)
				return;

			BarrierGroup barriers(GetList());
			for (UINT i = 0; i < curRenderTargetCount; i++)
			{
				D3D12_RESOURCE_STATES nextRtState;
				if ((curRenderTargets[i]->Flags & DF_Resource12Flags::FrameBuffer) == DF_Resource12Flags::FrameBuffer)
					nextRtState = D3D12_RESOURCE_STATE_PRESENT;
				else
					nextRtState = D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE | D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE;

				curRenderTargets[i]->AddTransition(D3D12_RESOURCE_STATE_RENDER_TARGET, nextRtState, barriers);
			}
			barriers.Commit();
			curRenderTargetCount = 0;
		}

		void DF_CommandList12::Reset()
		{
			int allocatorIndex = CalcAllocatorIndex();
			if (allocatorIndex != curAllocatorIndex)
				DF_D3DErrors::Throw(cmdAllocatorList[allocatorIndex]->Reset());
			DF_D3DErrors::Throw(cmdListPtr[0]->Reset(cmdAllocatorList[allocatorIndex], NULL));
			cmdListPtr[0]->SetGraphicsRootSignature(device->GetRootSignature());
			cmdListPtr[0]->SetDescriptorHeaps(1, srvDescrHeap->GetHeapPtr());
			cmdListPtr[0]->SetGraphicsRootDescriptorTable(RS_PARAMID_UNBOUNDED_TEX_TABLE, srvDescrHeap->GetGPUDescriptorAt(0));
			cmdListPtr[0]->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
			UpdateAllocatorIndex();
		}

		void DF_CommandList12::SetRootConstants(cli::array<System::Byte>^ data)
		{
			pin_ptr<System::Byte> pinnedData = &data[0];
			cmdListPtr[0]->SetGraphicsRoot32BitConstants(RS_PARAMID_CONSTANTS, data->Length / 4, pinnedData, 0);
		}

		void DF_CommandList12::SetGlobalConstantBuffer(DF_Resource12^ cbuffer, int byteOffset)
		{
			cmdListPtr[0]->SetGraphicsRootConstantBufferView(RS_PARAMID_CBV_GLOBALS, cbuffer->GetResource()->GetGPUVirtualAddress() + byteOffset);
		}

		void DF_CommandList12::SetLocalConstantBuffer(DF_Resource12^ cbuffer, int byteOffset)
		{
			cmdListPtr[0]->SetGraphicsRootConstantBufferView(RS_PARAMID_CBV_MATERIAL, cbuffer->GetResource()->GetGPUVirtualAddress() + byteOffset);
		}

		void DF_CommandList12::SetRenderTargets(cli::array<DF_Resource12^>^ renderTargets, DF_Resource12^ depthStencil)
		{
			TransitionOutRenderTargets();

			// prepare descriptors and barrirers for render targets
			D3D12_CPU_DESCRIPTOR_HANDLE rtDescrList[8];
			BarrierGroup barriers(GetList());
			for (int i = 0; i < renderTargets->Length; i++)
			{
				if (!renderTargets[i])
				{
					curRenderTargetCount = i;
					break;
				}

				curRenderTargets[i] = renderTargets[i];
				rtDescrList[i] = renderTargets[i]->GetRTV();

				D3D12_RESOURCE_STATES prevState = D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE | D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE;
				if ((curRenderTargets[i]->Flags & DF_Resource12Flags::FrameBuffer) == DF_Resource12Flags::FrameBuffer)
					prevState = D3D12_RESOURCE_STATE_PRESENT;
				renderTargets[i]->AddTransition(prevState, D3D12_RESOURCE_STATE_RENDER_TARGET, barriers);
			}

			// prepare depth stencil descriptor
			D3D12_CPU_DESCRIPTOR_HANDLE dsDescr;
			if (depthStencil)
				dsDescr = depthStencil->GetDSV();

			// issue commands
			barriers.Commit();
			cmdListPtr[0]->OMSetRenderTargets(curRenderTargetCount, rtDescrList, FALSE, depthStencil ? &dsDescr : NULL);
		}

		void DF_CommandList12::SetPipelineState(DF_PipelineState12^ pso)
		{
			cmdListPtr[0]->SetPipelineState(pso->GetPSO());
		}

		void DF_CommandList12::SetViewport(UINT x, UINT y, UINT width, UINT height)
		{
			D3D12_VIEWPORT viewport;
			viewport.Height = (float)height;
			viewport.MinDepth = 0.0f;
			viewport.MaxDepth = 1.0f;
			viewport.TopLeftX = (float)x;
			viewport.TopLeftY = (float)y;
			viewport.Width = (float)width;
			cmdListPtr[0]->RSSetViewports(1, &viewport);
		}

		void DF_CommandList12::SetScissor(UINT x, UINT y, UINT width, UINT height)
		{
			CD3DX12_RECT scissor(x, y, width, height);
			cmdListPtr[0]->RSSetScissorRects(1, &scissor);
		}

		void DF_CommandList12::SetVertexBuffer(DF_Resource12^ vertexBuffer)
		{
			cmdListPtr[0]->IASetVertexBuffers(0, 1, vertexBuffer->GetVBV());
		}

		void DF_CommandList12::SetInstanceBuffer(DF_Resource12^ instanceBuffer, UINT instanceByteOffset, UINT instanceByteSize, UINT instanceBufferByteSize)
		{
			D3D12_VERTEX_BUFFER_VIEW instanceVbView;
			instanceVbView.BufferLocation = instanceBuffer->GetResource()->GetGPUVirtualAddress() + instanceByteOffset;
			instanceVbView.StrideInBytes = instanceByteSize;
			instanceVbView.SizeInBytes = instanceBufferByteSize;
			cmdListPtr[0]->IASetVertexBuffers(1, 1, &instanceVbView);
		}

		void DF_CommandList12::SetIndexBuffer(DF_Resource12^ indexBuffer)
		{
			D3D12_INDEX_BUFFER_VIEW* ibView = indexBuffer->GetIBV();
			cmdListPtr[0]->IASetIndexBuffer(ibView);
		}
		
		void DF_CommandList12::ClearRenderTargetView(DF_Resource12^ renderTarget, float r, float g, float b, float a)
		{
			FLOAT clearColor[] = { r, g, b, a };
			cmdListPtr[0]->ClearRenderTargetView(renderTarget->GetRTV(), clearColor, 0, NULL);
		}
		
		void DF_CommandList12::ClearRenderTargetView(DF_Resource12^ renderTarget, float r, float g, float b, float a, int fromX, int fromY, int toX, int toY)
		{
			FLOAT clearColor[] = { r, g, b, a };
			D3D12_RECT clearRect = { fromX, fromY, toX, toY };
			cmdListPtr[0]->ClearRenderTargetView(renderTarget->GetRTV(), clearColor, 1, &clearRect);
		}
		
		void DF_CommandList12::ClearDepthStencilView(DF_Resource12^ depthStencil, float depth)
		{
			cmdListPtr[0]->ClearDepthStencilView(depthStencil->GetDSV(), D3D12_CLEAR_FLAG_DEPTH, depth, 0, 0, NULL);
		}

		inline void DF_CommandList12::DrawInstanced(UINT vertexCount, UINT instanceCount)
		{
			cmdListPtr[0]->DrawInstanced(vertexCount, instanceCount, 0, 0);
		}

		inline void DF_CommandList12::DrawIndexedInstanced(UINT indexCount, UINT instanceCount)
		{
			cmdListPtr[0]->DrawIndexedInstanced(indexCount, instanceCount, 0, 0, 0);
		}

		void DF_CommandList12::CopyResource(DF_Resource12^ dest, DF_Resource12^ src)
		{
			cmdListPtr[0]->CopyResource(dest->GetResource(), src->GetResource());
		}

		void DF_CommandList12::CopyBufferRegion(DF_Resource12^ dest, UINT64 destOffset,  DF_Resource12^ src, UINT64 srcOffset, UINT64 byteSize)
		{
			// save current resource states
			D3D12_RESOURCE_STATES srcState = src->GetState(), destState = dest->GetState();

			// copy barriers
			BarrierGroup barriers(GetList());
			dest->AddTransitionIfNeeded(D3D12_RESOURCE_STATE_COPY_DEST, barriers);
			src->AddTransitionIfNeeded(D3D12_RESOURCE_STATE_COPY_SOURCE, barriers);
			barriers.Commit();

			// copy buffers
			cmdListPtr[0]->CopyBufferRegion(dest->GetResource(), destOffset, src->GetResource(), srcOffset, byteSize);

			// restore states
			dest->AddTransitionIfNeeded(destState, barriers);
			src->AddTransitionIfNeeded(srcState, barriers);
			barriers.Commit();
		}

		void DF_CommandList12::CopyTextureRegion(DF_Resource12^ destTexture, DF_Resource12^ srcTexture)
		{
			D3D12_RESOURCE_STATES srcState = srcTexture->GetState(), destState = destTexture->GetState();
			BarrierGroup barriers(GetList());

			// sync for copy
			srcTexture->AddTransitionIfNeeded(D3D12_RESOURCE_STATE_COPY_SOURCE, barriers);
			destTexture->AddTransitionIfNeeded(D3D12_RESOURCE_STATE_COPY_DEST, barriers);
			barriers.Commit();

			// prepare copy params and resources
			D3D12_TEXTURE_COPY_LOCATION srcLocation;
			srcLocation.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
			srcLocation.SubresourceIndex = 0;
			srcLocation.pResource = srcTexture->GetResource();

			D3D12_TEXTURE_COPY_LOCATION destLocation;
			destLocation.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
			destLocation.SubresourceIndex = 0;
			destLocation.pResource = destTexture->GetResource();

			// copy texture data 
			GetList()->CopyTextureRegion(&destLocation, 0, 0, 0, &srcLocation, NULL);

			// sync for copy completion before usage
			srcTexture->AddTransitionIfNeeded(srcState, barriers);
			destTexture->AddTransitionIfNeeded(destState, barriers);
			barriers.Commit();
		}

		void DF_CommandList12::ResourceBarrier(DF_Resource12^ res, DF_ResourceState afterState)
		{
			BarrierGroup barriers(GetList());
			res->AddTransitionIfNeeded(static_cast<D3D12_RESOURCE_STATES>(afterState), barriers);
			barriers.Commit();
		}
		
		void DF_CommandList12::Close()
		{
			TransitionOutRenderTargets();

			// close the current command list
			DF_D3DErrors::Throw(cmdListPtr[0]->Close());
		}
		
		void DF_CommandList12::BeginEvent(System::String^ eventName, System::Byte r, System::Byte g, System::Byte b)
		{
			System::IntPtr eventNamePtr = System::Runtime::InteropServices::Marshal::StringToHGlobalAnsi(eventName);
			PCSTR cstrEventName = static_cast<PCSTR>(eventNamePtr.ToPointer());
			PIXBeginEvent(cmdListPtr[0], PIX_COLOR(r, g, b), cstrEventName);
			System::Runtime::InteropServices::Marshal::FreeHGlobal(eventNamePtr);
		}
		
		void DF_CommandList12::EndEvent()
		{
			PIXEndEvent(cmdListPtr[0]);
		}
		
		void DF_CommandList12::OnSwapChainUpdated()
		{
			curAllocatorIndex = CalcAllocatorIndex();
			curRenderTargetCount = 0;
			for (int i = 0; i < DF_12_FRAME_COUNT; i++)
				frameToAllocatorOffset[i] = 0;
		}
	}
}
