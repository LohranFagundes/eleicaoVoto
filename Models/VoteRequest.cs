namespace VoteHomWebApp.Models
{
    public class VoteRequest
    {
        public int? Step1CandidateId { get; set; }
        public int? Step2CandidateId { get; set; }
        public string VoteType { get; set; } = "candidate"; // candidate, blank, null
        public int CurrentStep { get; set; } = 1;
    }
}