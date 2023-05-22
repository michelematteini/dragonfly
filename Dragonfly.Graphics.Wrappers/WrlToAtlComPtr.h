#pragma once

// TODO: comment out #include <wrl\client.h> before including
#include "atlbase.h"
#define ComPtr ATL::CComPtr
#define GetAddressOf() operator&()
#define Get() p
#define As(x) QueryInterface(x)
#define Reset() Release()