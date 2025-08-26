using VoteHomWebApp.Services;

namespace VoteHomWebApp.Models
{
    public class MultipleVoteModel
    {
        public List<SingleVoteChoice> Votes { get; set; } = new List<SingleVoteChoice>();
        public string? Justification { get; set; }
    }

    public class SingleVoteChoice
    {
        public int PositionId { get; set; }
        public string PositionName { get; set; } = string.Empty;
        public int? CandidateId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public bool IsBlankVote { get; set; }
        public bool IsNullVote { get; set; }
        public string VoteType { get; set; } = "candidate"; // candidate, blank, null
    }

    public class MultipleVoteSession
    {
        public List<Position> AllPositions { get; set; } = new List<Position>();
        public Dictionary<int, SingleVoteChoice> VoteChoices { get; set; } = new Dictionary<int, SingleVoteChoice>();
        public int CurrentPositionIndex { get; set; } = 0;
        public bool IsComplete => VoteChoices.Count == AllPositions.Count;
        public Position? CurrentPosition => CurrentPositionIndex < AllPositions.Count ? AllPositions[CurrentPositionIndex] : null;
    }
}