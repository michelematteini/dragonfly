#include "DirectxCommon.h"
#include <d3d9.h>
#include <d3dx9.h>
#include <msclr\marshal.h>

#define foreach_dispMode(name, format) \
	UINT dmcount = dx_obj->GetAdapterModeCount(D3DADAPTER_DEFAULT, format ); \
	D3DDISPLAYMODE name; \
	dx_obj->EnumAdapterModes(D3DADAPTER_DEFAULT, format, 0, &name ); \
	for(UINT i = 0; i < dmcount; i++, dx_obj->EnumAdapterModes(D3DADAPTER_DEFAULT, format, i, &name)) 

using namespace std;
using namespace msclr::interop;

using namespace System;
using namespace System::Runtime::InteropServices;

namespace DragonflyGraphicsWrappers {

	namespace DX9 {

		#pragma region ENUMS
		/*================ ENUMS ==========*/

		public enum class DF_StreamSourceType
		{
			Vertices = 0,
			IndexedData = D3DSTREAMSOURCE_INDEXEDDATA,
			InstancedData = D3DSTREAMSOURCE_INSTANCEDATA
		};

		public enum class DF_RenderStateFlag
		{
			ZEnable = D3DRS_ZENABLE,
			ZWriteEnable = D3DRS_ZWRITEENABLE,
			AlphaTestEnable = D3DRS_ALPHATESTENABLE,
			AlphaBlendEnable = D3DRS_ALPHABLENDENABLE,
			AntiAliasingEnable = D3DRS_MULTISAMPLEANTIALIAS
		};

		public enum class DF_Usage
		{
			None = 0,
			WriteOnly = D3DUSAGE_WRITEONLY,
			Dynamic = D3DUSAGE_DYNAMIC,
			RenderTarget = D3DUSAGE_RENDERTARGET,
		};

		public enum class DF_SamplerStateType
		{
			AddressX = D3DSAMP_ADDRESSU,
			AddressY = D3DSAMP_ADDRESSV,
			AddressZ = D3DSAMP_ADDRESSW,
			BorderColor = D3DSAMP_BORDERCOLOR,
			MagFilter = D3DSAMP_MAGFILTER,
			MinFilter = D3DSAMP_MINFILTER,
			MipFilter = D3DSAMP_MIPFILTER,
			MaxAnisotropy = D3DSAMP_MAXANISOTROPY,
			MipMapBias = D3DSAMP_MIPMAPLODBIAS
		};

		public enum class DF_TextureFilterType {
			None = D3DTEXF_NONE,
			Point = D3DTEXF_POINT,
			Linear = D3DTEXF_LINEAR,
			Anisotropic = D3DTEXF_ANISOTROPIC
		};

		#pragma endregion 

		#pragma region FORWARD DECLARATIONS

		//directx 9 main classes
		ref class DF_Directx3D9;
		ref class DF_D3D9Device;

		//directx graphic resources
		ref class DF_VertexDeclaration;
		ref class DF_VertexBuffer;
		ref class DF_IndexBuffer;
		ref class DF_Texture;
		ref class DF_Surface;

		// errors
		ref class DF_D3D9Errors;

		#pragma endregion

		#pragma region DX Errors to Exceptions wrap

		/*
		* D3D Error Exceptions
		*/
		public ref class D3D9CallResults abstract sealed
		{
		public:
			literal HRESULT DEVICE_LOST = D3DERR_DEVICELOST;
			literal HRESULT DEVICE_OK = D3D_OK;
			literal HRESULT DEVICE_NOT_RESET = D3DERR_DEVICENOTRESET;
			literal HRESULT DEVICE_REMOVED = D3DERR_DEVICEREMOVED;
			literal HRESULT DRIVER_ERROR = D3DERR_DRIVERINTERNALERROR;
			literal HRESULT OUT_OF_MEMORY = D3DERR_OUTOFVIDEOMEMORY;
			literal HRESULT INVALID_CALL = D3DERR_INVALIDCALL;
		};

		#pragma endregion	 

		#pragma region DX RESOURCES

		/*======================================
		* Vertex buffer.
		========================================*/
		public ref class DF_VertexBuffer : public DF_Resource
		{
		internal:
			DF_VertexBuffer(IDirect3DVertexBuffer9 * vertBuff) : DF_Resource(vertBuff) { }

			IDirect3DVertexBuffer9 * GetNativePointer()
			{
				return (IDirect3DVertexBuffer9 * )this->comPtrList[0];
			}

		public:
			generic <typename T>
				void SetVertices(UINT startIndex, UINT count, cli::array<T> ^ vertices)
				{
					void * vertBuffData;
					pin_ptr<T> pinPtrVertices = &(vertices[0]);
					DF_D3DErrors::Throw(GetNativePointer()->Lock(startIndex * sizeof(T), count * sizeof(T), &vertBuffData, 0));
					memcpy(vertBuffData, pinPtrVertices, count * sizeof(T));
					DF_D3DErrors::Throw(GetNativePointer()->Unlock());
				}
		};


		/*=============================================*/
		/*            Index buffer                     */
		/*=============================================*/
		public ref class DF_IndexBuffer : public DF_Resource
		{
		internal:
			DF_IndexBuffer(IDirect3DIndexBuffer9 * indexBuffer) : DF_Resource(indexBuffer) { }
			
			IDirect3DIndexBuffer9 * GetNativePointer()
			{
				return (IDirect3DIndexBuffer9 *)this->comPtrList[0];
			}

		public:
			void SetIndices(UINT startIndex, UINT count, cli::array<USHORT> ^ indices)
			{
				void * indexBufferData;
				pin_ptr<USHORT> pinPtrIndices = &(indices[0]);
				DF_D3DErrors::Throw(GetNativePointer()->Lock(startIndex * sizeof(USHORT), count * sizeof(USHORT), &indexBufferData, 0));
				memcpy(indexBufferData, pinPtrIndices, count * sizeof(USHORT));
				DF_D3DErrors::Throw(GetNativePointer()->Unlock());
			}
		};


		/*=======================================================*/
		/*                Vertex declaration                     */
		/* Informations on the structure of a custom vertex      */
		/*=======================================================*/
		public ref class DF_VertexDeclaration : public DF_Resource
		{
		private:
			IDirect3DVertexDeclaration9 *dx_vertDecl;

		internal:
			DF_VertexDeclaration(IDirect3DVertexDeclaration9 * vertDecl) : DF_Resource(vertDecl) { }

			IDirect3DVertexDeclaration9 * GetNativePointer()
			{
				return (IDirect3DVertexDeclaration9 *)this->comPtrList[0];
			}
		};


		/*============================================*/
		/*            Surface                         */
		/* A single surface usable as render target   */
		/*============================================*/
		public ref class DF_Surface
		{
		private:
			IDirect3DSurface9 * dx_surface;
			bool released;

		internal:
			DF_Surface(IDirect3DSurface9 * surface)
				: dx_surface(surface)
			{

			}

			IDirect3DSurface9 * GetNativePointer()
			{
				return dx_surface;
			}

		public:
			generic <typename T>
				void SetData(cli::array<T> ^ data)
				{
					D3DLOCKED_RECT lockedRect;
					pin_ptr<T> pinPtrData = &data[0];
					DF_D3DErrors::Throw(dx_surface->LockRect(&lockedRect, NULL, D3DLOCK_DISCARD));
					memcpy(lockedRect.pBits, pinPtrData, data->Length * sizeof(T));
					DF_D3DErrors::Throw(dx_surface->UnlockRect());
				}

			generic <typename T>
				void GetData(cli::array<T> ^ destBuffer, bool discard)
				{
					D3DLOCKED_RECT lockedRect;
					D3DSURFACE_DESC texInfo;
					DF_D3DErrors::Throw(dx_surface->GetDesc(&texInfo));

					// check if the buffer size can fit the texture
					DF_D3DErrors::Throw(dx_surface->LockRect(&lockedRect, NULL, discard ? D3DLOCK_DISCARD : D3DLOCK_READONLY));
					int requiredLength = lockedRect.Pitch * texInfo.Height / sizeof(T);
					if (destBuffer->Length < requiredLength)
					{
						DF_D3DErrors::Check(dx_surface->UnlockRect());
						throw gcnew Exception("Destination buffer size is smaller then the locked surface! (" + requiredLength + ")");
					}

					// copy surface data	
					pin_ptr<T> pinPtrData = &destBuffer[0];
					memcpy(pinPtrData, lockedRect.pBits, requiredLength * sizeof(T));
					DF_D3DErrors::Throw(dx_surface->UnlockRect());
				}

				void Release()
				{
					if (!released)
					{
						dx_surface->Release();
						released = true;
					}
				}

				//finalizer and destructor for gc
				~DF_Surface() { this->!DF_Surface(); }
				!DF_Surface() { Release(); }
		};


		/*=============================================*/
		/*         2D Texture object                   */
		/*=============================================*/
		public ref class DF_Texture : public DF_Resource
		{
		internal:
			DF_Texture(IDirect3DTexture9 * texture) : DF_Resource(texture) { }

			IDirect3DTexture9 * GetNativePointer()
			{
				return (IDirect3DTexture9 *)this->comPtrList[0];
			}

		public:
			DF_Surface ^ GetSurfaceLevel(UINT level)
			{
				IDirect3DSurface9 *dx_surface;
				DF_D3DErrors::Throw(GetNativePointer()->GetSurfaceLevel(level, &dx_surface));
				return gcnew DF_Surface(dx_surface);
			}
		};


		/*=============================================*/
		/*            Vertex Shader                    */
		/*=============================================*/
		public ref class DF_VertexShader : public DF_Resource
		{
		internal:
			DF_VertexShader(IDirect3DVertexShader9 * vs) : DF_Resource(vs) { }

			IDirect3DVertexShader9 * GetNativePointer()
			{
				return (IDirect3DVertexShader9 *)this->comPtrList[0];
			}

		};

		/*=============================================*/
		/*              Pixel Shader                   */
		/*=============================================*/
		public ref class DF_PixelShader : public DF_Resource
		{
		internal:
			DF_PixelShader(IDirect3DPixelShader9 * ps) : DF_Resource(ps) { }

			IDirect3DPixelShader9 * GetNativePointer()
			{
				return (IDirect3DPixelShader9 *)this->comPtrList[0];
			}
		};

#pragma endregion

		/*=======================================================================*/
		/* This class includes methods for creating the D3d9 device,
		/* enumerating and retrieving his capabilities.
		/*=======================================================================*/
		public ref class DF_Directx3D9
		{
		public:
			static DF_Directx3D9 ^ Instance = nullptr;
		private:
			LPDIRECT3D9 dx_obj;
			D3DPRESENT_PARAMETERS * defParams;
			bool supportedDevice;
			UINT adapterIndex;//TODO: add setDevice / getDeviceCount public methods
			D3DDEVTYPE deviceType;
			bool released;

			bool fillDefaultPresParams()
			{
				ZeroMemory(defParams, sizeof(*defParams));
				defParams->SwapEffect = D3DSWAPEFFECT_DISCARD;

				D3DFORMAT backBufferFormat;
				deviceType = D3DDEVTYPE_HAL;

				//search for available backbuffer format (fullsreen)
				if (DF_D3DErrors::Check(dx_obj->CheckDeviceType(adapterIndex, deviceType, D3DFMT_X8R8G8B8, D3DFMT_X8R8G8B8, false)))
					backBufferFormat = D3DFMT_X8R8G8B8;
				else if (DF_D3DErrors::Check(dx_obj->CheckDeviceType(adapterIndex, deviceType, D3DFMT_X1R5G5B5, D3DFMT_X1R5G5B5, false)))
					backBufferFormat = D3DFMT_X1R5G5B5;
				else if (DF_D3DErrors::Check(dx_obj->CheckDeviceType(adapterIndex, deviceType, D3DFMT_R5G6B5, D3DFMT_R5G6B5, false)))
					backBufferFormat = D3DFMT_R5G6B5;
				else
					//if none of these is available, switch to ref device type and try again
				{
					deviceType = D3DDEVTYPE_REF;

					if (DF_D3DErrors::Check(dx_obj->CheckDeviceType(adapterIndex, deviceType, D3DFMT_X8R8G8B8, D3DFMT_X8R8G8B8, false)))
						backBufferFormat = D3DFMT_X8R8G8B8;
					else if (DF_D3DErrors::Check(dx_obj->CheckDeviceType(adapterIndex, deviceType, D3DFMT_X1R5G5B5, D3DFMT_X1R5G5B5, false)))
						backBufferFormat = D3DFMT_X1R5G5B5;
					else if (DF_D3DErrors::Check(dx_obj->CheckDeviceType(adapterIndex, deviceType, D3DFMT_R5G6B5, D3DFMT_R5G6B5, false)))
						backBufferFormat = D3DFMT_R5G6B5;
					else return false;//no format available, device cannot be created
				}

				//set backbuffer format
				defParams->BackBufferFormat = backBufferFormat;

				//initialize depth buffer
				defParams->EnableAutoDepthStencil = TRUE;
				if (SupportedDepthFormat(D3DFMT_D24S8, backBufferFormat))
					defParams->AutoDepthStencilFormat = D3DFMT_D24S8;
				else if (SupportedDepthFormat(D3DFMT_D24X8, backBufferFormat))
					defParams->AutoDepthStencilFormat = D3DFMT_D24X8;
				else if (SupportedDepthFormat(D3DFMT_D16, backBufferFormat))
					defParams->AutoDepthStencilFormat = D3DFMT_D16;
				else return false;//no format available, device cannot be created

				defParams->PresentationInterval = D3DPRESENT_INTERVAL_IMMEDIATE;
				defParams->MultiSampleType = D3DMULTISAMPLE_NONE;//multisampling disabled by default

																 //select the best display mode
				D3DDISPLAYMODE bestDispMode;
				ZeroMemory(&bestDispMode, sizeof(bestDispMode));

				foreach_dispMode(dispMode, backBufferFormat)
				{
					if (dispMode.Width > bestDispMode.Width)
						bestDispMode = dispMode;
					else if (dispMode.Height > bestDispMode.Height)
					{
						bestDispMode.Height = dispMode.Height;
						bestDispMode.RefreshRate = dispMode.RefreshRate;
					}
					else if (dispMode.RefreshRate > bestDispMode.RefreshRate)
						bestDispMode.RefreshRate = dispMode.RefreshRate;
				}

				//save best mode to params
				defParams->BackBufferWidth = bestDispMode.Width;
				defParams->BackBufferHeight = bestDispMode.Height;
				defParams->FullScreen_RefreshRateInHz = bestDispMode.RefreshRate;

				return true;
			}

			bool SupportedTextureFormat(D3DFORMAT format, D3DFORMAT backBufferFormat)
			{
				return DF_D3DErrors::Check(dx_obj->CheckDeviceFormat(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, backBufferFormat, 0, D3DRTYPE_TEXTURE, format));
			}

			bool SupportedTargetFormat(D3DFORMAT format, D3DFORMAT backBufferFormat)
			{
				return DF_D3DErrors::Check(dx_obj->CheckDeviceFormat(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, backBufferFormat, D3DUSAGE_RENDERTARGET, D3DRTYPE_SURFACE, format));
			}

			bool SupportedDepthFormat(D3DFORMAT format, D3DFORMAT backBufferFormat)
			{
				return DF_D3DErrors::Check(dx_obj->CheckDeviceFormat(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, backBufferFormat, D3DUSAGE_DEPTHSTENCIL, D3DRTYPE_SURFACE, format));
			}

		internal:
			//updates default or old pp with the new settings, checking for resolution / format support
			static void fillPresParams(D3DPRESENT_PARAMETERS * pp, DF_Directx3D9 ^ dx, IntPtr targetHandle, bool fullScreen, int preferredWidth, int preferredHeight)
			{
				*pp = *Instance->defParams;//fill with default data
				pp->hDeviceWindow = (HWND)targetHandle.ToPointer();
				pp->Windowed = fullScreen ? FALSE : TRUE;

				if (fullScreen) 				
				{
					// cannot toggle fullscreen on a child window, use parent
					pp->hDeviceWindow = GetAncestor(pp->hDeviceWindow, GA_ROOT); 
					
					//check if the requested resolution is supported	
					LPDIRECT3D9 dx_obj = dx->dx_obj;
					foreach_dispMode(dispMode, pp->BackBufferFormat)
					{
						if (dispMode.Width == preferredWidth && dispMode.Height == preferredHeight)
						{
							pp->BackBufferWidth = preferredWidth;
							pp->BackBufferHeight = preferredHeight;
							pp->FullScreen_RefreshRateInHz = Instance->defParams->FullScreen_RefreshRateInHz;
							break;
						}
					}
				}
				else
				{
					pp->BackBufferWidth = preferredWidth;
					pp->BackBufferHeight = preferredHeight;
					pp->FullScreen_RefreshRateInHz = 0;
				}

				pp->BackBufferFormat = fullScreen ? pp->BackBufferFormat : D3DFMT_UNKNOWN;
			}

			D3DMULTISAMPLE_TYPE SupportedMSAA(D3DFORMAT format)
			{
				DWORD multisampleType = D3DMULTISAMPLE_16_SAMPLES;
				while (multisampleType > 0 && FAILED(dx_obj->CheckDeviceMultiSampleType(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, format, FALSE, (D3DMULTISAMPLE_TYPE)multisampleType, NULL)))
				{
					multisampleType = (multisampleType >> 2) << 1;
				}
				return (D3DMULTISAMPLE_TYPE)multisampleType;
			}

		public:
			DF_Directx3D9()
			{
				adapterIndex = D3DADAPTER_DEFAULT;//TODO: some sort of device choice can be added

				dx_obj = Direct3DCreate9(D3D_SDK_VERSION);

				if (dx_obj != NULL)
				{
					if (defParams == NULL)
					{
						defParams = new D3DPRESENT_PARAMETERS();
					}
					supportedDevice = fillDefaultPresParams();
				}
				Instance = this;
			}

			bool IsAvailable()
			{
				return dx_obj != NULL && supportedDevice;
			}

			DF_D3D9Device ^ CreateDevice(IntPtr targetHandle, bool fullScreen, int preferredWidth, int preferredHeight, bool antiAliasing);

			System::Collections::Generic::List<DF_DisplayMode> ^ GetDisplayModes()
			{
				System::Collections::Generic::List<DF_DisplayMode> ^ clrDisplayModes = gcnew System::Collections::Generic::List<DF_DisplayMode>();
				if (!IsAvailable()) return clrDisplayModes;

				UINT displayModeCount = dx_obj->GetAdapterModeCount(adapterIndex, defParams->BackBufferFormat);
				DF_DisplayMode curMode;
				D3DDISPLAYMODE dxCurMode;
				for (UINT i = 0; i < displayModeCount; i++)
				{
					dx_obj->EnumAdapterModes(adapterIndex, defParams->BackBufferFormat, i, &dxCurMode);
					curMode.Width = dxCurMode.Width;
					curMode.Height = dxCurMode.Height;
					curMode.RefreshRate = dxCurMode.RefreshRate;
					clrDisplayModes->Add(curMode);
				}

				return clrDisplayModes;
			}

			void Release()
			{
				if (!released)
				{
					dx_obj->Release();
					released = true;
				}
			}

			//finalizer and destructor for gc
			~DF_Directx3D9() { this->!DF_Directx3D9(); }
			!DF_Directx3D9() { Release(); delete defParams; }
		};

		/*================================================*/
		/*                D3D9 Device			          */
		/*================================================*/
		public ref class DF_D3D9Device
		{
		private:
			LPDIRECT3DDEVICE9 dx_device;
			bool released;

		public:
			DF_D3D9Device(LPDIRECT3DDEVICE9 device)
				: dx_device(device)
			{

			}

			void Release()
			{
				if (!released)
				{
					dx_device->Release();
					released = true;
				}
			}

			property bool Released
			{
				bool get()
				{
					return this->released;
				}
			}

			void Present()
			{
				DF_D3DErrors::Check(dx_device->Present(NULL, NULL, NULL, NULL));
			}

			void TestCooperativeLevel()
			{
				DF_D3DErrors::Check(dx_device->TestCooperativeLevel());
			}

			void Clear(int r, int g, int b, int a, bool clearTargets, bool clearDepthBuffer)
			{
				DWORD clearFlags = 0;
				if (clearTargets)
					clearFlags |= D3DCLEAR_TARGET;
				if (clearDepthBuffer)
					clearFlags |= D3DCLEAR_ZBUFFER;

				DF_D3DErrors::Throw(dx_device->Clear(0, NULL, clearFlags, D3DCOLOR_RGBA(r, g, b, a), 0.0f, 0));
			}

			void Clear(int r, int g, int b, int a, bool clearTargets, bool clearDepthBuffer, int fromX, int fromY, int toX, int toY)
			{
				DWORD clearFlags = 0;
				if (clearTargets)
					clearFlags |= D3DCLEAR_TARGET;
				if (clearDepthBuffer)
					clearFlags |= D3DCLEAR_ZBUFFER;

				D3DRECT clearRect = { fromX, fromY, toX, toY };

				DF_D3DErrors::Throw(dx_device->Clear(1, &clearRect, clearFlags, D3DCOLOR_RGBA(r, g, b, a), 0.0f, 0));
			}

			void SetViewport(UINT x, UINT y, UINT width, UINT height)
			{
				D3DVIEWPORT9 vp;
				vp.X = x;
				vp.Y = y;
				vp.Width = width;
				vp.Height = height;
				vp.MinZ = 0.0f;
				vp.MaxZ = 1.0f;
				DF_D3DErrors::Throw(dx_device->SetViewport(&vp));
			}

			void BeginScene()
			{
				DF_D3DErrors::Throw(dx_device->BeginScene());
			}

			void EndScene()
			{
				DF_D3DErrors::Throw(dx_device->EndScene());
			}

			void Reset(IntPtr targetHandle, bool fullScreen, int preferredWidth, int preferredHeight)
			{
				D3DPRESENT_PARAMETERS dx_PresParams;
				DF_Directx3D9::fillPresParams(&dx_PresParams, DF_Directx3D9::Instance, targetHandle, fullScreen, preferredWidth, preferredHeight);
				DF_D3DErrors::Throw(dx_device->Reset(&dx_PresParams));
				if (dx_PresParams.MultiSampleType) SetRenderStateFLag(DF_RenderStateFlag::AntiAliasingEnable, true);
				SetZFunction(DF_CompareFunc::GreaterEqual); // inverted z by default
			}

			DF_VertexDeclaration ^ CreateVertexDeclaration(cli::array<DF_VertexElement> ^ elems)
			{
				D3DVERTEXELEMENT9 * dx_elems = new D3DVERTEXELEMENT9[elems->Length + 1];

				for (int i = 0; i < elems->Length; i++)
				{
					dx_elems[i].Method = D3DDECLMETHOD_DEFAULT;
					dx_elems[i].Offset = static_cast<WORD>(elems[i].Offset);
					dx_elems[i].Stream = static_cast<WORD>(elems[i].Stream);
					dx_elems[i].Type = static_cast<BYTE>(elems[i].Type) - 1;
					dx_elems[i].Usage = static_cast<BYTE>(elems[i].Usage);
					dx_elems[i].UsageIndex = static_cast<BYTE>(elems[i].UsageIndex);
				}

				D3DVERTEXELEMENT9 declEnd = D3DDECL_END();
				dx_elems[elems->Length] = declEnd;

				//DF_VertexDeclaration
				IDirect3DVertexDeclaration9 *dx_vertDecl;
				DF_D3DErrors::Throw(dx_device->CreateVertexDeclaration(dx_elems, &dx_vertDecl));
				delete[] dx_elems;
				return gcnew DF_VertexDeclaration(dx_vertDecl);
			}

			void SetVertexDeclaration(DF_VertexDeclaration ^ vertDecl)
			{
				IDirect3DVertexDeclaration9 * vd = vertDecl == nullptr ? NULL : vertDecl->GetNativePointer();
				DF_D3DErrors::Throw(dx_device->SetVertexDeclaration(vd));
			}

			DF_VertexBuffer ^ CreateVertexBuffer(UINT byteLength, DF_Usage usage)
			{
				IDirect3DVertexBuffer9 * vertBuff;
				DF_D3DErrors::Throw(dx_device->CreateVertexBuffer(byteLength, static_cast<DWORD>(usage), 0, D3DPOOL_MANAGED, &vertBuff, NULL));
				return gcnew DF_VertexBuffer(vertBuff);
			}

			void SetStreamSource(UINT streamNumber, DF_VertexBuffer ^ vertexBuffer, UINT byteOffset, UINT vertexSize)
			{
				IDirect3DVertexBuffer9 * vb = vertexBuffer == nullptr ? NULL : vertexBuffer->GetNativePointer();
				DF_D3DErrors::Throw(dx_device->SetStreamSource(streamNumber, vb, byteOffset, vertexSize));
			}

			void SetStreamSourceFreq(UINT streamNumber, DF_StreamSourceType stType, UINT instanceCount)
			{
				DF_D3DErrors::Throw(dx_device->SetStreamSourceFreq(streamNumber, static_cast<UINT>(stType) | instanceCount));
			}

			DF_IndexBuffer ^ CreateIndexBuffer(UINT byteLength)
			{
				IDirect3DIndexBuffer9 * indexBuffer;
				DF_D3DErrors::Throw(dx_device->CreateIndexBuffer(byteLength, D3DUSAGE_WRITEONLY, D3DFMT_INDEX16, D3DPOOL_MANAGED, &indexBuffer, NULL));
				return gcnew DF_IndexBuffer(indexBuffer);
			}

			void SetIndexBuffer(DF_IndexBuffer ^ indexBuffer)
			{
				IDirect3DIndexBuffer9 * ib = indexBuffer == nullptr ? NULL : indexBuffer->GetNativePointer();
				DF_D3DErrors::Throw(dx_device->SetIndices(ib));
			}

			DF_Texture ^ CreateTexture(UINT width, UINT height, DF_Usage usage, DF_SurfaceFormat format, bool generateMipmaps)
			{
				IDirect3DTexture9 * texture;
				DF_D3DErrors::Throw(dx_device->CreateTexture(
					width,
					height,
					generateMipmaps ? 0 : 1,
					static_cast<DWORD>(usage),
					static_cast<D3DFORMAT>(format),
					(usage == DF_Usage::RenderTarget) ? D3DPOOL_DEFAULT : D3DPOOL_MANAGED,
					&texture,
					NULL
				));
				return gcnew DF_Texture(texture);
			}

			DF_Texture ^ CreateTexture(String ^ path, [Out] int % width, [Out] int % height)
			{
				marshal_context context;
				LPCTSTR cstrPath = context.marshal_as<const TCHAR*>(path);
				IDirect3DTexture9 * texture;
				DF_D3DErrors::Throw(D3DXCreateTextureFromFile(dx_device, cstrPath, &texture));

				D3DSURFACE_DESC texInfo;
				DF_D3DErrors::Throw(texture->GetLevelDesc(0, &texInfo));
				width = (int)texInfo.Width;
				height = (int)texInfo.Height;
				return gcnew DF_Texture(texture);
			}

			DF_Texture ^ CreateTexture(cli::array<Byte> ^ fileBytes, [Out] int % width, [Out] int % height)
			{
				pin_ptr<Byte> fileBytesPtr = &fileBytes[0];
				IDirect3DTexture9 * texture;
				DF_D3DErrors::Throw(D3DXCreateTextureFromFileInMemory(dx_device, fileBytesPtr, fileBytes->Length, &texture));

				D3DSURFACE_DESC texInfo;
				DF_D3DErrors::Throw(texture->GetLevelDesc(0, &texInfo));
				width = (int)texInfo.Width;
				height = (int)texInfo.Height;
				return gcnew DF_Texture(texture);
			}

			DF_Surface ^ CreateDepthStencilBuffer(UINT width, UINT height, bool antialiased)
			{
				IDirect3DSurface9 * depthBuffer;
				D3DMULTISAMPLE_TYPE msaaType = D3DMULTISAMPLE_NONE;
				if (antialiased) msaaType = DF_Directx3D9::Instance->SupportedMSAA((D3DFORMAT)DF_SurfaceFormat::A8R8G8B8);
				DF_D3DErrors::Throw(dx_device->CreateDepthStencilSurface(
					width,
					height,
					D3DFORMAT::D3DFMT_D24S8,
					msaaType,
					0,
					TRUE,
					&depthBuffer,
					NULL
				));
				return gcnew DF_Surface(depthBuffer);
			}

			DF_Surface ^ CreateDepthStencilBuffer(UINT width, UINT height)
			{
				return CreateDepthStencilBuffer(width, height, false);
			}

			void SetDepthStencilBuffer(DF_Surface ^ depthBuffer)
			{
				DF_D3DErrors::Throw(dx_device->SetDepthStencilSurface(depthBuffer->GetNativePointer()));
			}

			DF_Surface ^ GetDepthStencilBuffer()
			{
				IDirect3DSurface9 * depthBuffer;
				DF_D3DErrors::Throw(dx_device->GetDepthStencilSurface(&depthBuffer));
				return gcnew DF_Surface(depthBuffer);
			}

			void DrawPrimitive(DF_PrimitiveType primitiveType, UINT startVertex, UINT primitiveCount)
			{
				DF_D3DErrors::Throw(dx_device->DrawPrimitive(static_cast<D3DPRIMITIVETYPE>(primitiveType), startVertex, primitiveCount));
			}

			void DrawIndexedPrimitive(DF_PrimitiveType primitiveType, INT baseVertexIndex, UINT minVertexIndex, UINT vertexCount, UINT startIndex, UINT primitiveCount)
			{
				DF_D3DErrors::Throw(
					dx_device->DrawIndexedPrimitive(
						static_cast<D3DPRIMITIVETYPE>(primitiveType),
						baseVertexIndex,//Offset from the start of the vertex buffer to the first vertex.
						minVertexIndex,//Minimum index value used during the call. Just an hint for directX to optimize performances.
						vertexCount,//Number of vertices used during the call. Just an hint for directX to optimize performances.
						startIndex,//Offset from the start of the index buffer to the first index to be used.
						primitiveCount
					)
				);
			}

			void SetRenderTarget(UINT renderTargetIndex, DF_Surface ^ renderTarget)
			{
				IDirect3DSurface9 * rt = renderTarget == nullptr ? NULL : renderTarget->GetNativePointer();
				DF_D3DErrors::Throw(dx_device->SetRenderTarget(renderTargetIndex, rt));
			}

			DF_Surface ^ GetRenderTarget(UINT renderTargetIndex)
			{
				IDirect3DSurface9 *rt;
				DF_D3DErrors::Throw(dx_device->GetRenderTarget(renderTargetIndex, &rt));
				return gcnew DF_Surface(rt);
			}

			void SetClipPlane(UINT index, float a, float b, float c, float d)
			{
				float plane[] = { a, b, c, d };
				DF_D3DErrors::Throw(dx_device->SetClipPlane(index, &plane[0]));
			}

			void SetRenderStateFLag(DF_RenderStateFlag flag, bool value)
			{
				DF_D3DErrors::Throw(dx_device->SetRenderState(static_cast<D3DRENDERSTATETYPE>(flag), value ? TRUE : FALSE));
			}

			void SetClipPlaneEnableMask(UINT mask)
			{
				DF_D3DErrors::Throw(dx_device->SetRenderState(D3DRS_CLIPPLANEENABLE, mask));
			}

			void SetFillMode(DF_FillMode f)
			{
				DF_D3DErrors::Throw(dx_device->SetRenderState(D3DRS_FILLMODE, static_cast<DWORD>(f)));
			}

			void SetSourceBlend(DF_BlendMode value)
			{
				DF_D3DErrors::Throw(dx_device->SetRenderState(D3DRS_SRCBLEND, static_cast<DWORD>(value)));
			}

			void SetDestinationBlend(DF_BlendMode value)
			{
				DF_D3DErrors::Throw(dx_device->SetRenderState(D3DRS_DESTBLEND, static_cast<DWORD>(value)));
			}

			void SetCullMode(DF_CullMode value)
			{
				DF_D3DErrors::Throw(dx_device->SetRenderState(D3DRS_CULLMODE, static_cast<DWORD>(value)));
			}

			void SetZFunction(DF_CompareFunc value)
			{
				DF_D3DErrors::Throw(dx_device->SetRenderState(D3DRS_ZFUNC, static_cast<DWORD>(value)));
			}

			void SetAlphaRef(UINT value)
			{
				value = value & 0xff;
				DF_D3DErrors::Throw(dx_device->SetRenderState(D3DRS_ALPHAREF, static_cast<DWORD>(value)));
				DF_D3DErrors::Throw(dx_device->SetRenderState(D3DRS_ALPHAFUNC, D3DCMP_GREATEREQUAL));
			}

			void SetAlphaFunction(DF_CompareFunc value)
			{
				DF_D3DErrors::Throw(dx_device->SetRenderState(D3DRS_ALPHAFUNC, static_cast<DWORD>(value)));
			}

			void SetTexture(UINT sampler, DF_Texture ^ texture)
			{
				IDirect3DTexture9 * tx = texture == nullptr ? NULL : texture->GetNativePointer();
				DF_D3DErrors::Throw(dx_device->SetTexture(static_cast<DWORD>(sampler), tx));
			}

			void SetVertexShaderTexture(UINT sampler, DF_Texture^ texture)
			{
				IDirect3DTexture9* tx = texture == nullptr ? NULL : texture->GetNativePointer();
				DF_D3DErrors::Throw(dx_device->SetTexture(static_cast<DWORD>(sampler) + D3DVERTEXTEXTURESAMPLER0, tx));
			}

			UINT GetDxColor(float r, float g, float b, float a)
			{
				return static_cast<UINT>(D3DCOLOR_COLORVALUE(r, g, b, a));
			}

			void SetSamplerState(UINT sampler, DF_SamplerStateType type, UINT value)
			{
				DF_D3DErrors::Throw(dx_device->SetSamplerState(static_cast<DWORD>(sampler), static_cast<D3DSAMPLERSTATETYPE>(type), static_cast<DWORD>(value)));
			}

			void SetSamplerState(UINT sampler, DF_SamplerStateType type, float value)
			{
				DF_D3DErrors::Throw(dx_device->SetSamplerState(static_cast<DWORD>(sampler), static_cast<D3DSAMPLERSTATETYPE>(type), *reinterpret_cast<DWORD *>(&value)));
			}

			void SetVertexShader(DF_VertexShader ^ vs)
			{
				IDirect3DVertexShader9 * vsp = vs == nullptr ? NULL : vs->GetNativePointer();
				DF_D3DErrors::Throw(dx_device->SetVertexShader(vsp));
			}

			void SetPixelShader(DF_PixelShader ^ ps)
			{
				IDirect3DPixelShader9 * psp = ps == nullptr ? NULL : ps->GetNativePointer();
				DF_D3DErrors::Throw(dx_device->SetPixelShader(psp));
			}

			DF_VertexShader ^ CreateVertexShader(cli::array<Byte> ^ compiledVS)
			{
				pin_ptr<Byte> pinPtrVS = &compiledVS[0];
				IDirect3DVertexShader9 *vs;
				DF_D3DErrors::Throw(dx_device->CreateVertexShader((DWORD*)pinPtrVS, &vs));
				return gcnew DF_VertexShader(vs);
			}

			DF_PixelShader ^ CreatePixelShader(cli::array<Byte> ^ compiledPS)
			{
				pin_ptr<Byte> pinPtrPS = &compiledPS[0];
				IDirect3DPixelShader9 *ps;
				DF_D3DErrors::Throw(dx_device->CreatePixelShader((DWORD*)pinPtrPS, &ps));
				return gcnew DF_PixelShader(ps);
			}

			void SetVertexShaderConstantB(UINT registerIndex, cli::array<bool> ^ c)
			{
				//convert .NET bools to c++ bools
				BOOL *constArray = new BOOL[c->Length];
				for (int ci = 0; ci < c->Length; ci++)
				{
					constArray[ci] = c[ci] ? TRUE : FALSE;
				}

				DF_D3DErrors::Throw(dx_device->SetVertexShaderConstantB(registerIndex, constArray, c->Length));

				delete[] constArray;
			}
			
			void SetVertexShaderConstantF(UINT registerIndex, cli::array<float> ^ c)
			{
				pin_ptr<float> pinPtrC = &c[0];
				DF_D3DErrors::Throw(dx_device->SetVertexShaderConstantF(registerIndex, pinPtrC, c->Length / 4));
			}

			void SetVertexShaderConstantI(UINT registerIndex, cli::array<int> ^ c)
			{
				pin_ptr<int> pinPtrC = &c[0];
				DF_D3DErrors::Throw(dx_device->SetVertexShaderConstantI(registerIndex, pinPtrC, c->Length / 4));
			}

			void SetPixelShaderConstantB(UINT registerIndex, cli::array<bool> ^ c)
			{
				//convert .NET bools to c++ bools
				BOOL *constArray = new BOOL[c->Length];
				for (int ci = 0; ci < c->Length; ci++)
				{
					constArray[ci] = c[ci] ? TRUE : FALSE;
				}

				DF_D3DErrors::Throw(dx_device->SetPixelShaderConstantB(registerIndex, constArray, c->Length));

				delete[] constArray;
			}

			void SetPixelShaderConstantF(UINT registerIndex, cli::array<float> ^ c)
			{
				pin_ptr<float> pinPtrC = &c[0];
				DF_D3DErrors::Throw(dx_device->SetPixelShaderConstantF(registerIndex, pinPtrC, c->Length / 4));
			}

			void SetPixelShaderConstantI(UINT registerIndex, cli::array<int> ^ c)
			{
				pin_ptr<int> pinPtrC = &c[0];
				DF_D3DErrors::Throw(dx_device->SetPixelShaderConstantI(registerIndex, pinPtrC, c->Length / 4));
			}

			DF_Surface ^ GetRenderTargetData(DF_Surface ^ renderTarget, UINT width, UINT height, DF_SurfaceFormat format)
			{
				IDirect3DSurface9 * rtSurfaceCopy;
				DF_D3DErrors::Throw(dx_device->CreateOffscreenPlainSurface(width, height, static_cast<D3DFORMAT>(format), D3DPOOL_SYSTEMMEM, &rtSurfaceCopy, NULL));
				HRESULT copyResult = dx_device->GetRenderTargetData(renderTarget->GetNativePointer(), rtSurfaceCopy);
				if (!DF_D3DErrors::Check(copyResult))
					rtSurfaceCopy->Release(); // release offscreen surface before throwing an exception
				DF_D3DErrors::Throw(copyResult); 
				return gcnew DF_Surface(rtSurfaceCopy);
			}

			void SetRenderTargetData(DF_Surface^ srcOffscreenSurface, DF_Surface^ destRenderTarget)
			{
				DF_D3DErrors::Throw(dx_device->UpdateSurface(srcOffscreenSurface->GetNativePointer(), NULL, destRenderTarget->GetNativePointer(), NULL));
			}

			DF_Surface ^ CopyRenderTarget(DF_Surface ^ renderTarget, UINT width, UINT height, DF_SurfaceFormat format)
			{
				IDirect3DSurface9 * rtCopy;
				DF_D3DErrors::Throw(dx_device->CreateRenderTarget(width, height, static_cast<D3DFORMAT>(format), D3DMULTISAMPLE_NONE, 0, FALSE, &rtCopy, NULL));
				DF_D3DErrors::Throw(dx_device->StretchRect(renderTarget->GetNativePointer(), NULL, rtCopy, NULL, D3DTEXF_NONE));
				return gcnew DF_Surface(rtCopy);
			}

			void SetTextureData(DF_Surface^ srcOffscreenSurface, DF_Surface^ destTextureSurface, UINT textureHeight)
			{
				// lock src surface
				D3DLOCKED_RECT lockedSrcRect = {};
				DF_D3DErrors::Throw(srcOffscreenSurface->GetNativePointer()->LockRect(&lockedSrcRect, NULL, D3DLOCK_READONLY));

				// lock dest surface
				D3DLOCKED_RECT lockedDestRect = {};
				DF_D3DErrors::Throw(destTextureSurface->GetNativePointer()->LockRect(&lockedDestRect, NULL, D3DLOCK_DISCARD));

				// copy data from src to dest
				INT copyPitch = min(lockedSrcRect.Pitch, lockedDestRect.Pitch);
				memcpy(lockedDestRect.pBits, lockedSrcRect.pBits, copyPitch * textureHeight);

				// unlock src and dest
				DF_D3DErrors::Check(srcOffscreenSurface->GetNativePointer()->UnlockRect());
				DF_D3DErrors::Check(destTextureSurface->GetNativePointer()->UnlockRect());
			}

			//finalizer and destructor for gc
			~DF_D3D9Device() { this->!DF_D3D9Device(); }
			!DF_D3D9Device() { Release(); }
		};

		DF_D3D9Device ^ DF_Directx3D9::CreateDevice(IntPtr targetHandle, bool fullScreen, int preferredWidth, int preferredHeight, bool antiAliasing)
		{
			if (!IsAvailable())
			{
				return nullptr;//device cannot be created
			}

			if (antiAliasing)
				defParams->MultiSampleType = SupportedMSAA(defParams->BackBufferFormat);

			D3DPRESENT_PARAMETERS dx_PresParams;
			fillPresParams(&dx_PresParams, this, targetHandle, fullScreen, preferredWidth, preferredHeight);

			LPDIRECT3DDEVICE9 device;
			DF_D3DErrors::Throw(dx_obj->CreateDevice(adapterIndex, deviceType, (HWND)targetHandle.ToPointer(), D3DCREATE_HARDWARE_VERTEXPROCESSING, &dx_PresParams, &device));
			DF_D3D9Device  ^ dfDevice = gcnew DF_D3D9Device(device);
			if (antiAliasing) dfDevice->SetRenderStateFLag(DF_RenderStateFlag::AntiAliasingEnable, true);
			dfDevice->SetZFunction(DF_CompareFunc::GreaterEqual); // inverted z by default

			return dfDevice;
		}

	}
}