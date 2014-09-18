#pragma once

#include <ppltasks.h>

using namespace Concurrency;
using namespace Windows::Foundation;

namespace DjvuLibRT
{
	public ref class LicenseValidator sealed
	{
	public:
		static IAsyncOperation<bool>^ GetLicenseStatusAsync();
	internal:
		static task<bool> GetLicenseStatusStealth();
	};
}