using System.Net;
using System.Net.Mail;

namespace Semester_Project.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendOTPEmailAsync(string toEmail, string otp)
        {
            try
            {
                var smtpHost = _configuration["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];
                var senderName = _configuration["EmailSettings:SenderName"];

                // Validate email configuration
                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
                {
                    throw new Exception("Email settings are not configured. Please update appsettings.json with your Gmail credentials.");
                }

                if (senderEmail == "your-email@gmail.com" || senderPassword == "your-app-password-here")
                {
                    throw new Exception("Please configure your actual Gmail address and App Password in appsettings.json");
                }

                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(senderEmail, senderPassword)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail!, senderName),
                    Subject = "Your OTP Code - Mess Management System",
                    Body = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                                <h2 style='color: #333;'>Mess Management System</h2>
                                <p>Hello Admin,</p>
                                <p>Your One-Time Password (OTP) for login is:</p>
                                <h1 style='color: #007bff; font-size: 36px; letter-spacing: 5px; text-align: center;'>{otp}</h1>
                                <p style='color: #666;'>This OTP is valid for 10 minutes.</p>
                                <p style='color: #666;'>If you did not request this OTP, please ignore this email.</p>
                                <hr style='margin-top: 20px; border: none; border-top: 1px solid #ddd;'>
                                <p style='color: #999; font-size: 12px;'>This is an automated email. Please do not reply.</p>
                            </div>
                        </body>
                        </html>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"OTP email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send OTP email to {toEmail}");
                throw new Exception($"Email sending failed: {ex.Message}", ex);
            }
        }

        public async Task SendStudentCredentialsEmailAsync(string toEmail, string name, string password, string permanentId)
        {
            try
            {
                var smtpHost = _configuration["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];
                var senderName = _configuration["EmailSettings:SenderName"];

                // Validate email configuration
                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
                {
                    throw new Exception("Email settings are not configured. Please update appsettings.json with your Gmail credentials.");
                }

                if (senderEmail == "your-email@gmail.com" || senderPassword == "your-app-password-here")
                {
                    throw new Exception("Please configure your actual Gmail address and App Password in appsettings.json");
                }

                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(senderEmail, senderPassword)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail!, senderName),
                    Subject = "Welcome to Mess Management System - Your Login Credentials",
                    Body = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                                <h2 style='color: #333;'>Welcome to Mess Management System</h2>
                                <p>Hello <strong>{name}</strong>,</p>
                                <p>Your account has been successfully created! Below are your login credentials:</p>
                                
                                <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                                    <p style='margin: 5px 0;'><strong>Permanent ID:</strong> <span style='color: #28a745; font-size: 22px; font-weight: bold; letter-spacing: 2px;'>{permanentId}</span></p>
                                    <p style='margin: 5px 0;'><strong>Email:</strong> {toEmail}</p>
                                    <p style='margin: 5px 0;'><strong>Temporary Password:</strong> <span style='color: #007bff; font-size: 18px; font-weight: bold;'>{password}</span></p>
                                </div>

                                <div style='background-color: #d1ecf1; padding: 15px; border-radius: 5px; border-left: 4px solid #0c5460; margin: 20px 0;'>
                                    <p style='margin: 0; color: #0c5460;'><strong>🆔 Your Permanent ID:</strong> {permanentId}</p>
                                    <p style='margin: 5px 0 0 0; color: #0c5460; font-size: 14px;'>Use this ID for check-in at the mess counter. Keep it safe!</p>
                                </div>

                                <p style='color: #856404; background-color: #fff3cd; padding: 10px; border-radius: 5px; border-left: 4px solid #ffc107;'>
                                    <strong>⚠️ Important:</strong> For security reasons, please change your password after your first login.
                                </p>

                                <p>To login, visit: <a href='http://localhost:5242/Student/Login' style='color: #007bff;'>Student Login</a></p>

                                <hr style='margin-top: 20px; border: none; border-top: 1px solid #ddd;'>
                                <p style='color: #999; font-size: 12px;'>This is an automated email. Please do not reply.</p>
                                <p style='color: #999; font-size: 12px;'>&copy; {DateTime.Now.Year} Mess Management System. All rights reserved.</p>
                            </div>
                        </body>
                        </html>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Student credentials email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send credentials email to {toEmail}");
                throw new Exception($"Email sending failed: {ex.Message}", ex);
            }
        }
    }
}
