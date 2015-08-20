using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace DjvuApp.Common
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
