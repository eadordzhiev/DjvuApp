using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.WindowsAzure.MobileServices;

namespace DjvuApp.Misc.TrialExperience
{
    public sealed class AzureTrialService : ITrialService
    {
        private readonly IMobileServiceClient _service;

        public AzureTrialService(IMobileServiceClient service)
        {
            _service = service;
            TrialPeriod = TimeSpan.FromDays(7);
        }

        public TimeSpan TrialPeriod { get; set; }

        private string GetDeviceId()
        {
            var token = Windows.System.Profile.HardwareIdentification.GetPackageSpecificToken(null);
            var hardwareId = token.Id;
            var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(hardwareId);

            byte[] bytes = new byte[hardwareId.Length];
            dataReader.ReadBytes(bytes);

            return BitConverter.ToString(bytes).Replace("-", "");
        }

        public async Task<DateTimeOffset> GetExpirationDate<TUser>() where TUser : TrialUser, new()
        {
            var userId = GetDeviceId();

            object dateVal;
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue("trialExpiration", out dateVal) == false)
            {
                // get user from server
                IMobileServiceTable<TUser> table = _service.GetTable<TUser>();

                var users = await table.Where(userInfo => userInfo.Id == userId).ToListAsync();
                var user = users.FirstOrDefault();

                if (user == null)
                {
                    // new user, add it
                    var trialExpirationDate = DateTimeOffset.Now + TrialPeriod;
                    dateVal = trialExpirationDate;
                    user = new TUser { Id = userId, TrialExpirationDate = trialExpirationDate.ToUniversalTime() };
                    await table.InsertAsync(user);
                }
                else
                {
                    // mobile services will deserialize as local DateTime
                    dateVal = user.TrialExpirationDate;
                }
                ApplicationData.Current.LocalSettings.Values["trialExpiration"] = dateVal;
            }
            var expirationDate = (DateTimeOffset)dateVal;
            return expirationDate;
        }
    }
}