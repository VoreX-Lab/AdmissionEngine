using AdmissionEngine.API.Models.Entities;

namespace AdmissionEngine.API.Services.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
    }
}