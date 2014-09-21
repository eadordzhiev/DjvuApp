using System;
using System.Threading.Tasks;

namespace DjvuApp.Misc.TrialExperience
{
    public interface ITrialService
    {
        /// <summary>
        /// Get or sets the time amount of time the user is allowed to use the trial.
        /// </summary>
        TimeSpan TrialPeriod { get; set; }

        /// <summary>
        /// Gets a value indicating if the trial has expired for the given userId.
        /// </summary>
        /// <returns>A task that will complete when the data is retrieved.</returns>
        Task<DateTimeOffset> GetExpirationDate<TUser>() where TUser : TrialUser, new();
    }
}