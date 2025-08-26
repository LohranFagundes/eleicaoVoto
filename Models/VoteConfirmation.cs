namespace VoteHomWebApp.Models
{
    public class VoteConfirmation
    {
        public string VoterName { get; set; } = string.Empty;
        public string ElectionName { get; set; } = string.Empty;
        public string VoteId { get; set; } = string.Empty;
        public DateTime VoteDateTime { get; set; }
        public string CPF { get; set; } = string.Empty;
    }
}