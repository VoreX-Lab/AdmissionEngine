using AdmissionEngine.API.Models.DTOS;
using AdmissionEngine.API.Models.Entities;

namespace AdmissionEngine.API.Services.Interfaces
{
    public interface IEligibilityEvaluator
    {
        EvaluationResultDto EvaluateCandidate(User candidate, AdmissionRule rule);
    }
}