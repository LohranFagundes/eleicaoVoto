namespace VoteHomWebApp.Models
{
    public class ElectionInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsVotingPeriod
        {
            get
            {
                var now = DateTime.Now;
                return now >= StartDate && now <= EndDate;
            }
        }
    }
}