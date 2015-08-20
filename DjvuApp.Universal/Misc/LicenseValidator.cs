using System;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace DjvuApp.Misc
{
    public sealed class LicenseValidator
    {
        public static LicenseValidator Current { get; } = new LicenseValidator();

        private bool? _hasLicense;

        LicenseValidator()
        {
#if !PRODUCTION
            _hasLicense = true;
#endif
        }

        public async Task<bool> GetIsLicensedAsync()
        {
            if (_hasLicense == null)
            {
                try
                {
                    await Package.Current.InstalledLocation.GetFileAsync("AppxSignature.p7x");
                    _hasLicense = true;
                }
                catch (FileNotFoundException)
                {
                    _hasLicense = false;
                }
            }


            return _hasLicense.Value;
        }
    }
}
