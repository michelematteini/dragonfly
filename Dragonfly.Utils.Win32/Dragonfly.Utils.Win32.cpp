// This is the main DLL file.
#include <Windows.h>

namespace DragonflyUtils {

	public ref class Win32 abstract sealed
	{
	public:
		static bool IsWindowIdle()
		{
			MSG msg;
			return PeekMessage(&msg, NULL, 0, 0, 0) == FALSE;
		}

		static void GetKeyboardStates(array<byte> ^ kstate)
		{
			pin_ptr<byte> pptr_kstate = &(kstate[0]);
			GetKeyState(0);
			GetKeyboardState(pptr_kstate);
		}

		static bool IsTopLevelWindowHandle(System::IntPtr handle)
		{
			HWND winHandle = (HWND)handle.ToPointer();
			return winHandle == GetAncestor(winHandle, GA_ROOT);
		}

		static System::IntPtr GetTopLevelWindowHandle(System::IntPtr handle)
		{
			return System::IntPtr(GetAncestor((HWND)handle.ToPointer(), GA_ROOT));
		}
	};

	public enum class WmActivate
	{
		Inactive = WA_INACTIVE,
		Active = WA_ACTIVE,
		ClickActive = WA_CLICKACTIVE
	};

	public enum class MsgType
	{
		Activate = WM_ACTIVATE,
		KeyDown = WM_KEYDOWN,
		KeyUp = WM_KEYUP,
		MouseMove = WM_MOUSEMOVE ,
		LButtonDown = WM_LBUTTONDOWN ,
		LButtonUp = WM_LBUTTONUP,
		LButtonDblClick = WM_LBUTTONDBLCLK,
		RButtonDown = WM_RBUTTONDOWN,
		RButtonUp = WM_RBUTTONUP,
		MouseWheel = WM_MOUSEWHEEL
	};
}