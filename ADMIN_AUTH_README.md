# Mess Management System - Admin Authentication Setup

## ✅ What Has Been Completed

All code for the Admin Authentication system has been successfully created:

### 📁 Files Created:

1. **Models/User.cs** - User model with Email, PasswordHash, IsFirstTimeLogin, OTP fields
2. **Data/ApplicationDbContext.cs** - Entity Framework database context
3. **Services/IEmailService.cs** - Email service interface
4. **Services/EmailService.cs** - SMTP email service implementation
5. **Controllers/AdminController.cs** - Admin authentication controller with:
   - Login logic (checks for tahirnawaz12194@gmail.com)
   - OTP generation and verification
   - Password setup for first-time login
6. **Views/Admin/Login.cshtml** - Bootstrap 5 login page
7. **Views/Admin/VerifyOTP.cshtml** - Bootstrap 5 OTP verification page
8. **Views/Admin/SetPassword.cshtml** - Bootstrap 5 password setup page

### ⚙️ Configuration Files Updated:

- **appsettings.json** - Added database connection string and email settings
- **appsettings.Development.json** - Added development settings
- **Program.cs** - Configured DbContext, Session, and Email service

### 📦 NuGet Packages Installed:

- Microsoft.EntityFrameworkCore.SqlServer (v8.0.0)
- Microsoft.EntityFrameworkCore.Tools (v8.0.0)

---

## 🚀 Next Steps to Complete Setup

### Step 1: Stop the Running Application

Your app is currently running. Stop it by:
- Pressing **Ctrl+C** in the terminal, or
- Clicking the **Stop** button in VS Code

### Step 2: Apply Database Migration

After stopping the app, run:
```powershell
dotnet ef database update
```

This will create the `MessManagementDB` database with the Users table.

### Step 3: Configure Gmail SMTP (Required for Email)

To send OTP emails, you need to set up Gmail App Password:

1. **Enable 2-Factor Authentication** on your Gmail account
2. **Generate App Password**:
   - Go to: https://myaccount.google.com/apppasswords
   - Select "Mail" and "Windows Computer"
   - Generate password (16 characters)
3. **Update appsettings.json**:

```json
"EmailSettings": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SenderEmail": "your-gmail@gmail.com",
  "SenderPassword": "your-16-char-app-password",
  "SenderName": "Mess Management System"
}
```

⚠️ **Important**: Replace `your-gmail@gmail.com` and `your-16-char-app-password` with your actual values.

### Step 4: Run the Application

```powershell
dotnet run
```

Or press **F5** in VS Code to debug.

---

## 🧪 Testing the Authentication Flow

### Test Scenario 1: First-Time Login

1. Navigate to: `https://localhost:xxxx/Admin/Login`
2. Enter email: `tahirnawaz12194@gmail.com`
3. Click **Login**
4. System will:
   - Generate 6-digit OTP
   - Send OTP to email
   - Redirect to OTP verification page
5. Enter the OTP from your email
6. Set a permanent password (minimum 6 characters)
7. You're now logged in!

### Test Scenario 2: Subsequent Logins

1. Navigate to: `https://localhost:xxxx/Admin/Login`
2. Enter email: `tahirnawaz12194@gmail.com`
3. Enter your password
4. Click **Login**
5. You're logged in!

### Test Scenario 3: Unauthorized Email

1. Try any email other than `tahirnawaz12194@gmail.com`
2. System will show: "Unauthorized email address."

---

## 🔍 Key Features Implemented

✅ **Fixed Admin Email**: Only `tahirnawaz12194@gmail.com` can access
✅ **First-Time OTP**: 6-digit OTP sent via email on first login
✅ **OTP Expiration**: 10-minute validity
✅ **Password Setup**: Permanent password after OTP verification
✅ **Session Management**: Admin session tracking
✅ **Bootstrap 5 UI**: Modern, responsive design
✅ **Password Hashing**: SHA256 for security
✅ **Validation**: Client-side and server-side validation

---

## 📋 Database Schema

**Users Table:**
- `Id` (int, Primary Key, Identity)
- `Email` (nvarchar(255), Unique, Not Null)
- `PasswordHash` (nvarchar(255), Nullable)
- `IsFirstTimeLogin` (bit, Not Null, Default: true)
- `CurrentOTP` (nvarchar(max), Nullable)
- `OTPGeneratedAt` (datetime2, Nullable)

---

## 🛠️ Troubleshooting

### Problem: Email not sending

**Solution:**
- Verify Gmail App Password is correct
- Check if 2FA is enabled on Gmail
- Ensure firewall isn't blocking port 587

### Problem: Database connection failed

**Solution:**
- Verify SQL Server LocalDB is installed
- Check connection string in appsettings.json
- Run: `sqllocaldb info` to check LocalDB status

### Problem: Migration errors

**Solution:**
- Delete the `Migrations` folder
- Run: `dotnet ef migrations add InitialCreate`
- Run: `dotnet ef database update`

---

## 📞 Support

For any issues or questions, contact the development team.

**Admin Email**: tahirnawaz12194@gmail.com

---

## 📝 Notes

- The default route is set to `Admin/Login` (not Home/Index)
- Session timeout is set to 30 minutes
- Password minimum length is 6 characters
- OTP is valid for 10 minutes
- Bootstrap 5 and Bootstrap Icons are already included

---

✨ **Your Admin Authentication System is Ready!** ✨

Just complete Steps 1-4 above to start using it.
