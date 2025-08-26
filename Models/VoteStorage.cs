namespace VoteHomWebApp.Models
{
    public class VoteStorage
    {
        public string VoterCpf { get; set; } = string.Empty;
        public int ElectionId { get; set; }
        public List<VoteChoice> Votes { get; set; } = new List<VoteChoice>();
        public DateTime VotedAt { get; set; }
        public string ReceiptToken { get; set; } = string.Empty;
        public string VoteHash { get; set; } = string.Empty;
    }

    public class VoteChoice
    {
        public int PositionId { get; set; }
        public string PositionName { get; set; } = string.Empty;
        public int? CandidateId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public string CandidateNumber { get; set; } = string.Empty;
        public bool IsBlankVote { get; set; }
        public bool IsNullVote { get; set; }
        public string? Justification { get; set; }
    }
}