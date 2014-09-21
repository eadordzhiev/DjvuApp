#include "pch.h"
#include "LicenseValidator.h"
#include <ppltasks.h>
#include <collection.h>

using namespace std;
using namespace Concurrency;
using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::ApplicationModel;
using namespace Windows::Storage;
using namespace DjvuLibRT;

static task<bool> CheckLicense()
{
	//#define PRODUCTION
	DBGPRINT(L"Checking license\n");
#ifdef PRODUCTION
	auto applicationFolder = Windows::ApplicationModel::Package::Current->InstalledLocation;

	return create_task(applicationFolder->GetFilesAsync())
		.then([](IVectorView<StorageFile^>^ files)
	{
		for (auto file : files)
		{
			if (file->Name == "AppxSignature.p7x")
				return true;
		}

		return false;
	});
#else
	return task_from_result(true);
#endif
}

static task<bool> CheckLicenseStealth()
{
	DBGPRINT(L"Checking license stealth\n");
	auto applicationFolder = Package::Current->InstalledLocation;

	return create_task(applicationFolder->GetFolderAsync("Pages"))
		.then([](StorageFolder^ folder)
	{
		return create_task(folder->GetFilesAsync())
			.then([](IVectorView<StorageFile^>^ files)
		{
			int counter = 0;
			for (auto file : files)
			{
				if (file->Name == "AboutPage.xbf"
					|| file->Name == "MainPage.xbf"
					|| file->Name == "ViewerPage.xbf")
				{
					counter++;
				}
			}

			return counter >= 3;
		});
	});
}

static enum class LicenseStatus
{
	NotChecked,
	NoLicense,
	HasLicense
};

IAsyncOperation<bool>^ LicenseValidator::GetLicenseStatusAsync()
{
	static auto licenseStatus = LicenseStatus::NotChecked;

	return create_async([]
	{
		if (licenseStatus == LicenseStatus::NotChecked)
		{
			return CheckLicense()
				.then([](bool hasLicense)
			{
				licenseStatus = hasLicense ? LicenseStatus::HasLicense : LicenseStatus::NoLicense;
				return hasLicense;
			});
		}
		else
		{
			return task_from_result(licenseStatus == LicenseStatus::HasLicense);
		}
	});
}

task<bool> LicenseValidator::GetLicenseStatusStealth()
{
	static auto licenseStatus = LicenseStatus::NotChecked;

	if (licenseStatus == LicenseStatus::NotChecked)
	{
		return CheckLicenseStealth()
			.then([](bool hasLicense)
		{
			licenseStatus = hasLicense ? LicenseStatus::HasLicense : LicenseStatus::NoLicense;
			return hasLicense;
		});
	}
	else
	{
		return task_from_result(licenseStatus == LicenseStatus::HasLicense);
	}
}