namespace Semester_Project.Services
{
    public interface IJwtService
    {
        string GenerateToken(string email, string role, int userId);
        bool ValidateToken(string token);
        string? GetEmailFromToken(string token);
        string? GetRoleFromToken(string token);
    }
}
