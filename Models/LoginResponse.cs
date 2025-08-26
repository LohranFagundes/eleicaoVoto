namespace VoteHomWebApp.Models
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public int VoterId { get; set; }
        public string? VoterName { get; set; }
        public int ElectionId { get; set; }
        public string? ElectionTitle { get; set; }
    }
}
