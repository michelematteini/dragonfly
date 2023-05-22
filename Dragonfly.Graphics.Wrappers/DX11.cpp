#include "DirectxCommon.h"
#include <new>
#include <d3d11_4.h>
#include "atlbase.h"
#include <D3Dcompiler.h>
#include <msclr/marshal.h>
#include "DDSTextureLoader11.h"
#include "WICTextureLoader11.h"
#include <wincodec.h>
#include "ScreenGrab11.h"

using namespace std;
using namespace System;
using namespace DragonflyGraphicsWrappers;
using namespace msclr::interop;
using namespace System::Runtime::InteropServices;

#undef UpdateResource

namespace DragonflyGraphicsWrappers {

	namespace DX11 {

#define DF_11_FEATURE_LEVEL D3D_FEATURE_LEVEL_11_1

// uncomment to enable flip-mode presentation. Known Problems:
// -After using flip-mode on a HWND, other APIs can't use that hwnd again 
// (see https://docs.microsoft.com/en-us/windows/win32/api/dxgi/ne-dxgi-dxgi_swap_effect)
//#define FLIP_MODE_SWAPCHAIN 

#pragma region ENUMS

		public enum class DF_Usage11
		{
			Default = D3D11_USAGE_DEFAULT,
			Immutable = D3D11_USAGE_IMMUTABLE,
			Dynamic = D3D11_USAGE_DYNAMIC,
			Staging = D3D11_USAGE_STAGING,
		};

		public enum class DF_TexBinding
		{
			None = 0,
			ShaderResource = D3D11_BIND_SHADER_RESOURCE,
			RenderTarget = D3D11_BIND_RENDER_TARGET,
			DepthStencil = D3D11_BIND_DEPTH_STENCIL
		};

		public enum class DF_SamplAddressMode
		{
			ShaderResource = D3D11_BIND_SHADER_RESOURCE,
			RenderTarget = D3D11_BIND_RENDER_TARGET,
			DepthStencil = D3D11_BIND_DEPTH_STENCIL
		};

		public enum class DF_TextureFilterType11
		{
			Point = D3D11_FILTER_MIN_MAG_MIP_POINT,
			MinMagPointMipLinear = D3D11_FILTER_MIN_MAG_POINT_MIP_LINEAR,
			MinPointMagMipLinear = D3D11_FILTER_MIN_POINT_MAG_MIP_LINEAR,
			MinLinearMagMipPoint = D3D11_FILTER_MIN_LINEAR_MAG_MIP_POINT,
			MinLinearMagPointMipLinear = D3D11_FILTER_MIN_LINEAR_MAG_POINT_MIP_LINEAR,
			MinMagLinearMipPoint = D3D11_FILTER_MIN_MAG_LINEAR_MIP_POINT,
			Linear = D3D11_FILTER_MIN_MAG_MIP_LINEAR,
			Anisotropic = D3D11_FILTER_ANISOTROPIC,
		};


#pragma endregion

#pragma region STRUCTURES

		public value struct DF_SamplerDesc
		{
		public:
			DF_TextureAddress AddressX, AddressY, AddressZ;
			DF_TextureFilterType11 Filter;
			float BorderR, BorderG, BorderB, BorderA;
		};

#pragma endregion

#pragma region FORWARD DECLARATIONS

		//directx 11 main classes
		ref class DF_Directx3D11;
		ref class DF_D3D11Device;

#pragma endregion

#pragma region UTILS AND CONVERSIONS

		public ref class DX11Conv abstract sealed
		{
		public:
			static cli::array<DXGI_FORMAT> ^ DeclTypeToDXGIFORMAT = gcnew cli::array<DXGI_FORMAT> {
				DXGI_FORMAT_UNKNOWN,
				DXGI_FORMAT_R32_FLOAT,
				DXGI_FORMAT_R32G32_FLOAT,
				DXGI_FORMAT_R32G32B32_FLOAT,
				DXGI_FORMAT_R32G32B32A32_FLOAT
			};


			static DXGI_FORMAT SurfaceToDXGIFORMAT(DF_SurfaceFormat format)
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

		};

#pragma endregion

#pragma region RESOURCE CLASSES

		/*=============================================*/
		/*       VERTEX SHADER                         */
		/*=============================================*/
		public ref class DF_VertexShader11 : public DF_Resource
		{
		internal:
			DF_VertexShader11(ID3D11VertexShader * vs) : DF_Resource(vs) { }
		};

		/*=============================================*/
		/*       PIXEL  SHADER                         */
		/*=============================================*/
		public ref class DF_PixelShader11 : public DF_Resource
		{
		internal:
			DF_PixelShader11(ID3D11PixelShader * vs) : DF_Resource(vs) { }
		};

		/*=======================================================*/
		/*                INPUT LAYOUT                           */
		/* Informations on the structure of a custom vertex      */
		/*=======================================================*/
		public ref class DF_InputLayout : public DF_Resource
		{
		internal:
			DF_InputLayout(ID3D11InputLayout * inputLayout) : DF_Resource(inputLayout) { }
		};

		/*=============================================*/
		/*            RESOURCE                         */
		/*                  	                       */
		/*=============================================*/
		public ref class DF_Resource11 : public DF_Resource
		{
		internal:
			DF_Resource11(ID3D11Resource * state) : DF_Resource(state) { }

			ID3D11Resource * GetResource()
			{
				return static_cast<ID3D11Resource *>(comPtrList[0]);
			}

		};

		/*=============================================*/
		/*            BUFFER                           */
		/*=============================================*/
		public ref class DF_Buffer11 : public DF_Resource11
		{
		private:
			UINT byteSize;

		internal:
			DF_Buffer11(ID3D11Buffer * buffer, UINT byteSize) : DF_Resource11(buffer) 
			{
				this->byteSize = byteSize;
			}

		public:
			int GetByteSize()
			{
				return byteSize;
			}
		};

		/*=============================================*/
		/*            RASTER STATE                     */
		/*=============================================*/
		public ref class DF_RasterState : public DF_Resource
		{
		internal:
			DF_RasterState(ID3D11RasterizerState1 * state) : DF_Resource(state) { }
		};

		/*=============================================*/
		/*            BLEND STATE                      */
		/*=============================================*/
		public ref class DF_BlendState : public DF_Resource
		{
		internal:
			DF_BlendState(ID3D11BlendState1 * state) : DF_Resource(state) { }
		};

		/*=============================================*/
		/*            SAMPLER STATE                    */
		/*=============================================*/
		public ref class DF_SamplerState : public DF_Resource
		{
		internal:
			DF_SamplerState(ID3D11SamplerState * state) : DF_Resource(state) { }
		};

		/*=============================================*/
		/*            DEPTH-STENCIL STATE              */
		/*=============================================*/
		public ref class DF_DepthStencilState : public DF_Resource
		{
		internal:
			DF_DepthStencilState(ID3D11DepthStencilState * state) : DF_Resource(state) { }
		};

		/*=============================================*/
		/*            TEXTURE                          */
		/*=============================================*/
		public ref class DF_Texture11 : public DF_Resource11
		{
		internal:
			DF_Texture11(ID3D11Texture2D1 * texResource) : DF_Resource11(texResource)
			{
				AddComPtr(NULL); 
				AddComPtr(NULL);
				AddComPtr(NULL);
			}

			ID3D11Texture2D1 * GetTex()
			{
				return static_cast<ID3D11Texture2D1 *>(comPtrList[0]);
			}

			void PrepareShaderView(ID3D11Device * device)
			{
				ID3D11ShaderResourceView * textureView = static_cast<ID3D11ShaderResourceView *>(comPtrList[1]);

				if (!comPtrList[1])
				{
					// create shader view
					ID3D11Texture2D1* texture = GetTex();
					D3D11_TEXTURE2D_DESC texInfo;
					texture->GetDesc(&texInfo);
					D3D11_SHADER_RESOURCE_VIEW_DESC rsrvDesc = CD3D11_SHADER_RESOURCE_VIEW_DESC(texture, D3D11_SRV_DIMENSION_TEXTURE2D, texInfo.Format, 0U, texInfo.MipLevels);
					DF_D3DErrors::Throw(device->CreateShaderResourceView(texture, &rsrvDesc, &textureView));
					comPtrList[1] = textureView;
				}
			}

			ID3D11ShaderResourceView* GetShaderView()
			{
				return static_cast<ID3D11ShaderResourceView*>(comPtrList[1]);
			}

			void PrepareDepthStencilView(ID3D11Device * device)
			{
				ID3D11DepthStencilView * dsView = static_cast<ID3D11DepthStencilView *>(comPtrList[2]);

				if (!comPtrList[2])
				{
					// create shader view
					ID3D11Texture2D1* texture = GetTex();
					D3D11_TEXTURE2D_DESC texInfo;
					texture->GetDesc(&texInfo);
					D3D11_DEPTH_STENCIL_VIEW_DESC descDSV = {};
					descDSV.Format = texInfo.Format;
					descDSV.ViewDimension = D3D11_DSV_DIMENSION_TEXTURE2D;
					descDSV.Texture2D.MipSlice = 0;
					DF_D3DErrors::Throw(device->CreateDepthStencilView(texture, &descDSV, &dsView));
					comPtrList[2] = dsView;
				}
			}

			ID3D11DepthStencilView * GetDepthStencilView()
			{
				return static_cast<ID3D11DepthStencilView*>(comPtrList[2]);
			}

			void PrepareRenderTargetView(ID3D11Device * device)
			{
				ID3D11RenderTargetView * rtView = static_cast<ID3D11RenderTargetView *>(comPtrList[3]);
			
				if (!comPtrList[3])
				{
					// create rt view
					ID3D11Texture2D1 * texture = GetTex();
					DF_D3DErrors::Throw(device->CreateRenderTargetView(texture, nullptr, &rtView));
					comPtrList[3] = rtView;
				}
			}

			ID3D11RenderTargetView* GetRenderTargetView()
			{
				return static_cast<ID3D11RenderTargetView*>(comPtrList[3]);
			}

			D3D11_TEXTURE2D_DESC GetTextureDesc()
			{
				D3D11_TEXTURE2D_DESC texInfo;
				GetTex()->GetDesc(&texInfo);
				return texInfo;
			}

		public:
			int GetWidth()
			{
				return GetTextureDesc().Width;
			}

			int GetHeight()
			{
				return GetTextureDesc().Height;
			}			

		};



#pragma endregion

		/// <summary>
		/// This class includes methods for creating the D3d11 device, enumerating and retrieving his capabilities.
		/// </summary>
		public ref class DF_Directx3D11 abstract sealed
		{
		public:
			static bool IsAvailable()
			{
				D3D_FEATURE_LEVEL featureLevels[] = { DF_11_FEATURE_LEVEL };
				UINT numFeatureLevels = ARRAYSIZE(featureLevels);
				D3D_FEATURE_LEVEL supportedLevel;
				return DF_D3DErrors::Check(D3D11CreateDevice(nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, 0, featureLevels, numFeatureLevels, D3D11_SDK_VERSION, NULL, &supportedLevel, NULL));;
			}

			static System::Collections::Generic::List<DF_DisplayMode>^ GetDefaultDisplayModes()
			{
				CComPtr<IDXGIOutput> defaultOutput;
				DF_Display::GetDefaultOutput(&defaultOutput);
				return DF_Display::GetDisplayModesFromOutput(defaultOutput);
			}


		}; // DF_Directx3D11

		/// <summary>
		/// A context used to execute commands on the GPU.
		/// </summary>
		public ref class DF_D3D11DeviceContext : public DF_Resource
		{
		private:
			ID3D11ShaderResourceView** emptyBindings;
			int ctxID, cmdListID;

		internal:
			DF_D3D11DeviceContext() : DF_Resource(NULL) 
			{
				emptyBindings = new ID3D11ShaderResourceView * [128];
				ctxID = 0;
				cmdListID = AddComPtr(NULL);
			}
			
			ID3D11DeviceContext3* GetContext() 
			{
				return reinterpret_cast<ID3D11DeviceContext3*>(comPtrList[ctxID]);
			}

			ID3D11DeviceContext3** GetContextPtr()
			{
				return reinterpret_cast<ID3D11DeviceContext3**>(&comPtrList[ctxID]);
			}

			ID3D11CommandList* GetCmdList()
			{
				return reinterpret_cast<ID3D11CommandList*>(comPtrList[cmdListID]);
			}

			ID3D11CommandList** GetCmdListPtr()
			{
				return reinterpret_cast<ID3D11CommandList**>(&comPtrList[cmdListID]);
			}

			bool IsEmulated = false;

		public:

			virtual ~DF_D3D11DeviceContext() 
			{ 
				delete[] emptyBindings;
			}

			void Clear(DF_Texture11^ renderTarget, float r, float g, float b, float a)
			{
				FLOAT clearColor[] = { r, g, b, a };
				GetContext()->ClearRenderTargetView(renderTarget->GetRenderTargetView(), clearColor);
			}

			void ClearView(DF_Texture11^ renderTarget, float r, float g, float b, float a, int fromX, int fromY, int toX, int toY)
			{
				D3D11_RECT clearRect = { fromX, fromY, toX, toY };

				FLOAT clearColor[] = { r, g, b, a };
				GetContext()->ClearView(renderTarget->GetRenderTargetView(), clearColor, &clearRect, 1);
			}

			void ClearDepth(DF_Texture11^ depthBuffer, float depth)
			{
				GetContext()->ClearDepthStencilView(depthBuffer->GetDepthStencilView(), D3D11_CLEAR_DEPTH, depth, 0);
			}

			void SetVertexShader(DF_VertexShader11^ vs)
			{
				ID3D11VertexShader* vsp = vs == nullptr ? NULL : static_cast<ID3D11VertexShader*>(vs->Get());
				GetContext()->VSSetShader(vsp, nullptr, 0);
			}

			void SetPixelShader(DF_PixelShader11^ ps)
			{
				ID3D11PixelShader* psp = ps == nullptr ? NULL : static_cast<ID3D11PixelShader*>(ps->Get());
				GetContext()->PSSetShader(psp, nullptr, 0);
			}

			void SetVSConstantBuffer(DF_Buffer11^ buffer, UINT slot)
			{
				ID3D11Buffer* buff = buffer == nullptr ? NULL : static_cast<ID3D11Buffer*>(buffer->Get());
				GetContext()->VSSetConstantBuffers(slot, 1, &buff);
			}

			void SetVSConstantBuffer(DF_Buffer11^ buffer, UINT slot, UINT startReg, UINT regCount)
			{
				ID3D11Buffer* buff = buffer == nullptr ? NULL : static_cast<ID3D11Buffer*>(buffer->Get());
				GetContext()->VSSetConstantBuffers1(slot, 1, &buff, &startReg, &regCount);
			}

			void SetPSConstantBuffer(DF_Buffer11^ buffer, UINT slot)
			{
				ID3D11Buffer* buff = buffer == nullptr ? NULL : static_cast<ID3D11Buffer*>(buffer->Get());
				GetContext()->PSSetConstantBuffers(slot, 1, &buff);
			}

			void SetPSConstantBuffer(DF_Buffer11^ buffer, UINT slot, UINT startReg, UINT regCount)
			{
				ID3D11Buffer* buff = buffer == nullptr ? NULL : static_cast<ID3D11Buffer*>(buffer->Get());
				GetContext()->PSSetConstantBuffers1(slot, 1, &buff, &startReg, &regCount);
			}

			void SetInputLayout(DF_InputLayout^ inputLayout)
			{
				ID3D11InputLayout* layout = inputLayout == nullptr ? NULL : (ID3D11InputLayout*)inputLayout->Get();
				GetContext()->IASetInputLayout(layout);
			}

			void SetIndexBuffer(DF_Buffer11^ indexBuffer)
			{
				ID3D11Buffer* ib = indexBuffer == nullptr ? NULL : static_cast<ID3D11Buffer*>(indexBuffer->Get());
				GetContext()->IASetIndexBuffer(ib, DXGI_FORMAT_R16_UINT, 0);
			}

			void SetVertexBuffer(UINT slot, DF_Buffer11^ vertexBuffer, UINT byteOffset, UINT vertexSize)
			{
				ID3D11Buffer* vb = vertexBuffer == nullptr ? NULL : static_cast<ID3D11Buffer*>(vertexBuffer->Get());
				GetContext()->IASetVertexBuffers(slot, 1, &vb, &vertexSize, &byteOffset);
			}

			void SetRasterState(DF_RasterState^ state)
			{
				ID3D11RasterizerState* s = static_cast<ID3D11RasterizerState*>(state->Get());
				GetContext()->RSSetState(s);
			}

			void SetBlendState(DF_BlendState^ state)
			{
				ID3D11BlendState1* bs = static_cast<ID3D11BlendState1*>(state->Get());
				GetContext()->OMSetBlendState(bs, NULL, 0xffffffff);
			}

			void SetVSSampler(DF_SamplerState^ sampler, UINT slot)
			{
				ID3D11SamplerState* s = static_cast<ID3D11SamplerState*>(sampler->Get());
				GetContext()->VSSetSamplers(slot, 1, &s);
			}

			void SetPSSampler(DF_SamplerState^ sampler, UINT slot)
			{
				ID3D11SamplerState* s = static_cast<ID3D11SamplerState*>(sampler->Get());
				GetContext()->PSSetSamplers(slot, 1, &s);
			}

			void SetDepthStencilState(DF_DepthStencilState^ state)
			{
				ID3D11DepthStencilState* dss = static_cast<ID3D11DepthStencilState*>(state->Get());
				GetContext()->OMSetDepthStencilState(dss, NULL);
			}

			void SetVSTexture(UINT slot, DF_Texture11^ tex)
			{
				ID3D11ShaderResourceView* t = tex == nullptr ? NULL : tex->GetShaderView();
				GetContext()->VSSetShaderResources(slot, 1, &t);
			}

			void SetPSTexture(UINT slot, DF_Texture11^ tex)
			{
				ID3D11ShaderResourceView* t = tex == nullptr ? NULL : tex->GetShaderView();
				GetContext()->PSSetShaderResources(slot, 1, &t);
			}

			void ClearTextureBindings(UINT fromSlot, UINT count)
			{
				GetContext()->VSGetShaderResources(fromSlot, count, emptyBindings);
				GetContext()->PSGetShaderResources(fromSlot, count, emptyBindings);
			}

			void SetPrimitiveTopology(DF_PrimitiveType topology)
			{
				GetContext()->IASetPrimitiveTopology(static_cast<D3D11_PRIMITIVE_TOPOLOGY>(topology));
			}

			void Draw(UINT vertexCount, UINT startVertex)
			{
				GetContext()->Draw(vertexCount, startVertex);
			}

			void DrawIndexed(UINT indexCount, UINT startIndex)
			{
				GetContext()->DrawIndexed(indexCount, startIndex, 0);
			}

			void DrawInstanced(UINT indexCount, UINT startIndex, UINT instanceCount)
			{
				GetContext()->DrawIndexedInstanced(indexCount, instanceCount, startIndex, 0, 0);
			}

			void DrawInstancedConstOffset(UINT indexCount, UINT startIndex, UINT instanceCount, DF_Buffer11^ cb, UINT cbAddress, UINT cbInstanceRegSize)
			{
				ID3D11DeviceContext3* context = GetContext();
				ID3D11Buffer* buff = static_cast<ID3D11Buffer*>(cb->Get()), * nullBuff = NULL;

				for (UINT i = 0, startReg = 0; i < instanceCount; i++, startReg += cbInstanceRegSize)
				{
					context->VSSetConstantBuffers1(cbAddress, 1, &buff, &startReg, &cbInstanceRegSize);
					context->PSSetConstantBuffers1(cbAddress, 1, &buff, &startReg, &cbInstanceRegSize);
					context->DrawIndexed(indexCount, startIndex, 0);

					// workaround for cmd list emulation bug (see msdn on 
					if (IsEmulated)
					{
						context->VSSetConstantBuffers(cbAddress, 1, &nullBuff);
						context->PSSetConstantBuffers(cbAddress, 1, &nullBuff);
					}
				}
			}

			void SetRenderTargets(DF_Texture11^ depthBuffer, ...cli::array<DF_Texture11^>^ renderTargets)
			{
				ID3D11DepthStencilView* depth = NULL;
				if (depthBuffer)
					depth = depthBuffer->GetDepthStencilView();

				UINT rtCount = 0;
				ID3D11RenderTargetView** rts = new ID3D11RenderTargetView * [renderTargets->Length];
				for (int i = 0; i < renderTargets->Length; i++)
				{
					if (!renderTargets[i]) break;
					rts[i] = renderTargets[i]->GetRenderTargetView();
					rtCount++;
				}

				GetContext()->OMSetRenderTargets(rtCount, rts, depth);
				delete[] rts;

				// update viewport to match the render targets
				if (renderTargets[0])
				{
					D3D11_TEXTURE2D_DESC rtDesc = renderTargets[0]->GetTextureDesc();
					SetViewport(0, 0, rtDesc.Width, rtDesc.Height);
				}
			}

			void SetViewport(UINT x, UINT y, UINT width, UINT height)
			{
				D3D11_VIEWPORT vp;
				vp.Width = (FLOAT)width;
				vp.Height = (FLOAT)height;
				vp.MinDepth = 0.0f;
				vp.MaxDepth = 1.0f;
				vp.TopLeftX = (FLOAT)x;
				vp.TopLeftY = (FLOAT)y;
				GetContext()->RSSetViewports(1, &vp);
			}

			void CopyResource(DF_Resource11^ src, DF_Resource11^ dest)
			{
				GetContext()->CopyResource(dest->GetResource(), src->GetResource());
			}

			generic <typename T> void SetResourceData(DF_Resource11^ res, cli::array<T>^ data)
			{
				pin_ptr<T> pinPtrData = &data[0];

				D3D11_MAPPED_SUBRESOURCE lockedData;
				DF_D3DErrors::Throw(GetContext()->Map(res->GetResource(), 0, D3D11_MAP_WRITE_DISCARD, 0, &lockedData));

				memcpy(lockedData.pData, pinPtrData, data->Length * sizeof(T));

				GetContext()->Unmap(res->GetResource(), 0);
			}

			generic <typename T> void SetResourceData(DF_Resource11^ res, cli::array<T, 2>^ data)
			{
				pin_ptr<T> pinPtrData = &data[0, 0];

				D3D11_MAPPED_SUBRESOURCE lockedData;
				DF_D3DErrors::Throw(GetContext()->Map(res->GetResource(), 0, D3D11_MAP_WRITE_DISCARD, 0, &lockedData));

				byte* gpuData = static_cast<byte*>(lockedData.pData);
				UINT cpuDataPitch = data->GetLength(0) * sizeof(T);
				int cpuDataRows = data->GetLength(1);
				for (int row = 0; row < cpuDataRows; row++)
					memcpy(&gpuData[row * lockedData.RowPitch], &pinPtrData[0, row], cpuDataPitch);

				GetContext()->Unmap(res->GetResource(), 0);
			}

			generic <typename T> bool GetResourceData(DF_Resource11^ res, [Out] cli::array<T>^ data, bool waitForGPU)
			{
				pin_ptr<T> pinPtrData = &data[0];

				D3D11_MAPPED_SUBRESOURCE lockedData;
				HRESULT mapResult = GetContext()->Map(res->GetResource(), 0, D3D11_MAP_READ, waitForGPU ? 0 : D3D11_MAP_FLAG_DO_NOT_WAIT, &lockedData);

				if (!waitForGPU && mapResult == DXGI_ERROR_WAS_STILL_DRAWING)
					return false;

				DF_D3DErrors::Throw(mapResult);

				memcpy(pinPtrData, lockedData.pData, data->Length * sizeof(T));

				GetContext()->Unmap(res->GetResource(), 0);
				return true;
			}

			generic <typename T> bool GetResourceData2D(DF_Resource11^ res, [Out] cli::array<T>^ data, UINT width, UINT height, bool waitForGPU)
			{
				pin_ptr<T> pinPtrData = &data[0];
				D3D11_MAPPED_SUBRESOURCE lockedData;
				HRESULT mapResult = GetContext()->Map(res->GetResource(), 0, D3D11_MAP_READ, waitForGPU ? 0 : D3D11_MAP_FLAG_DO_NOT_WAIT, &lockedData);

				if (!waitForGPU && mapResult == DXGI_ERROR_WAS_STILL_DRAWING)
					return false;

				DF_D3DErrors::Throw(mapResult);

				byte* gpuData = static_cast<byte*>(lockedData.pData);
				UINT cpuDataPitch = data->GetLength(0) / height;
				for (int row = 0; row < (int)height; row++)
				{
					pin_ptr<T> pinPtrRow = &data[row * cpuDataPitch];
					memcpy(pinPtrRow, &gpuData[row * lockedData.RowPitch], cpuDataPitch * sizeof(T));
				}

				GetContext()->Unmap(res->GetResource(), 0);
				return true;
			}

			bool IsResourceDataAvailable(DF_Resource11^ res)
			{
				D3D11_MAPPED_SUBRESOURCE lockedData;
				HRESULT mapResult = GetContext()->Map(res->GetResource(), 0, D3D11_MAP_READ, D3D11_MAP_FLAG_DO_NOT_WAIT, &lockedData);

				if (mapResult == DXGI_ERROR_WAS_STILL_DRAWING)
					return false;

				DF_D3DErrors::Throw(mapResult);

				GetContext()->Unmap(res->GetResource(), 0);
				return true;
			}

			void BeginEvent(String^ eventName)
			{
				System::IntPtr eventNamePtr = System::Runtime::InteropServices::Marshal::StringToHGlobalUni(eventName);
				LPCWSTR cstrEventName = static_cast<LPCWSTR>(eventNamePtr.ToPointer());
				GetContext()->BeginEventInt(cstrEventName, NULL);	
				System::Runtime::InteropServices::Marshal::FreeHGlobal(eventNamePtr);
			}

			void EndEvent()
			{
				GetContext()->EndEvent();
			}

			void FinishCommandList()
			{
				DF_D3DErrors::Throw(GetContext()->FinishCommandList(FALSE, GetCmdListPtr()));
			}

			/// <summary>
			/// Release all the memory associated with the last recorded command list.
			/// </summary>
			void ReleaseCommandList()
			{
				if (GetCmdList())
					GetCmdList()->Release();
				*GetCmdListPtr() = NULL;
			}

		};

		/// <summary>
		/// The main directx11 device context. 
		/// </summary>
		public ref class DF_D3D11Device : public DF_D3D11DeviceContext
		{
		private:
			DF_Texture11 ^ backBuffer, ^ backZBuffer;
			bool released, offlineMode, emulatesCmdLists;
			int devicePtrID, swapChainPtrID;

		internal:
			ID3D11Device3* GetDevice()
			{
				return reinterpret_cast<ID3D11Device3*>(this->comPtrList[devicePtrID]);
			}

			ID3D11Device3** GetDevicePtr()
			{
				return reinterpret_cast<ID3D11Device3**>(&this->comPtrList[devicePtrID]);
			}

			IDXGISwapChain1* GetSwapChain()
			{
				return reinterpret_cast<IDXGISwapChain1*>(this->comPtrList[swapChainPtrID]);
			}

			IDXGISwapChain1** GetSwapChainPtr()
			{
				return reinterpret_cast<IDXGISwapChain1**>(&this->comPtrList[swapChainPtrID]);
			}

		public:
			DF_D3D11Device(IntPtr targetHandle, bool fullScreen, int preferredWidth, int preferredHeight, bool antiAliasing)
			{
				released = false;
				offlineMode = targetHandle.ToInt64() == 0;
				devicePtrID = AddComPtr(NULL);
				swapChainPtrID = AddComPtr(NULL);
				InitializeDevice(targetHandle, fullScreen, preferredWidth, preferredHeight, antiAliasing);

				// check for cmd lists support
				{
					D3D11_FEATURE_DATA_THREADING threadingCaps = { FALSE, FALSE };
					DF_D3DErrors::Throw(GetDevice()->CheckFeatureSupport(D3D11_FEATURE_THREADING, &threadingCaps, sizeof(threadingCaps)));
					emulatesCmdLists = !threadingCaps.DriverCommandLists;
				}
			}

			void Present()
			{
				if (offlineMode) return;
				DF_D3DErrors::Throw(GetSwapChain()->Present(0, 0));
			}

			bool CanRender()
			{
				if (offlineMode) return true;
				return DF_D3DErrors::Check(GetSwapChain()->Present(0, DXGI_PRESENT_TEST));
			}

			DF_VertexShader11^ CreateVertexShader(cli::array<Byte>^ compiledVS, int startIndex, int bufferSize)
			{
				pin_ptr<Byte> pinPtrVS = &compiledVS[startIndex];
				ID3D11VertexShader* vs;
				DF_D3DErrors::Throw(GetDevice()->CreateVertexShader((DWORD*)pinPtrVS, bufferSize, nullptr, &vs));
				return gcnew DF_VertexShader11(vs);
			}


			DF_VertexShader11 ^ CreateVertexShader(cli::array<Byte> ^ compiledVS)
			{
				return CreateVertexShader(compiledVS, 0, compiledVS->Length);
			}

			DF_PixelShader11^ CreatePixelShader(cli::array<Byte>^ compiledPS, int startIndex, int bufferSize)
			{
				pin_ptr<Byte> pinPtrPS = &compiledPS[startIndex];
				ID3D11PixelShader* ps;
				DF_D3DErrors::Throw(GetDevice()->CreatePixelShader((DWORD*)pinPtrPS, bufferSize, nullptr, &ps));
				return gcnew DF_PixelShader11(ps);
			}

			DF_PixelShader11 ^ CreatePixelShader(cli::array<Byte> ^ compiledPS)
			{
				return CreatePixelShader(compiledPS, 0, compiledPS->Length);
			}

			DF_InputLayout ^ CreateInputLayout(cli::array<DF_VertexElement> ^ elems, cli::array<Byte> ^ compiledVS, int compiledVSFirstByteIndex, int compiledVSByteLength)
			{
				pin_ptr<Byte> pinPtrVS = &compiledVS[compiledVSFirstByteIndex];

				// create vertex declaration ( Define the vertex input layout )
				unique_ptr<D3D11_INPUT_ELEMENT_DESC[]>  dx11InputElems(new D3D11_INPUT_ELEMENT_DESC[elems->Length]);

				for (int i = 0; i < elems->Length; i++)
				{
					dx11InputElems[i] = {
						elems[i].Usage == DF_DeclUsage::Position ? "SV_POSITION" : "TEXCOORD",
						elems[i].UsageIndex,
						DX11Conv::DeclTypeToDXGIFORMAT[Convert::ToInt32(elems[i].Type)],
						elems[i].Stream,
						elems[i].Offset,
						elems[i].Stream > 0 ? D3D11_INPUT_PER_INSTANCE_DATA : D3D11_INPUT_PER_VERTEX_DATA,
						elems[i].Stream > 0 ? 1u : 0
					};
				}

				ID3D11InputLayout * inputLayout;
				DF_D3DErrors::Throw(GetDevice()->CreateInputLayout(dx11InputElems.get(), elems->Length, (DWORD *)pinPtrVS, compiledVSByteLength, &inputLayout));
				return gcnew DF_InputLayout(inputLayout);
			}

			generic <typename T>
			DF_Buffer11 ^ CreateVertexBuffer(UINT byteLength, DF_Usage11 usage, cli::array<T> ^ initialValue)
			{
				pin_ptr<T> pinPtrValue = initialValue != nullptr ? &initialValue[0] : nullptr;
				return CreateBuffer_Internal(byteLength, usage, D3D11_BIND_VERTEX_BUFFER, pinPtrValue);
			}

			DF_Buffer11 ^ CreateVertexBuffer(UINT byteLength, DF_Usage11 usage)
			{
				return CreateBuffer_Internal(byteLength, usage, D3D11_BIND_VERTEX_BUFFER, NULL);
			}

			DF_Buffer11 ^ CreateIndexBuffer(UINT byteLength, DF_Usage11 usage, cli::array<USHORT> ^ initialValue)
			{
				pin_ptr<USHORT> pinPtrValue = initialValue != nullptr ? &initialValue[0] : nullptr;
				return CreateBuffer_Internal(byteLength, usage, D3D11_BIND_INDEX_BUFFER, pinPtrValue);
			}

			DF_Buffer11 ^ CreateIndexBuffer(UINT byteLength, DF_Usage11 usage)
			{
				return CreateBuffer_Internal(byteLength, usage, D3D11_BIND_INDEX_BUFFER, NULL);
			}

			DF_Buffer11 ^ CreateConstantBuffer(UINT byteLength)
			{
				return CreateBuffer_Internal(byteLength, DF_Usage11::Dynamic, D3D11_BIND_CONSTANT_BUFFER, NULL);
			}

			DF_RasterState ^ CreateRasterState(DF_CullMode cullMode, DF_FillMode fillMode)
			{
				D3D11_RASTERIZER_DESC1 rsDesc = CD3D11_RASTERIZER_DESC1(CD3D11_DEFAULT());
				rsDesc.CullMode = static_cast<D3D11_CULL_MODE>(cullMode);
				rsDesc.FillMode = static_cast<D3D11_FILL_MODE>(fillMode);

				ID3D11RasterizerState1 * rs;
				DF_D3DErrors::Throw(GetDevice()->CreateRasterizerState1(&rsDesc, &rs));
				return gcnew DF_RasterState(rs);
			}

			DF_BlendState ^ CreateBlendState(bool blendEnabled, DF_BlendMode srcBlend, DF_BlendMode destBlend)
			{
				D3D11_BLEND_DESC1 bsDesc = CD3D11_BLEND_DESC1(CD3D11_DEFAULT());
				bsDesc.RenderTarget[0].BlendEnable = blendEnabled ? TRUE : FALSE;
				bsDesc.RenderTarget[0].SrcBlend = static_cast<D3D11_BLEND>(srcBlend);
				bsDesc.RenderTarget[0].DestBlend = static_cast<D3D11_BLEND>(destBlend);

				ID3D11BlendState1 * bs;
				DF_D3DErrors::Throw(GetDevice()->CreateBlendState1(&bsDesc, &bs));
				return gcnew DF_BlendState(bs);
			}

			DF_SamplerState ^ CreateSamplerState(DF_SamplerDesc desc)
			{
				D3D11_SAMPLER_DESC ssDesc = CD3D11_SAMPLER_DESC(CD3D11_DEFAULT());
				ssDesc.AddressU = static_cast<D3D11_TEXTURE_ADDRESS_MODE>(desc.AddressX);
				ssDesc.AddressV = static_cast<D3D11_TEXTURE_ADDRESS_MODE>(desc.AddressY);
				ssDesc.AddressW = static_cast<D3D11_TEXTURE_ADDRESS_MODE>(desc.AddressZ);
				ssDesc.Filter = static_cast<D3D11_FILTER>(desc.Filter);
				ssDesc.MaxAnisotropy = 16;
				ssDesc.BorderColor[0] = desc.BorderA;
				ssDesc.BorderColor[1] = desc.BorderR;
				ssDesc.BorderColor[2] = desc.BorderG;
				ssDesc.BorderColor[3] = desc.BorderB;
				if (desc.Filter != DF_TextureFilterType11::Point)
					ssDesc.MipLODBias = -0.9f; // sharper mipmapping with a little more noise

				ID3D11SamplerState * ss;
				DF_D3DErrors::Throw(GetDevice()->CreateSamplerState(&ssDesc, &ss));
				return gcnew DF_SamplerState(ss);
			}

			DF_DepthStencilState ^ CreateDepthStencilState(bool depthTestEnabled, bool depthWriteEnabled, DF_CompareFunc depthTest)
			{
				D3D11_DEPTH_STENCIL_DESC  dssDesc = CD3D11_DEPTH_STENCIL_DESC(CD3D11_DEFAULT());
				dssDesc.DepthEnable = depthTestEnabled ? TRUE : FALSE;
				dssDesc.DepthWriteMask = depthWriteEnabled ? D3D11_DEPTH_WRITE_MASK_ALL : D3D11_DEPTH_WRITE_MASK_ZERO;
				dssDesc.DepthFunc = static_cast<D3D11_COMPARISON_FUNC>(depthTest);

				ID3D11DepthStencilState * dss;
				GetDevice()->CreateDepthStencilState(&dssDesc, &dss);
				return gcnew DF_DepthStencilState(dss);
			}
	
			DF_Texture11 ^ CreateTexture(String ^ path)
			{
				marshal_context context;
				LPCTSTR cstrPath = context.marshal_as<const TCHAR*>(path);
				CComPtr<ID3D11Resource> texResource;
				
				if(path->EndsWith("dds"))
					DF_D3DErrors::Throw(DirectX::CreateDDSTextureFromFile(GetDevice(), cstrPath, &texResource, NULL));
				else
					DF_D3DErrors::Throw(DirectX::CreateWICTextureFromFileEx(GetDevice(), cstrPath, 0Ui64, D3D11_USAGE_DEFAULT, D3D11_BIND_SHADER_RESOURCE, 0, 0, DirectX::WIC_LOADER_IGNORE_SRGB, &texResource, NULL));
	
				ID3D11Texture2D1 * texture;
				DF_D3DErrors::Throw(texResource->QueryInterface(__uuidof(ID3D11Texture2D1), reinterpret_cast<void**>(&texture)));
				DF_Texture11 ^ managedTex = gcnew DF_Texture11(texture);

				// prepare needed views
				managedTex->PrepareShaderView(GetDevice());

				return managedTex;
			}
	
			DF_Texture11 ^ CreateTexture(cli::array<Byte> ^ fileBytes, bool isDDS)
			{
				pin_ptr<Byte> fileBytesPtr = &fileBytes[0];
				CComPtr<ID3D11Resource> texResource;

				if (isDDS) 
					DF_D3DErrors::Throw(DirectX::CreateDDSTextureFromMemory(GetDevice(), fileBytesPtr, fileBytes->Length, &texResource, NULL));
				else 
					DF_D3DErrors::Throw(DirectX::CreateWICTextureFromMemoryEx(GetDevice(), fileBytesPtr, fileBytes->Length, 0Ui64, D3D11_USAGE_DEFAULT, D3D11_BIND_SHADER_RESOURCE, 0, 0, DirectX::WIC_LOADER_IGNORE_SRGB,  &texResource, NULL));

				ID3D11Texture2D1 * texture;
				DF_D3DErrors::Throw(texResource->QueryInterface(__uuidof(ID3D11Texture2D1), reinterpret_cast<void**>(&texture)));
				DF_Texture11^ managedTex = gcnew DF_Texture11(texture);

				// prepare needed views
				managedTex->PrepareShaderView(GetDevice());

				return managedTex;
			}

			generic <typename T>
			DF_Texture11 ^ CreateTexture(UINT width, UINT height, DF_SurfaceFormat format, bool generateMipmaps, DF_Usage11 usage, cli::array<T> ^ initialValue, DF_TexBinding binding)
			{
				pin_ptr<T> pinPtrValue = &initialValue[0];

				//initial data
				D3D11_SUBRESOURCE_DATA initValue = {};
				if (initialValue != nullptr)
					initValue.pSysMem = (DWORD *)pinPtrValue;

				return CreateTexture_Internal(width, height, format, generateMipmaps, usage, initialValue != nullptr ? &initValue : NULL, binding);

			}

			DF_Texture11 ^ CreateTexture(UINT width, UINT height, DF_SurfaceFormat format, bool generateMipmaps, DF_Usage11 usage, DF_TexBinding binding)
			{
				return CreateTexture_Internal(width, height, format, generateMipmaps, usage, NULL, binding);
			}

			void Resize(UINT width, UINT height)
			{
				// remove all outstanding references to the swap chain's buffers.
				GetContext()->OMSetRenderTargets(0, 0, 0);
				ReleaseBackBuffers();
				DF_D3DErrors::Throw(GetSwapChain()->ResizeBuffers(0, width, height, DXGI_FORMAT_UNKNOWN, 0));
				
				RefreshBackBuffer();			
				SetRenderTargets(GetBackBufferDepth(), GetBackBuffer());
			}

			void UpdateSwapChain(IntPtr targetHandle, bool fullScreen, int preferredWidth, int preferredHeight, bool antiAliasing)
			{
				ReleaseSwapChain();

				// Create new swap chain
				{
					// Obtain DXGI factory from device (since we used nullptr for pAdapter above)
					CComPtr<IDXGIFactory3> dxgiFactory;
					{
						CComPtr<IDXGIDevice> dxgiDevice;
						DF_D3DErrors::Throw(GetDevice()->QueryInterface(__uuidof(IDXGIDevice), reinterpret_cast<void**>(&dxgiDevice)));
						CComPtr<IDXGIAdapter> adapter;
						DF_D3DErrors::Throw(dxgiDevice->GetAdapter(&adapter));
						DF_D3DErrors::Throw(adapter->GetParent(__uuidof(IDXGIFactory3), reinterpret_cast<void**>(&dxgiFactory)));
					}

					DXGI_SWAP_CHAIN_DESC1 sd = {};
					sd.Width = preferredWidth;
					sd.Height = preferredHeight;
					sd.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
					sd.SampleDesc.Count = 1;
					sd.SampleDesc.Quality = 0;
					sd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
#ifdef FLIP_MODE_SWAPCHAIN
					sd.BufferCount = 2;
					sd.SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD;
#else
					sd.BufferCount = 1;
					sd.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;
#endif // FLIP_MODE_SWAPCHAIN

					sd.Flags = DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH;
					
					DF_D3DErrors::Throw(dxgiFactory->CreateSwapChainForHwnd(GetDevice(), (HWND)targetHandle.ToPointer(), &sd, nullptr, nullptr, GetSwapChainPtr()));
					dxgiFactory->MakeWindowAssociation((HWND)targetHandle.ToPointer(), DXGI_MWA_NO_ALT_ENTER);
				}

				// Create render target and depth stencils and set them
				RefreshBackBuffer();
				SetRenderTargets(GetBackBufferDepth(), GetBackBuffer());

				// Go full screen if required
				if (fullScreen)
					SetFullscreen(true);
			}

			void SetFullscreen(bool fullscreen)
			{
				if (offlineMode) return;
				DXGI_SWAP_CHAIN_DESC1 sd;
				GetSwapChain()->GetDesc1(&sd);
				DF_D3DErrors::Throw(GetSwapChain()->SetFullscreenState(fullscreen, NULL));
				if (fullscreen)
					Resize(sd.Width, sd.Height);
			}

			System::Collections::Generic::List<DF_DisplayMode> ^ GetDisplayModes()
			{
				if (offlineMode) 
					return gcnew System::Collections::Generic::List<DF_DisplayMode>();

				// get representation of the output adapter
				CComPtr<IDXGIOutput> output;
				DF_D3DErrors::Throw(GetSwapChain()->GetContainingOutput(&output));

				return DF_Display::GetDisplayModesFromOutput(output);
			}

			DF_Texture11 ^ GetBackBuffer()
			{
				return backBuffer;
			}

			DF_Texture11 ^ GetBackBufferDepth()
			{
				return backZBuffer;
			}

			generic <typename T>
			void UpdateResource(DF_Resource11 ^ res, cli::array<T> ^ values)
			{
				pin_ptr<T> pinPtrValue = &values[0];
				GetContext()->UpdateSubresource(res->GetResource(), 0, nullptr, (DWORD *)pinPtrValue, 0, 0);
			}

			generic <typename T>
			void UpdateResource(DF_Resource11^ res, cli::array<T>^ values, int count)
			{
				pin_ptr<T> pinPtrValue = &values[0];
				const D3D11_BOX destRange = D3D11_BOX{ 0U, 0U, 0U, (UINT)count * sizeof(T), 1U, 1U };
				GetContext()->UpdateSubresource(res->GetResource(), 0, &destRange, (DWORD*)pinPtrValue, 0, 0);
			}

			generic <typename T> void UpdateResource2D(DF_Resource11 ^ res, cli::array<T> ^ value, int rowLength)
			{
				pin_ptr<T> pinPtrValue = &value[0];
				GetContext()->UpdateSubresource(res->GetResource(), 0, nullptr, (DWORD *)pinPtrValue, rowLength * sizeof(T), 0);
			}

			//generic <typename T>
			//void UpdateBuffer(DF_Buffer11 ^ res, cli::array<T, 2> ^ value)
			//{
			//	pin_ptr<T> pinPtrValue = &value[0, 0];
			//	UINT cpuDataPitch = value->GetLength(0) * sizeof(T);
			//	GetContext()->UpdateSubresource(res->GetResource(), 0, nullptr, (DWORD *)pinPtrValue, cpuDataPitch, 0);
			//}

			void SaveBackbufferToFile(String ^ path)
			{
				marshal_context context;
				const wchar_t* cstrPath = context.marshal_as<const wchar_t*>(path);
				DirectX::SaveWICTextureToFile(GetContext(), GetBackBuffer()->GetResource(), GUID_ContainerFormatBmp, cstrPath);
			}

			DF_D3D11DeviceContext ^ CreateDeferredContext()
			{
				DF_D3D11DeviceContext^ ctx = gcnew DF_D3D11DeviceContext();
				DF_D3DErrors::Throw(GetDevice()->CreateDeferredContext3(0, ctx->GetContextPtr()));
				ctx->IsEmulated = emulatesCmdLists;
				return ctx;
			}

			void ExecuteCommandList(DF_D3D11DeviceContext ^ cmds)
			{
				GetContext()->ExecuteCommandList(cmds->GetCmdList(), FALSE);
			}

			void ReleaseSwapChain()
			{
				if (!GetSwapChain())
					return;
			
				// destroy current swap chain
				SetFullscreen(false);
				GetContext()->OMSetRenderTargets(0, 0, 0);
				ReleaseBackBuffers();
				GetSwapChain()->Release();
				*GetSwapChainPtr() = NULL; // to prevent parent destructor to release it again 
				GetContext()->Flush();
			}

			virtual void Release() override
			{
				if (!IsReleased())
				{
					ReleaseSwapChain();
				}

				DF_D3D11DeviceContext::Release();
			}

		private:

			void ReleaseBackBuffers()
			{
				backBuffer->Release();
				backZBuffer->Release();
			}

			void RefreshBackBuffer()
			{
				// retrieve backbuffer surface
				ID3D11Texture2D1 * newBackBufferTex;
				DF_D3DErrors::Throw(GetSwapChain()->GetBuffer(0, __uuidof(ID3D11Texture2D1), reinterpret_cast<void**>(&newBackBufferTex)));

				// create new backBuffer	
				backBuffer = gcnew DF_Texture11(newBackBufferTex);
				backBuffer->PrepareRenderTargetView(GetDevice());

				// create new default depth buffer
				D3D11_TEXTURE2D_DESC bbDesc = backBuffer->GetTextureDesc();
				backZBuffer = CreateTexture(bbDesc.Width, bbDesc.Height, DF_SurfaceFormat::DEFAULT_DEPTH_FORMAT, false, DF_Usage11::Default, DF_TexBinding::DepthStencil);
			}

			DF_Texture11 ^ CreateTexture_Internal(UINT width, UINT height, DF_SurfaceFormat format, bool generateMipmaps, DF_Usage11 usage, D3D11_SUBRESOURCE_DATA * initValue, DF_TexBinding binding)
			{
				// texture description
				DXGI_FORMAT dxgiFormat = DX11Conv::SurfaceToDXGIFORMAT(format);
				D3D11_TEXTURE2D_DESC1 texDesc = CD3D11_TEXTURE2D_DESC1(dxgiFormat, width, height, 1U, generateMipmaps ? 0U : 1U);
				texDesc.Usage = static_cast<D3D11_USAGE>(usage == DF_Usage11::Dynamic ? DF_Usage11::Default : usage); // dynamic currently unsupported, write once per frame with D3D11_CPU_ACCESS_WRITE penalty is low anyway....
				texDesc.BindFlags = static_cast<UINT>(binding);

				if (binding == DF_TexBinding::ShaderResource && usage == DF_Usage11::Dynamic)
					texDesc.CPUAccessFlags |= D3D11_CPU_ACCESS_WRITE;
				if (usage == DF_Usage11::Staging)
					texDesc.CPUAccessFlags |= D3D11_CPU_ACCESS_READ; // staging textures are always readable from cpu

				// create the texture
				ID3D11Texture2D1 * texture;
				DF_D3DErrors::Throw(GetDevice()->CreateTexture2D1(&texDesc, initValue, &texture));
				DF_Texture11^ managedTex = gcnew DF_Texture11(texture);

				// prepare needed views
				if (!binding.HasFlag(DF_TexBinding::DepthStencil) && usage != DF_Usage11::Staging)
					managedTex->PrepareShaderView(GetDevice());
				if (binding.HasFlag(DF_TexBinding::DepthStencil))
					managedTex->PrepareDepthStencilView(GetDevice());
				if (binding.HasFlag(DF_TexBinding::RenderTarget))
					managedTex->PrepareRenderTargetView(GetDevice());

				return managedTex;
			}

			DF_Buffer11 ^ CreateBuffer_Internal(UINT byteLength, DF_Usage11 usage, UINT bindFlags, const void * initialDataPtr)
			{
				D3D11_BUFFER_DESC bd = {};
				bd.Usage = static_cast<D3D11_USAGE>(usage);
				bd.ByteWidth = byteLength;
				bd.BindFlags = bindFlags;

				// guess cpu access from usage
				bd.CPUAccessFlags = 0;
				if(usage == DF_Usage11::Staging) bd.CPUAccessFlags |= D3D11_CPU_ACCESS_READ;
				if (usage == DF_Usage11::Dynamic) bd.CPUAccessFlags |= D3D11_CPU_ACCESS_WRITE;

				D3D11_SUBRESOURCE_DATA initValue = {};
				if (initialDataPtr != NULL)
					initValue.pSysMem = initialDataPtr;

				ID3D11Buffer * buffer;
				DF_D3DErrors::Throw(GetDevice()->CreateBuffer(&bd, initialDataPtr != NULL ? &initValue : NULL, &buffer));
				return gcnew DF_Buffer11(buffer, byteLength);
			}

			void InitializeDevice(IntPtr targetHandle, bool fullScreen, int preferredWidth, int preferredHeight, bool antiAliasing)
			{

				// Create device
				{
					UINT createDeviceFlags = 0;
#if DEBUG_LAYER
					createDeviceFlags |= D3D11_CREATE_DEVICE_DEBUG;
#endif

					D3D_FEATURE_LEVEL featureLevels[] = { DF_11_FEATURE_LEVEL };
					UINT numFeatureLevels = ARRAYSIZE(featureLevels);

					CComPtr<ID3D11Device> device;
					CComPtr<ID3D11DeviceContext> deviceContext;
					DF_D3DErrors::Throw(D3D11CreateDevice(nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, createDeviceFlags, featureLevels, numFeatureLevels, D3D11_SDK_VERSION, &device, NULL, &deviceContext));
					DF_D3DErrors::Throw(device->QueryInterface(__uuidof(ID3D11Device3), reinterpret_cast<void**>(GetDevicePtr())));
					DF_D3DErrors::Throw(deviceContext->QueryInterface(__uuidof(ID3D11DeviceContext3), reinterpret_cast<void**>(GetContextPtr())));
				}

				// Create swap chain / buffer / viewport
				if (!offlineMode)
					UpdateSwapChain(targetHandle, fullScreen, preferredWidth, preferredHeight, antiAliasing);
			}
		};


	}
}