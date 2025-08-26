namespace VoteHomWebApp.Models
{
    public class MultiplePositionsTestResult
    {
        public int ElectionId { get; set; }
        public string ElectionTitle { get; set; } = string.Empty;
        public bool HasMultiplePositions { get; set; }
        public string Message { get; set; } = string.Empty;
        public string RequiredVotingMethod { get; set; } = string.Empty;
    }

    public class VotingTestResult
    {
        public int ElectionId { get; set; }
        public string ElectionTitle { get; set; } = string.Empty;
        public int PositionsProvided { get; set; }
        public string ValidationResult { get; set; } = string.Empty;
        public string ValidationMessage { get; set; } = string.Empty;
        public List<TestVoteChoice> ProvidedVotes { get; set; } = new List<TestVoteChoice>();
    }

    public class TestVoteChoice
    {
        public int PositionId { get; set; }
        public string PositionName { get; set; } = string.Empty;
        public int? CandidateId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public bool IsBlankVote { get; set; }
        public bool IsNullVote { get; set; }
    }

    public class SystemIntegrityTestResult
    {
        public int ElectionId { get; set; }
        public string ElectionTitle { get; set; } = string.Empty;
        public SystemIntegrityTests SystemIntegrityTests { get; set; } = new SystemIntegrityTests();
        public string OverallStatus { get; set; } = string.Empty;
        public DateTime TestedAt { get; set; }
    }

    public class SystemIntegrityTests
    {
        public IntegrityTestComponent MultiplePositionsDetection { get; set; } = new IntegrityTestComponent();
        public IntegrityTestComponent ElectionValidation { get; set; } = new IntegrityTestComponent();
        public IntegrityTestComponent IntegrityValidation { get; set; } = new IntegrityTestComponent();
    }

    public class IntegrityTestComponent
    {
        public bool Success { get; set; }
        public bool HasMultiplePositions { get; set; }
        public bool IsValid { get; set; }
        public bool IntegrityValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ValidationMessage { get; set; } = string.Empty;
    }
}