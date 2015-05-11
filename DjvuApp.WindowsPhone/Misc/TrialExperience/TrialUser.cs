using System;

namespace DjvuApp.Misc.TrialExperience
{
    /// <summary>
    /// Represents information for a trial user.
    /// </summary>
    public abstract class TrialUser
    {
        /// <summary>
        /// Gets or sets the id for the user.
        /// </summary>
        public abstract string Id { get; set; }

        /// <summary>
        /// Gets or sets the date the trial will expire for the user.
        /// </summary>
        public abstract DateTimeOffset TrialExpirationDate { get; set; }
    }
}