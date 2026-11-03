using AdmissionEngine.API.Models.Entities;

namespace AdmissionEngine.API.Services.Interfaces
{
    public interface IRedistributionEngine
    {
        Task<List<Recommendation>> RecommendAlternativeCoursesAsync(User candidate, List<AdmissionRule> rules);
    }
}