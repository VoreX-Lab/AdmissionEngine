using AdmissionEngine.API.Models.DTOS;

namespace AdmissionEngine.API.Services.Interfaces
{
    public interface IAdmissionEngineService
    {
        Task<EvaluationResultDto> EvaluateApplicantAsync(string studentId);
        Task<string> RespondToCourseOfferAsync(OfferResponseDto response);
    }
}