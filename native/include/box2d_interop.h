#pragma once

#include <cstdint>

#ifdef _WIN32
#define EXPORT_API extern "C" __declspec(dllexport)
#else
#define EXPORT_API extern "C"
#endif

EXPORT_API int32_t run();
