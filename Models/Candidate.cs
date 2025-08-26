
namespace VoteHomWebApp.Models
{
    public class Candidate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string Party { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;
        public string PhotoBase64 { get; set; } = string.Empty;
        public int PositionId { get; set; }
        public string PositionName { get; set; } = string.Empty;
        public int OrderPosition { get; set; }
    }
}
