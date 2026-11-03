using AdmissionEngine.API.Models.Entities;
using AdmissionEngine.API.Models.Enums;

namespace AdmissionEngine.API.Models.DTOS
{
    public class EvaluationResultDto
    {
        public string? StudentId { get; set; }
        public bool IsEligible { get; set; } = true;
        public AdmissionStatus Status { get; set; } = AdmissionStatus.Pending;
        public string TargetCourse { get; set; } = string.Empty;
        public List<string> Remarks { get; set; } = new();
        public List<Recommendation> RecommendedCourses { get; set; } = new();
    }
}