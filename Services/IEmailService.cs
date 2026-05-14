namespace Semester_Project.Services
{
    public interface IEmailService
    {
        Task SendOTPEmailAsync(string toEmail, string otp);
        Task SendStudentCredentialsEmailAsync(string toEmail, string name, string password, string permanentId);
    }
}
