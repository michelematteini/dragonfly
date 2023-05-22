#include "DF_Fence12.h"
#include "DF_CommandList12.h"
#include "DF_Resource12.h"
#include "DF_Directx3D12.h"
#include "DF_DescriptorHeap12.h"
#include "DF_PipelineState12.h"
#include "DF_D3D12Device.h"
#include "WICTextureLoader12.h"
#include "DDSTextureLoader12.h"

using namespace msclr::interop;

namespace DragonflyGraphicsWrappers {
	namespace DX12 {

		inline ID3D12Device* DF_D3D12Device::GetDevice()
		{
			return devicePtr[0];
		}

		inline IDXGISwapChain3* DF_D3D12Device::GetSwapChain()
		{
			return swapChainPtr[0];
		}

		inline ID3D12CommandQueue* DF_D3D12Device::GetCmdQueue()
		{
			return cmdQueuePtr[0];
		}

		inline ID3D12RootSignature* DF_D3D12Device::GetRootSignature()
		{
			return rootSignaturePtr[0];
		}

		DF_D3D12Device::DF_D3D12Device(System::IntPtr targetHandle, bool fullScreen, int preferredWidth, int preferredHeight, bool antiAliasing, UINT rootConstantsByteSize, cli::array<DF_SamplerDesc12>^ samplerDescList)
		{
			offlineMode = targetHandle.ToInt64() == 0;
			frameBuffers = gcnew cli::array<DF_Resource12^>(DF_12_FRAME_COUNT);

			// prepare pipeline objects
			{
				CComPtr<IDXGIFactory4> dxgi;
				CreateDXGI(&dxgi);
				CreateDevice(dxgi);
				CreateCommandQueue();
				CreateDefaultHeaps();
				CreateRootSignature(rootConstantsByteSize, samplerDescList);

				if (!offlineMode)
				{
					swapChainPtr = MakeComPtr(IDXGISwapChain3);
					CreateSwapChain(dxgi, targetHandle, preferredWidth, preferredHeight);
					if (fullScreen)
					{
						SetFullscreen(true);
					}
					PrepareFrameBuffers();
										
					// does not support fullscreen barriers with alt+ENTER
					DF_D3DErrors::Throw(dxgi->MakeWindowAssociation((HWND)targetHandle.ToPointer(), DXGI_MWA_NO_ALT_ENTER));
				}
			}

			// prepare frame buffer fence
			frameBufferFence = gcnew DF_Fence12(devicePtr[0], frameIndex);
			frameFenceValues = gcnew cli::array<UINT64>(DF_12_FRAME_COUNT);

			WaitForGPU();
		}

		DF_CommandList12^ DF_D3D12Device::CreateCommandList(int framePadding)
		{
			return gcnew DF_CommandList12(this, srvHeap, framePadding);
		}

		DF_Resource12^ DF_D3D12Device::CreateRenderTarget(UINT width, UINT height, DF_SurfaceFormat format)
		{
			D3D12_RESOURCE_DESC renderTargetDesc;
			renderTargetDesc.Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;
			renderTargetDesc.DepthOrArraySize = 1;
			renderTargetDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
			renderTargetDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;
			renderTargetDesc.Format = DX12Conv::SurfaceToDXGIFORMAT(format);
			renderTargetDesc.Height = height;
			renderTargetDesc.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
			renderTargetDesc.MipLevels = 1;
			renderTargetDesc.SampleDesc.Count = 1;
			renderTargetDesc.SampleDesc.Quality = 0;
			renderTargetDesc.Width = width;
			D3D12_CLEAR_VALUE clearValue = {};
			clearValue.Format = renderTargetDesc.Format;

			return CreateCommittedRes(renderTargetDesc, &clearValue, D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE | D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE, D3D12_HEAP_TYPE_DEFAULT, ResourceViews::RTV | ResourceViews::SRV);
		}

		DF_Resource12^ DF_D3D12Device::CreateDepthBuffer(UINT width, UINT height)
		{
			D3D12_RESOURCE_DESC depthStencilDesc;
			depthStencilDesc.Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;
			depthStencilDesc.DepthOrArraySize = 1;
			depthStencilDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
			depthStencilDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL;
			depthStencilDesc.Format = DXGI_FORMAT_D32_FLOAT;
			depthStencilDesc.Height = height;
			depthStencilDesc.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
			depthStencilDesc.MipLevels = 1;
			depthStencilDesc.SampleDesc.Count = 1;
			depthStencilDesc.SampleDesc.Quality = 0;
			depthStencilDesc.Width = width;
			D3D12_CLEAR_VALUE clearValue = {};
			clearValue.Format = depthStencilDesc.Format;
			clearValue.DepthStencil.Depth = 0.0f;
			clearValue.DepthStencil.Stencil = 0;
			
			return CreateCommittedRes(depthStencilDesc, &clearValue, D3D12_RESOURCE_STATE_DEPTH_WRITE, D3D12_HEAP_TYPE_DEFAULT, ResourceViews::DSV);
		}

		DF_Resource12^ DF_D3D12Device::CreateBuffer(int byteSize, DF_CPUAccess cpuAccess)
		{
			D3D12_RESOURCE_DESC cbufferDesc;
			cbufferDesc.Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;
			cbufferDesc.DepthOrArraySize = 1;
			cbufferDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
			cbufferDesc.Flags = D3D12_RESOURCE_FLAG_NONE;
			cbufferDesc.Format = DXGI_FORMAT_UNKNOWN;
			cbufferDesc.Height = 1;
			cbufferDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
			cbufferDesc.MipLevels = 1;
			cbufferDesc.SampleDesc.Count = 1;
			cbufferDesc.SampleDesc.Quality = 0;
			cbufferDesc.Width = byteSize;

			return CreateCommittedRes(
				cbufferDesc, nullptr, 
				cpuAccess == DF_CPUAccess::Write ? D3D12_RESOURCE_STATE_GENERIC_READ : (cpuAccess == DF_CPUAccess::Read ? D3D12_RESOURCE_STATE_COPY_DEST : D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER),
				cpuAccess == DF_CPUAccess::Write ? D3D12_HEAP_TYPE_UPLOAD : (cpuAccess == DF_CPUAccess::Read ? D3D12_HEAP_TYPE_READBACK : D3D12_HEAP_TYPE_DEFAULT),
				ResourceViews::None
			);
		}

		DF_Resource12^ DF_D3D12Device::CreateVertexBuffer(int vertexByteSize, int vertexCount, DF_CPUAccess cpuAccess)
		{
			int byteSize = vertexByteSize * vertexCount;
			DF_Resource12^ vbResource = CreateBuffer(byteSize, cpuAccess);
			vbResource->PrepareVBV(vertexByteSize, byteSize);
			return vbResource;
		}

		DF_Resource12^ DF_D3D12Device::CreateIndexBuffer(int indexCount, DF_CPUAccess cpuAccess)
		{
			int byteSize = sizeof(WORD) * indexCount;
			DF_Resource12^ ibResource = CreateBuffer(byteSize, cpuAccess);
			ibResource->PrepareIBV(byteSize);
			return ibResource;
		}

		DF_PipelineState12^ DF_D3D12Device::CreatePSO(DF_PSODesc12 psoDesc)
		{
			pin_ptr<System::Byte> pinPtrVS = &psoDesc.CompiledVS[psoDesc.CompiledVSFirstByteIndex];
			pin_ptr<System::Byte> pinPtrPS = &psoDesc.CompiledPS[psoDesc.CompiledPSFirstByteIndex];

			// Create dx12 input layout declaration
			D3D12_INPUT_ELEMENT_DESC* dx12InputElems = new D3D12_INPUT_ELEMENT_DESC[psoDesc.InputLayout->Length];

			for (int i = 0; i < psoDesc.InputLayout->Length; i++)
			{
				dx12InputElems[i] = {
					psoDesc.InputLayout[i].Usage == DF_DeclUsage::Position ? "SV_POSITION" : "TEXCOORD",
					psoDesc.InputLayout[i].UsageIndex,
					DX12Conv::DeclTypeToDXGIFORMAT[System::Convert::ToInt32(psoDesc.InputLayout[i].Type)],
					psoDesc.InputLayout[i].Stream,
					psoDesc.InputLayout[i].Offset,
					psoDesc.InputLayout[i].Stream > 0 ? D3D12_INPUT_CLASSIFICATION_PER_INSTANCE_DATA : D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
					psoDesc.InputLayout[i].Stream > 0 ? 1u : 0
				};
			}
			D3D12_INPUT_LAYOUT_DESC inputLayoutDesc;
			inputLayoutDesc.pInputElementDescs = dx12InputElems;
			inputLayoutDesc.NumElements = psoDesc.InputLayout->Length;

			// Describe and create the graphics pipeline state objects (PSO).
			D3D12_GRAPHICS_PIPELINE_STATE_DESC dx12PsoDesc = {};
			dx12PsoDesc.InputLayout = inputLayoutDesc;
			dx12PsoDesc.pRootSignature = rootSignaturePtr[0];
			dx12PsoDesc.VS = CD3DX12_SHADER_BYTECODE(pinPtrVS, psoDesc.CompiledVSByteLength);
			dx12PsoDesc.PS = CD3DX12_SHADER_BYTECODE(pinPtrPS, psoDesc.CompiledPSByteLength);
			dx12PsoDesc.RasterizerState = CD3DX12_RASTERIZER_DESC(D3D12_DEFAULT);
			dx12PsoDesc.RasterizerState.CullMode = static_cast<D3D12_CULL_MODE>(psoDesc.CullMode);
			dx12PsoDesc.RasterizerState.FillMode = static_cast<D3D12_FILL_MODE>(psoDesc.FillMode);
			dx12PsoDesc.BlendState = CD3DX12_BLEND_DESC(D3D12_DEFAULT);
			dx12PsoDesc.BlendState.IndependentBlendEnable = FALSE;
			dx12PsoDesc.BlendState.RenderTarget[0].BlendEnable = psoDesc.BlendEnable;
			dx12PsoDesc.BlendState.RenderTarget[0].SrcBlend = static_cast<D3D12_BLEND>(psoDesc.SrcBlend);
			dx12PsoDesc.BlendState.RenderTarget[0].DestBlend = static_cast<D3D12_BLEND>(psoDesc.DestBlend);
			dx12PsoDesc.DepthStencilState = CD3DX12_DEPTH_STENCIL_DESC(D3D12_DEFAULT);
			dx12PsoDesc.DepthStencilState.DepthEnable = psoDesc.DepthEnabled ? TRUE : FALSE;
			dx12PsoDesc.DepthStencilState.DepthWriteMask = psoDesc.DepthWriteEnabled ? D3D12_DEPTH_WRITE_MASK_ALL : D3D12_DEPTH_WRITE_MASK_ZERO;
			dx12PsoDesc.DepthStencilState.DepthFunc = static_cast<D3D12_COMPARISON_FUNC>(psoDesc.DepthTest);
			dx12PsoDesc.DSVFormat = (psoDesc.DepthEnabled || psoDesc.DepthWriteEnabled) ? DXGI_FORMAT_D32_FLOAT : DXGI_FORMAT_UNKNOWN;
			dx12PsoDesc.DepthStencilState.StencilEnable = FALSE;
			dx12PsoDesc.SampleMask = UINT_MAX;
			dx12PsoDesc.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
			dx12PsoDesc.NumRenderTargets = psoDesc.RenderTargetCount;
			for (int i = 0; i < psoDesc.RenderTargetCount; i++)
				dx12PsoDesc.RTVFormats[i] = DX12Conv::SurfaceToDXGIFORMAT(psoDesc.RenderTargetFormats[i]);
			dx12PsoDesc.SampleDesc.Count = 1;

			// Create the pipeline state
			ID3D12PipelineState* psoResource;
			DF_D3DErrors::Throw(devicePtr[0]->CreateGraphicsPipelineState(&dx12PsoDesc, IID_PPV_ARGS(&psoResource)));

			delete[] dx12InputElems;

			return gcnew DF_PipelineState12(psoResource);
		}

		DF_Resource12^ DF_D3D12Device::CreateTexture(cli::array<System::Byte>^ fileBytes, bool isDDS, DF_CommandList12^ copyCommandList, DF_Resource12^% uploadResource, int% width, int% height)
		{
			pin_ptr<System::Byte> fileBytesPtr = &fileBytes[0];
			DF_Resource12^ managedTex = gcnew DF_Resource12(NULL, D3D12_RESOURCE_STATE_COPY_DEST);
			std::vector<D3D12_SUBRESOURCE_DATA> subresources;
			std::unique_ptr<uint8_t[]> wicData;

			if (isDDS)
			{
				// create texture resource on a default heap
				DF_D3DErrors::Throw(DirectX::LoadDDSTextureFromMemory(GetDevice(), fileBytesPtr, fileBytes->Length, managedTex->GetResourcePtr(), subresources));
			}
			else
			{
				// create texture resource on a default heap (with no mipmaps)
				subresources.resize(1);
				DF_D3DErrors::Throw(DirectX::LoadWICTextureFromMemoryEx(GetDevice(), fileBytesPtr, fileBytes->Length, 0Ui64, D3D12_RESOURCE_FLAG_NONE, DirectX::WIC_LOADER_IGNORE_SRGB, managedTex->GetResourcePtr(), wicData, subresources[0]));
			}

			// create an updload resource to copy the texture data to the default resource
			const UINT64 uploadBufferSize = GetRequiredIntermediateSize(managedTex->GetResource(), 0, static_cast<UINT>(subresources.size()));
			uploadResource = CreateCommittedRes(CD3DX12_RESOURCE_DESC::Buffer(uploadBufferSize), nullptr, D3D12_RESOURCE_STATE_GENERIC_READ, D3D12_HEAP_TYPE_UPLOAD, ResourceViews::None);

			// use the specified command list to update the texture data
			UpdateSubresources(copyCommandList->GetList(), managedTex->GetResource(), uploadResource->GetResource(), 0, 0, static_cast<UINT>(subresources.size()), subresources.data());

			// sync for update completion before usage
			BarrierGroup barriers(copyCommandList->GetList());
			managedTex->AddTransitionIfNeeded(D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE | D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE, barriers);
			barriers.Commit();

			// prepare views
			managedTex->PrepareSRV(GetDevice(), srvHeap);

			// retrieve dimensions
			D3D12_RESOURCE_DESC texDesc = managedTex->GetResource()->GetDesc();
			width = (int)texDesc.Width;
			height = (int)texDesc.Height;

			return managedTex;
		}

		DF_Resource12^ DF_D3D12Device::CreateTexture(UINT width, UINT height, DF_SurfaceFormat format)
		{
			// create texture on the default heap
			DF_Resource12^ managedTex = CreateCommittedRes(
				CD3DX12_RESOURCE_DESC::Tex2D(DX12Conv::SurfaceToDXGIFORMAT(format), width, height, 1U, 1U), 
				nullptr, 
				D3D12_RESOURCE_STATE_COPY_DEST, 
				D3D12_HEAP_TYPE_DEFAULT, 
				ResourceViews::SRV
			);
			
			return managedTex;
		}

		void DF_D3D12Device::ExecuteCommandLists(cli::array<DF_CommandList12^>^ lists, int count)
		{
			ID3D12CommandList** clists = new ID3D12CommandList * [count];
			for (int i = 0; i < count; i++)
				clists[i] = lists[i]->GetList();
			cmdQueuePtr[0]->ExecuteCommandLists((UINT)count, clists);
			delete[] clists;
		}

		void DF_D3D12Device::ExecuteCommandList(DF_CommandList12^ list)
		{
			ID3D12CommandList* cmdList = list->GetList();
			cmdQueuePtr[0]->ExecuteCommandLists(1, &cmdList);
		}

		DF_Resource12^ DF_D3D12Device::GetBackBuffer()
		{
			return frameBuffers[frameIndex];
		}

		DF_Resource12^ DF_D3D12Device::GetBackBufferDepth()
		{
			return defaultDepth;
		}

		int DF_D3D12Device::GetBackBufferIndex()
		{
			return frameIndex;
		}

		void DF_D3D12Device::Present()
		{
			// present
			DF_D3DErrors::Throw(swapChainPtr[0]->Present(0, 0));

			// signal this frame termination and save its ID
			QueueGpuSignalTo(frameBufferFence);
			frameFenceValues[frameIndex] = frameBufferFence->GetLastSignalID();

			// move to the next frame
			frameIndex = swapChainPtr[0]->GetCurrentBackBufferIndex();

			// wait the completion of the frame previously rendered to this frame buffer
			if (frameBufferFence->GetCompletedValue() < frameFenceValues[frameIndex])
				frameBufferFence->Wait(frameFenceValues[frameIndex]);
		}

		void DF_D3D12Device::QueueGpuSignalTo(DF_Fence12^ fence)
		{
			fence->QueueSignal(cmdQueuePtr[0]);
		}

		void DF_D3D12Device::Resize(UINT width, UINT height)
		{
			WaitForGPU();
			ReleaseFrameBuffers();
			DF_D3DErrors::Throw(GetSwapChain()->ResizeBuffers(0, width, height, DXGI_FORMAT_UNKNOWN, DF_12_SWAPCHAIN_FLAGS));
			PrepareFrameBuffers();

			// update current frame id
			frameIndex = swapChainPtr[0]->GetCurrentBackBufferIndex();

			WaitForGPU();
		}

		DF_Resource12^ DF_D3D12Device::CreateCommittedRes(const D3D12_RESOURCE_DESC& desc, const D3D12_CLEAR_VALUE* clearValue, D3D12_RESOURCE_STATES initialState, D3D12_HEAP_TYPE heapType, ResourceViews requiredViews)
		{
			// create as a committed resource
			ID3D12Resource* committedRes;
			DF_D3DErrors::Throw(devicePtr[0]->CreateCommittedResource(
				&CD3DX12_HEAP_PROPERTIES(heapType),
				D3D12_HEAP_FLAG_NONE,
				&desc,
				initialState,
				clearValue,
				IID_PPV_ARGS(&committedRes)));
			DF_Resource12^ managedRes = gcnew DF_Resource12(committedRes, initialState);

			// prepare views
			if (HAS_FLAG(requiredViews, ResourceViews::RTV))
				managedRes->PrepareRTV(devicePtr[0], rtvHeap);
			if (HAS_FLAG(requiredViews, ResourceViews::SRV))
				managedRes->PrepareSRV(devicePtr[0], srvHeap);
			if (HAS_FLAG(requiredViews, ResourceViews::DSV))
				managedRes->PrepareDSV(devicePtr[0], dsvHeap);
			if (HAS_FLAG(requiredViews, ResourceViews::CBV))
				managedRes->PrepareCBV(devicePtr[0], nullptr);

			return managedRes;
		}

		void DF_D3D12Device::WaitForGPU()
		{
			if (!frameBufferFence->IsReleased())
			{
				QueueGpuSignalTo(frameBufferFence);
				frameBufferFence->Wait();
			}
		}

		void DF_D3D12Device::CreateDXGI(IDXGIFactory4** dxgi)
		{
			UINT dxgiFactoryFlags = 0;
#if DEBUG_LAYER
			dxgiFactoryFlags |= DXGI_CREATE_FACTORY_DEBUG; // Enable additional debug layers.
#endif
														   // create dxgi factory
			DF_D3DErrors::Throw(CreateDXGIFactory2(dxgiFactoryFlags, IID_PPV_ARGS(dxgi)));
		}

		void DF_D3D12Device::CreateDevice(IDXGIFactory4* dxgi)
		{
#if DEBUG_LAYER
			// Enable the debug layer (requires the Graphics Tools "optional feature").
			// NOTE: Enabling the debug layer after device creation will invalidate the active device.
			{
				CComPtr<ID3D12Debug> debugController;
				if (DF_D3DErrors::Check(D3D12GetDebugInterface(IID_PPV_ARGS(&debugController))))
					debugController->EnableDebugLayer();
			}
#endif

			CComPtr<IDXGIAdapter1> hardwareAdapter;
			DF_Directx3D12::GetHardwareAdapter(dxgi, &hardwareAdapter, true);
			devicePtr = MakeComPtr(ID3D12Device);
			DF_D3DErrors::Throw(D3D12CreateDevice(hardwareAdapter, DF_12_FEATURE_LEVEL, IID_PPV_ARGS(devicePtr)));


#ifdef DEBUG_LAYER
			// filter out unwanted debug messages
			{
				CComPtr<ID3D12InfoQueue> debugInfo;
				if (DF_D3DErrors::Check(devicePtr[0]->QueryInterface<ID3D12InfoQueue>(&debugInfo)))
				{
					D3D12_MESSAGE_ID hide[] =
					{
						D3D12_MESSAGE_ID_CLEARRENDERTARGETVIEW_MISMATCHINGCLEARVALUE,
						// TODO: Add other messages to be ignored here
					};
					D3D12_INFO_QUEUE_FILTER filter;
					memset(&filter, 0, sizeof(filter));
					filter.DenyList.NumIDs = _countof(hide);
					filter.DenyList.pIDList = hide;
					debugInfo->AddStorageFilterEntries(&filter);
				}

			}
#endif
		}

		void DF_D3D12Device::CreateCommandQueue()
		{
			// Describe and create the command queue.
			D3D12_COMMAND_QUEUE_DESC queueDesc = {};
			queueDesc.Flags = D3D12_COMMAND_QUEUE_FLAG_NONE;
			queueDesc.Type = D3D12_COMMAND_LIST_TYPE_DIRECT;
			cmdQueuePtr = MakeComPtr(ID3D12CommandQueue);
			DF_D3DErrors::Throw(devicePtr[0]->CreateCommandQueue(&queueDesc, IID_PPV_ARGS(cmdQueuePtr)));
		}

		void DF_D3D12Device::CreateSwapChain(IDXGIFactory4* dxgi, System::IntPtr targetHandle, int width, int height)
		{
			// Describe and create the swap chain.
			DXGI_SWAP_CHAIN_DESC1 swapChainDesc = {};
			swapChainDesc.BufferCount = DF_12_FRAME_COUNT;
			swapChainDesc.Width = width;
			swapChainDesc.Height = height;
			swapChainDesc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
			swapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
			swapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD;
			swapChainDesc.SampleDesc.Count = 1;
			swapChainDesc.Flags = DF_12_SWAPCHAIN_FLAGS;

			CComPtr<IDXGISwapChain1> swapChain;
			DF_D3DErrors::Throw(dxgi->CreateSwapChainForHwnd(
				cmdQueuePtr[0],        // Swap chain needs the queue so that it can force a flush on it.
				(HWND)targetHandle.ToPointer(),
				&swapChainDesc,
				nullptr,
				nullptr,
				&swapChain
			));
			
			DF_D3DErrors::Throw(swapChain->QueryInterface(__uuidof(IDXGISwapChain3), reinterpret_cast<void**>(swapChainPtr)));

			// update current frame id
			frameIndex = swapChainPtr[0]->GetCurrentBackBufferIndex();
		}

		void DF_D3D12Device::CreateDefaultHeaps()
		{
			// prepare render target descriptor heap
			rtvHeap = gcnew DF_DescriptorHeap12(devicePtr[0], D3D12_DESCRIPTOR_HEAP_TYPE_RTV, DF_12_MAX_RT_COUNT);
			// prepare depth stencil view descriptor heap
			dsvHeap = gcnew DF_DescriptorHeap12(devicePtr[0], D3D12_DESCRIPTOR_HEAP_TYPE_DSV, DF_12_MAX_DS_COUNT);
			// prepare shader resource view descriptor heap
			srvHeap = gcnew DF_DescriptorHeap12(devicePtr[0], D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV, DF_12_MAX_TEX_COUNT);
		}

		void DF_D3D12Device::CreateRootSignature(UINT rootConstantsByteSize, cli::array<DF_SamplerDesc12>^ samplerDescList)
		{
			// prepare root signature parameters
			rootConstantsByteSize = max(4, rootConstantsByteSize); // add a constant even if not required to preserve root param indices

			CD3DX12_ROOT_PARAMETER1* rootParams = new CD3DX12_ROOT_PARAMETER1[RS_PARAMID_COUNT];
			rootParams[RS_PARAMID_CONSTANTS].InitAsConstants(rootConstantsByteSize / 4, 2); // global dynamic 
			rootParams[RS_PARAMID_CBV_GLOBALS].InitAsConstantBufferView(0, 0, D3D12_ROOT_DESCRIPTOR_FLAG_DATA_VOLATILE); // global
			rootParams[RS_PARAMID_CBV_MATERIAL].InitAsConstantBufferView(1, 0, D3D12_ROOT_DESCRIPTOR_FLAG_DATA_VOLATILE); // material locals
			CD3DX12_DESCRIPTOR_RANGE1 textureTable;
			textureTable.Init(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, -1, 0, 0, D3D12_DESCRIPTOR_RANGE_FLAG_DESCRIPTORS_VOLATILE); // t0-unbounded
			rootParams[RS_PARAMID_UNBOUNDED_TEX_TABLE].InitAsDescriptorTable(1, &textureTable); // unbounded texture SRVs

																								// prepare static samplers
			CD3DX12_STATIC_SAMPLER_DESC* staticSamplers = new CD3DX12_STATIC_SAMPLER_DESC[samplerDescList->Length];
			for (int i = 0; i < samplerDescList->Length; i++)
			{
				D3D12_FILTER filter = static_cast<D3D12_FILTER>(samplerDescList[i].Filter);
				D3D12_TEXTURE_ADDRESS_MODE addressMode = static_cast<D3D12_TEXTURE_ADDRESS_MODE>(samplerDescList[i].AddressType);
				float mipLodBias = filter == D3D12_FILTER_MIN_MAG_MIP_POINT ? 0.0f : -0.9f; // sharper mipmapping
				float maxLod = samplerDescList[i].MipMaps ? D3D12_FLOAT32_MAX : 0.0f;
				D3D12_STATIC_BORDER_COLOR borderColor = static_cast<D3D12_STATIC_BORDER_COLOR>(samplerDescList[i].BorderColor);
				staticSamplers[i].Init((UINT)i, filter, addressMode, addressMode, addressMode, mipLodBias, 16U, D3D12_COMPARISON_FUNC_LESS_EQUAL, borderColor, 0.0f, maxLod);
			}

			// compile the root signature
			D3D12_ROOT_SIGNATURE_FLAGS rootSignatureFlags =
				D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT |
				D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS |
				D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS |
				D3D12_ROOT_SIGNATURE_FLAG_DENY_GEOMETRY_SHADER_ROOT_ACCESS;
			CD3DX12_VERSIONED_ROOT_SIGNATURE_DESC rootSignDesc(RS_PARAMID_COUNT, rootParams, (UINT)samplerDescList->Length, staticSamplers, rootSignatureFlags);
			CComPtr<ID3DBlob> serializedRootSig;
			CComPtr<ID3DBlob> serializationErrors;
			DF_D3DErrors::Throw(D3D12SerializeVersionedRootSignature(&rootSignDesc, &serializedRootSig, &serializationErrors));

			// create the root signature
			rootSignaturePtr = MakeComPtr(ID3D12RootSignature);
			DF_D3DErrors::Throw(devicePtr[0]->CreateRootSignature(0, serializedRootSig->GetBufferPointer(), serializedRootSig->GetBufferSize(), IID_PPV_ARGS(rootSignaturePtr)));

			delete[] rootParams;
			delete[] samplerDescList;
		}


		void DF_D3D12Device::ReleaseSwapChain()
		{
			if (!swapChainPtr[0])
				return;

			WaitForGPU();

			// destroy current swap chain
			DF_D3DErrors::Throw(GetSwapChain()->SetFullscreenState(false, NULL));
			ReleaseFrameBuffers();
			swapChainPtr[0]->Release();
			swapChainPtr[0] = NULL; // to prevent parent destructor to release it again 
		}

		void DF_D3D12Device::UpdateSwapChain(System::IntPtr targetHandle, bool fullScreen, int width, int height)
		{
			ReleaseSwapChain();
			CComPtr<IDXGIFactory4> dxgi;
			CreateDXGI(&dxgi);
			CreateSwapChain(dxgi, targetHandle, width, height);
			if (fullScreen)
				SetFullscreen(true);			
			PrepareFrameBuffers();		
			WaitForGPU();
		}

		void DF_D3D12Device::SetFullscreen(bool enabled)
		{
			DXGI_SWAP_CHAIN_DESC1 sd;
			GetSwapChain()->GetDesc1(&sd);
			DF_D3DErrors::Throw(GetSwapChain()->SetFullscreenState(enabled, NULL));
			Resize(sd.Width, sd.Height);
		}

		void DF_D3D12Device::Release()
		{
			if (!IsReleased())
			{
				ReleaseSwapChain();
				WaitForGPU();
			}

			DF_Resource::Release();
		}

		void DF_D3D12Device::PrepareFrameBuffers()
		{
			if (frameBuffers[0])
				return; // already prepared

			// create frame buffer resources
			for (UINT i = 0; i < DF_12_FRAME_COUNT; i++)
			{
				ID3D12Resource* frameBuffer;
				DF_D3DErrors::Throw(swapChainPtr[0]->GetBuffer(i, IID_PPV_ARGS(&frameBuffer)));
				frameBuffers[i] = gcnew DF_Resource12(frameBuffer, D3D12_RESOURCE_STATE_COMMON);
				frameBuffers[i]->PrepareRTV(devicePtr[0], rtvHeap);
				frameBuffers[i]->Flags = frameBuffers[i]->Flags | DF_Resource12Flags::FrameBuffer;
			}

			// create a default depth for the frame buffer
			D3D12_RESOURCE_DESC frameDesc = frameBuffers[0]->GetResource()->GetDesc();
			defaultDepth = CreateDepthBuffer((UINT)frameDesc.Width, (UINT)frameDesc.Height);
		}
		
		void DF_D3D12Device::ReleaseFrameBuffers()
		{
			if (!frameBuffers[0])
				return; // already released

			for (UINT i = 0; i < DF_12_FRAME_COUNT; i++)
			{
				frameBuffers[i]->Release();
				frameBuffers[i] = nullptr;
			}

			defaultDepth->Release();
			defaultDepth = nullptr;
		}

		System::Collections::Generic::List<DF_DisplayMode>^ DF_D3D12Device::GetDisplayModes()
		{
			if (offlineMode)
				return gcnew System::Collections::Generic::List<DF_DisplayMode>();

			CComPtr<IDXGIOutput> output;
			DF_D3DErrors::Throw(GetSwapChain()->GetContainingOutput(&output));

			return DF_Display::GetDisplayModesFromOutput(output);
		}

	}
}
