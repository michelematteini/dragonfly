#include "DirectxCommon.h"
#include <new>
#include <comdef.h>
#include "atlbase.h"
#include <D3Dcompiler.h>
#include <msclr\marshal.h>
#include <dxgi.h>
#include "Pix.h"

using namespace System;
using namespace msclr::interop;
using namespace msclr::interop;
using namespace System::Runtime::InteropServices;

namespace DragonflyGraphicsWrappers 
{

	D3DException::D3DException(HRESULT error) : ErrorCode(error)
	{
	}

	String ^ D3DException::ToString()
	{
		marshal_context context;
		_com_error comError(ErrorCode);
		return "Error Code: 0x" + ErrorCode.ToString("X") + " " + context.marshal_as<String ^>(comError.ErrorMessage());
	}

	long DF_D3DErrors::GetLastErrorCode()
	{
		return (long)errorCode;
	}

	bool DF_D3DErrors::Check(HRESULT res)
	{
		errorCode = res;
		return LastCallSucceeded();
	}

	void DF_D3DErrors::Throw(HRESULT res)
	{
		errorCode = res;

		if (!LastCallSucceeded())
			throw gcnew D3DException(res);
	}

	bool DF_D3DErrors::LastCallSucceeded()
	{
		return !FAILED(errorCode);
	}	

	public enum class CompileFlags
	{
		None = 0,
		Optimize = 1 << 0,
		EnableUnboundedDescrTables = 1 << 1
	};

	public ref class DirectxNativeUtils abstract sealed
	{
	public:
		static cli::array<Byte>^ CompileShader(String^ shaderSrcCode, String^ entryPointName, String^ compilerTarget/*e.g "vs_5_0" */, String^% errors, CompileFlags flags)
		{
			UINT compileFlags = 0;
			if(flags.HasFlag(CompileFlags::Optimize))
				compileFlags |= D3DCOMPILE_OPTIMIZATION_LEVEL3;
			else
				compileFlags |= (D3DCOMPILE_DEBUG | D3DCOMPILE_SKIP_OPTIMIZATION);

			if (flags.HasFlag(CompileFlags::EnableUnboundedDescrTables))
				compileFlags |= D3DCOMPILE_ENABLE_UNBOUNDED_DESCRIPTOR_TABLES;

			CComPtr<ID3DBlob> precompShader;
			CComPtr<ID3DBlob> compileErrors;

			// try to compile
			marshal_context context;
			bool compileSucceeded = DF_D3DErrors::Check(D3DCompile(
				context.marshal_as<const CHAR*>(shaderSrcCode),
				shaderSrcCode->Length,
				NULL,
				nullptr, nullptr,
				context.marshal_as<const CHAR*>(entryPointName),
				context.marshal_as<const CHAR*>(compilerTarget),
				compileFlags, 0, &precompShader, &compileErrors
			));

			// parse errors and warnings
			if (compileErrors.p)
			{
				char* c_errorMsg = (char*)compileErrors->GetBufferPointer();
				errors = context.marshal_as<System::String^>(c_errorMsg);
			}

			// return null if compilation failed
			if (!compileSucceeded)
			{		
				return nullptr;
			}

			// copy result to clr array
			cli::array<Byte>^ clrPrecompShader = gcnew cli::array<Byte>((int)precompShader->GetBufferSize());
			pin_ptr<Byte> pinPtrPrecompShader = &clrPrecompShader[0];
			memcpy(pinPtrPrecompShader, precompShader->GetBufferPointer(), precompShader->GetBufferSize());

			return clrPrecompShader;
		}

		static String^ DisassembleShader(cli::array<Byte>^ precompShader, int startIndex, int byteLength)
		{
			// disassemble from precompiled
			pin_ptr<Byte> pinPtrPrecompShader = &precompShader[startIndex];
			CComPtr<ID3DBlob> disassembly;
			bool disassembled = DF_D3DErrors::Check(D3DDisassemble(pinPtrPrecompShader, byteLength, 0, NULL, &disassembly));
			if (!disassembled)
				return "";

			// parse disassembly to clr String and return it
			const char* disassebledAscii = static_cast<const char*>(disassembly->GetBufferPointer());
			return gcnew String(disassebledAscii);
		}

		static String^ DisassembleShader(cli::array<Byte>^ precompShader)
		{
			return DisassembleShader(precompShader, 0, precompShader->Length);
		}
	};

	System::Collections::Generic::List<DF_DisplayMode>^ DF_Display::GetDisplayModesFromOutput(IDXGIOutput* output)
	{
		System::Collections::Generic::List<DF_DisplayMode>^ clrDisplayModes = gcnew System::Collections::Generic::List<DF_DisplayMode>();

		// get display modes
		UINT displayModeCount = 0;
		output->GetDisplayModeList(DXGI_FORMAT_R8G8B8A8_UNORM, 0, &displayModeCount, NULL);
		unique_ptr<DXGI_MODE_DESC> displayModes = unique_ptr<DXGI_MODE_DESC>(new DXGI_MODE_DESC[displayModeCount]);
		output->GetDisplayModeList(DXGI_FORMAT_R8G8B8A8_UNORM, 0, &displayModeCount, displayModes.get());

		// copy modes to a clr list
		DF_DisplayMode curMode;
		for (UINT i = 0; i < displayModeCount; i++)
		{
			curMode.Width = displayModes.get()[i].Width;
			curMode.Height = displayModes.get()[i].Height;
			curMode.RefreshRate = displayModes.get()[i].RefreshRate.Numerator / displayModes.get()[i].RefreshRate.Denominator;
			clrDisplayModes->Add(curMode);
		}

		return clrDisplayModes;
	}
	
	void DF_Display::GetDefaultOutput(IDXGIOutput** output)
	{
		// retrieve the default dgxi output
		CComPtr<IDXGIFactory> dxgiFactory;
		DF_D3DErrors::Throw(CreateDXGIFactory1(__uuidof(IDXGIFactory), reinterpret_cast<void**>(&dxgiFactory)));
		CComPtr<IDXGIAdapter> defaultAdapter;
		DF_D3DErrors::Throw(dxgiFactory->EnumAdapters(0, &defaultAdapter));
		DF_D3DErrors::Throw(defaultAdapter->EnumOutputs(0, output));
	}

	void Pix::BeginEvent(System::String^ eventName, System::Byte r, System::Byte g, System::Byte b)
	{
		System::IntPtr eventNamePtr = System::Runtime::InteropServices::Marshal::StringToHGlobalAnsi(eventName);
		PCSTR cstrEventName = static_cast<PCSTR>(eventNamePtr.ToPointer());
		PIXBeginEvent(PIX_COLOR(r, g, b), cstrEventName);
		System::Runtime::InteropServices::Marshal::FreeHGlobal(eventNamePtr);
	}

	void Pix::EndEvent()
	{
		PIXEndEvent();
	}
}