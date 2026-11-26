namespace KABILIN.Models
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Family = "Family";
    }

    /// <summary>Primary keys of the seeded rows in the Statuses table.</summary>
    public static class StatusIds
    {
        public const int Submitted = 1;     // legacy "Pending"
        public const int Processing = 2;    // unchanged
        public const int Completed = 3;     // legacy "Released"
        public const int UnderReview = 4;
        public const int Incomplete = 5;
        public const int Approved = 6;
        public const int Rejected = 7;

        /// <summary>Order in which statuses are presented to people.</summary>
        public static readonly int[] DisplayOrder =
            { Submitted, UnderReview, Incomplete, Processing, Approved, Completed, Rejected };

        public static bool IsClosed(int id) => id == Completed || id == Rejected;

        /// <summary>Allowed status transitions (current -> permitted next statuses).</summary>
        public static readonly IReadOnlyDictionary<int, int[]> Transitions =
            new Dictionary<int, int[]>
            {
                [Submitted]   = new[] { UnderReview, Rejected },
                [UnderReview] = new[] { Incomplete, Processing, Rejected },
                [Incomplete]  = new[] { UnderReview, Rejected },
                [Processing]  = new[] { Approved, Incomplete, Rejected },
                [Approved]    = new[] { Completed, Rejected },
                [Completed]   = Array.Empty<int>(),
                [Rejected]    = Array.Empty<int>()
            };
    }

    public static class RequestTypeIds
    {
        public const int DeathCertificate = 1;
        public const int BurialPermit = 2;
        public const int InsuranceClaims = 3;
        public const int BenefitsAssistance = 4;
    }

    public static class VerificationStates
    {
        public const string Pending = "Pending";
        public const string Verified = "Verified";
        public const string Incomplete = "Incomplete";
    }
}
