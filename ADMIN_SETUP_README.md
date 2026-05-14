# Admin Authentication System - Setup Instructions

## Overview
This is the Admin Authentication System for the Mess Management System with OTP-based first-time login.

## Features Implemented
✅ Single Admin with fixed email: `tahirnawaz12194@gmail.com`
✅ Bootstrap 5 styled login page
✅ OTP generation and email sending on first-time login
✅ OTP verification page
✅ Password setup after OTP verification
✅ SQL Server database integration
✅ Session management

## Setup Instructions

### 1. Install Required NuGet Packages
Run the following commands in the Package Manager Console or Terminal:

```powershell
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.AspNetCore.Session
```

### 2. Configure Email Settings (IMPORTANT!)

#### Get Gmail App Password (Free):
1. Go to your Google Account: https://myaccount.google.com/
2. Click on "Security" in the left sidebar
3. Enable "2-Step Verification" if not already enabled
4. Search for "App passwords" in the search bar
5. Click on "App passwords"
6. Select "Mail" and "Windows Computer" (or Other)
7. Click "Generate"
8. Copy the 16-character password (without spaces)

#### Update appsettings.json:
Replace the placeholder values in `appsettings.json` and `appsettings.Development.json`:

```json
"EmailSettings": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SenderEmail": "your-actual-email@gmail.com",
  "SenderPassword": "your-16-char-app-password",
  "SenderName": "Mess Management System"
}
```

### 3. Update Database Connection String (if needed)
The default connection string uses LocalDB:
```
Server=(localdb)\\mssqllocaldb;Database=MessManagementDB;Trusted_Connection=True;MultipleActiveResultSets=true
```

For SQL Server Express, use:
```
Server=.\\SQLEXPRESS;Database=MessManagementDB;Trusted_Connection=True;MultipleActiveResultSets=true
```

### 4. Create Database Migration
Run these commands in the Package Manager Console:

```powershell
Add-Migration InitialCreate
Update-Database
```

Or using .NET CLI:

```powershell
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 5. Run the Application

```powershell
dotnet run
```

The application will start and redirect to the Admin Login page.

## How It Works

### First-Time Login Flow:
1. Admin enters email: `tahirnawaz12194@gmail.com`
2. System generates a 6-digit OTP
3. OTP is sent to the email via SMTP
4. Admin enters OTP on verification page
5. After verification, admin sets their permanent password
6. Admin can now login with email and password

### Subsequent Logins:
1. Admin enters email and password
2. System validates credentials
3. Admin is logged in

## Files Created

### Controllers:
- `Controllers/AdminController.cs` - Handles Login, OTP Verification, and Password Setup

### Models:
- `Models/User.cs` - User entity with Email, PasswordHash, IsFirstTimeLogin, CurrentOTP, OTPGeneratedAt

### Data:
- `Data/ApplicationDbContext.cs` - EF Core DbContext for database operations

### Services:
- `Services/IEmailService.cs` - Email service interface
- `Services/EmailService.cs` - SMTP email service implementation

### Views:
- `Views/Admin/Login.cshtml` - Bootstrap 5 login page
- `Views/Admin/VerifyOTP.cshtml` - OTP verification page
- `Views/Admin/SetPassword.cshtml` - Password setup page

### Configuration:
- `Program.cs` - Updated with DbContext, Session, and Email service registration
- `appsettings.json` - Connection string and email settings

## Database Table Structure

### Users Table:
| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary Key |
| Email | nvarchar(255) | Unique email address |
| PasswordHash | nvarchar(255) | SHA256 hashed password |
| IsFirstTimeLogin | bit | Flag for first-time login |
| CurrentOTP | nvarchar(max) | Current OTP (cleared after verification) |
| OTPGeneratedAt | datetime2 | OTP generation timestamp |

## Security Features
- ✅ Anti-forgery tokens on all forms
- ✅ Password hashing using SHA256
- ✅ OTP expires after 10 minutes
- ✅ Session management
- ✅ Only authorized admin email allowed

## Testing the System

1. **First Login:**
   - Navigate to `/Admin/Login`
   - Enter: `tahirnawaz12194@gmail.com`
   - Check email for OTP
   - Enter OTP on verification page
   - Set a password (minimum 6 characters)

2. **Subsequent Login:**
   - Navigate to `/Admin/Login`
   - Enter email and password
   - Should login successfully

## Troubleshooting

### Email not sending?
- ✅ Check if you're using Gmail App Password (not regular password)
- ✅ Ensure 2-Step Verification is enabled
- ✅ Check spam folder
- ✅ Verify SMTP settings in appsettings.json

### Database errors?
- ✅ Run migrations: `dotnet ef database update`
- ✅ Check connection string
- ✅ Ensure SQL Server is running

### OTP expired?
- ✅ Go back to login and enter email again to get new OTP
- ✅ OTP is valid for 10 minutes only

## Next Steps
After authentication is working:
- [ ] Add authorization middleware to protect other pages
- [ ] Create admin dashboard
- [ ] Add other admin functionalities
- [ ] Implement password reset feature
- [ ] Add more security features (rate limiting, etc.)

## Notes
- The admin email `tahirnawaz12194@gmail.com` is hardcoded in `AdminController.cs`
- To change the admin email, modify the `ADMIN_EMAIL` constant
- Bootstrap 5 is already included in the default template
- Session timeout is set to 30 minutes
