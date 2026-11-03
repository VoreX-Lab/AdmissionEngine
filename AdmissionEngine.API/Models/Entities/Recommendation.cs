namespace AdmissionEngine.API.Models.Entities
{
    public class Recommendation
    {
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public double MatchPercentage { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}