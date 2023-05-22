#pragma once
#include <msclr\marshal.h>
#include <dxgi.h>
using namespace std;

namespace DragonflyGraphicsWrappers {

#pragma region DX Errors to Exceptions wrap

	/*====== DX Errors to Exceptions wrap =========*/

	/*
	* D3D Error Exceptions
	*/
	public ref class D3DException : public System::Exception
	{
	public:
		HRESULT ErrorCode;

	public:
		D3DException(HRESULT error);
		virtual System::String ^ ToString() override;
	};

	/*
	* Global Error-checking and logging.
	*/
	public ref class DF_D3DErrors abstract sealed
	{
	private:
		static HRESULT errorCode = 0;

	public:
		static long GetLastErrorCode();
		static bool Check(HRESULT res);
		static void Throw(HRESULT res);
		static bool LastCallSucceeded();

	};

#pragma endregion

#pragma region ENUMS
	/*================ ENUMS ==========*/

	public enum class DF_DeclType
	{
		Float = 1,
		Float2 = 2,
		Float3 = 3,
		Float4 = 4
	};

	public enum class DF_DeclUsage
	{
		Position = 0,//D3DDECLUSAGE_POSITION,
		TexCoord = 5,//D3DDECLUSAGE_TEXCOORD,
	};

	public enum class DF_CullMode
	{
		None = 1, //D3DCULL_NONE,
		CullClockwise = 2, //D3DCULL_CW,
		CullCounterClockwise = 3, //D3DCULL_CCW
	};

	public enum class DF_FillMode
	{
		Point = 1, //D3DFILL_POINT,
		Wireframe = 2, //D3DFILL_WIREFRAME,
		Solid = 3, //D3DFILL_SOLID
	};

	public enum class DF_BlendMode
	{
		Zero = 1, //D3DBLEND_ZERO,
		One = 2, //D3DBLEND_ONE,
		SrcColor = 3, //D3DBLEND_SRCCOLOR,
		InvSrcColor = 4, //D3DBLEND_INVSRCCOLOR,
		SrcAlpha = 5, //D3DBLEND_SRCALPHA,
		InvSrcAlpha = 6, //D3DBLEND_INVSRCALPHA,
		DestAlpha = 7, //D3DBLEND_DESTALPHA,
		InvDestAlpha = 8, //D3DBLEND_INVDESTALPHA,
		DestColor = 9, //D3DBLEND_DESTCOLOR,
		InvDestColor = 10, //D3DBLEND_INVDESTCOLOR,
		SrcAlphaSat = 11, //D3DBLEND_SRCALPHASAT,
		BlendFactor = 14, //D3DBLEND_BLENDFACTOR,
		InvBlendFactor = 15, //D3DBLEND_INVBLENDFACTOR,
		SrcColor2 = 16, //D3DBLEND_SRCCOLOR2,
		InvSrcColor2 = 17, //D3DBLEND_INVSRCCOLOR2,
	};

	public enum class DF_CompareFunc
	{
		Never = 1, //D3DCMP_NEVER,
		Less = 2, //D3DCMP_LESS,
		Equal = 3, //D3DCMP_EQUAL,
		LessEqual = 4, //D3DCMP_LESSEQUAL,
		Greater = 5, //D3DCMP_GREATER,
		NotEqual = 6, //D3DCMP_NOTEQUAL,
		GreaterEqual = 7, //D3DCMP_GREATEREQUAL,
		Always = 8, //D3DCMP_ALWAYS,
	};

	public enum class DF_SurfaceFormat
	{
		//color
		A8R8G8B8 = 21, //D3DFMT_A8R8G8B8,

		//half
		R16F = 111, //D3DFMT_R16F,
		G16R16F = 112, //D3DFMT_G16R16F,
		A16B16G16R16F = 113, //D3DFMT_A16B16G16R16F,

		//single
		R32F = 114, //D3DFMT_R32F,
		G32R32F = 115, //D3DFMT_G32R32F,
		A32B32G32R32F = 116, //D3DFMT_A32B32G32R32F,

		// depth-stencil, picked by api
		DEFAULT_DEPTH_FORMAT
	};

	/*
	* Specifies the type of the primitive to be drawn by the device.
	*/
	public enum class DF_PrimitiveType
	{
		PointList = 1,
		LineList = 2,
		LineStrip = 3,
		TriangleList = 4,
		TriangleStrip = 5
	};

	public enum class DF_TextureAddress {
		Wrap = 1, //D3DTADDRESS_WRAP, D3D12_TEXTURE_ADDRESS_MODE_WRAP 
		Mirror = 2, //D3DTADDRESS_MIRROR, D3D12_TEXTURE_ADDRESS_MODE_MIRROR 
		Clamp = 3, //D3DTADDRESS_CLAMP, D3D12_TEXTURE_ADDRESS_MODE_CLAMP 
		Border = 4, //D3DTADDRESS_BORDER, D3D12_TEXTURE_ADDRESS_MODE_BORDER 
	};

#pragma endregion

#pragma region STRUCTURES

	/*======== STRUCTURES =================*/

	/*
	* A single valid display mode
	*/
	public value struct DF_DisplayMode
	{
	public:
		int Width;
		int Height;
		int RefreshRate;
	};

	/*
	* Informations on a single field of a custom vertex.
	*/
	public value struct DF_VertexElement
	{
	public:
		WORD    Stream;     // Stream index
		WORD    Offset;     // Offset in the stream in bytes
		DF_DeclType    Type;       // Data type
		DF_DeclUsage    Usage;      // Semantics
		BYTE    UsageIndex; // Semantic index
	};

#pragma endregion

#pragma region UTILITY CLASSES


	#define MAX_RES_HEAP_COUNT 10
	#define TrackComPtr(Type, Value) reinterpret_cast<Type**>(&comPtrList[AddComPtr(Value)])
	#define MakeComPtr(Type) TrackComPtr(Type, NULL)
	#define MakeComPtrList(Type, size) reinterpret_cast<Type**>(&comPtrList[AddComPtrList(size)])
	
	/// <summary>
	/// A managed class that store and manage com ptr
	/// </summary>
	public ref class DF_Resource abstract
	{
	private:
		static int nextID = 0;

	private:
		int id, ptrCount;
		bool released;

	protected:
		IUnknown ** comPtrList;

	public:
		static int ReserveNewID()
		{
			return nextID++;
		}

	public:
		DF_Resource() : id(ReserveNewID()), ptrCount(0)
		{
			comPtrList = new IUnknown*[MAX_RES_HEAP_COUNT];
		}

		DF_Resource(IUnknown * comPtr)
		{
			this->DF_Resource::DF_Resource();
			AddComPtr(comPtr);
		}

		int GetResourceHash() { return id; }

		/// <summary>
		/// Adds an external com pointer to the list of tracked ones, and returns its index
		/// </summary>
		int AddComPtr(IUnknown * comPtr)
		{
			int id = AddComPtrList(1);
			comPtrList[id] = comPtr;
			return id;
		}

		/// <summary>
		/// Add a list of null initialized com pointers to the list of tracked ones.
		/// </summary>
		int AddComPtrList(int count)
		{
			if ((ptrCount + count) > MAX_RES_HEAP_COUNT)
				throw gcnew System::Exception("Com pointer limit exceeded!");

			int startID = ptrCount;
			ptrCount += count;
			for(int i = startID; i < ptrCount; i++)
				comPtrList[i] = NULL;
			return startID;
		}

		virtual void Release()
		{
			if (!released)
			{
				for(int i = 0; i < ptrCount; i++)
					if (comPtrList[i]) comPtrList[i]->Release();

				delete comPtrList;
				released = true;
			}
		}

		bool IsReleased()
		{
			return released;
		}

		//finalizer and destructor for gc
		virtual ~DF_Resource() { this->!DF_Resource(); }
		!DF_Resource()
		{
			Release();
		}

	internal:
		IUnknown * Get()
		{
			return this->comPtrList[0];
		}

	};


	ref class DF_Display abstract sealed
	{
	internal:
		static System::Collections::Generic::List<DF_DisplayMode>^ GetDisplayModesFromOutput(IDXGIOutput* output);

		static void GetDefaultOutput(IDXGIOutput** output);
	};

	public ref class Pix abstract sealed
	{
	public:
		static void BeginEvent(System::String^ eventName, System::Byte r, System::Byte g, System::Byte b);

		static void EndEvent();
	};

#pragma endregion

}