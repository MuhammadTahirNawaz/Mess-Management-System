namespace Semester_Project.Services
{
    public class MockEmailService : IEmailService
    {
        private readonly ILogger<MockEmailService> _logger;

        public MockEmailService(ILogger<MockEmailService> logger)
        {
            _logger = logger;
        }

        public Task SendOTPEmailAsync(string toEmail, string otp)
        {
            // Log OTP to console instead of sending email
            _logger.LogWarning($"===== MOCK EMAIL SERVICE =====");
            _logger.LogWarning($"OTP for {toEmail}: {otp}");
            _logger.LogWarning($"==============================");
            
            Console.WriteLine("\n");
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║     MOCK EMAIL - OTP CODE              ║");
            Console.WriteLine("╠════════════════════════════════════════╣");
            Console.WriteLine($"║  Email: {toEmail,27} ║");
            Console.WriteLine($"║  OTP:   {otp,27} ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.WriteLine("\n");

            return Task.CompletedTask;
        }

        public Task SendStudentCredentialsEmailAsync(string toEmail, string name, string password, string permanentId)
        {
            _logger.LogWarning($"===== MOCK EMAIL SERVICE =====");
            _logger.LogWarning($"Student credentials for {toEmail}");
            _logger.LogWarning($"==============================");
            
            Console.WriteLine("\n");
            Console.WriteLine("╔════════════════════════════════════════════╗");
            Console.WriteLine("║   MOCK EMAIL - STUDENT CREDENTIALS         ║");
            Console.WriteLine("╠════════════════════════════════════════════╣");
            Console.WriteLine($"║  Name:         {name,27} ║");
            Console.WriteLine($"║  Permanent ID: {permanentId,27} ║");
            Console.WriteLine($"║  Email:        {toEmail,27} ║");
            Console.WriteLine($"║  Password:     {password,27} ║");
            Console.WriteLine("╚════════════════════════════════════════════╝");
            Console.WriteLine("\n");

            return Task.CompletedTask;
        }
    }
}
