using System;

namespace DjvuApp.Misc.TrialExperience
{
    public sealed class DjvuReaderUserInfo : TrialUser
    {
        public override string Id { get; set; }
        public override DateTimeOffset TrialExpirationDate { get; set; }
    }
}
