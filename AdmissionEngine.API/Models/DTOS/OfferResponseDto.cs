namespace AdmissionEngine.API.Models.DTOS
{
    public class OfferResponseDto
    {
        public string? StudentId { get; set; }
        public string? CourseId { get; set; }
        public bool Accept { get; set; }
    }
}
