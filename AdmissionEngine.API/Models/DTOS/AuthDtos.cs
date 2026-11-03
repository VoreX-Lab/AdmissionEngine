namespace AdmissionEngine.API.Models.DTOs
{
    public record LoginDto(string Email, string Password);

    public record UserDataDto(
        string _id,
        string FullName,
        string Email,
        string Role,
        string Token
    );

    public record ApiResponse<T>(
        bool Success,
        string Message,
        T Data
    );
}