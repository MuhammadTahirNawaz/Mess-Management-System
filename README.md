# Mess Management System - Complete Implementation

## Overview
A modern Mess Management System built with ASP.NET Core MVC featuring digital ID cards with QR codes, quick check-in system, menu management, and automated billing.

## System Features

### 1. Admin Authentication System
- **Fixed Admin Email**: tahirnawaz12194@gmail.com
- **OTP-based First Login**: 6-digit OTP sent via Gmail
- **Password Setup**: First-time users set their password after OTP verification
- **Secure Login**: Password-based login for subsequent access
- **Session Management**: Maintains admin session across requests

### 2. Student Registration System
- **Email Restriction**: Only @student.uet.edu.pk emails allowed
- **Auto-Generated Credentials**: System generates unique PermanentID and random password
- **Email Notification**: Students receive credentials via email
- **Self-Service Password Change**: Students can change their password after first login
- **Student Dashboard**: Access to digital ID card and features

### 3. Digital ID Card System ⭐ NEW
- **Unique PermanentID**: Format UET-YYYY-XXXX (e.g., UET-2025-1234)
- **QR Code Generation**: Each student gets a unique QR code based on PermanentID
- **Professional Card Design**: University-branded digital ID card
- **Current Balance Display**: Shows real-time unpaid balance on card
- **Print/Download**: Students can print their ID card
- **Quick Check-In**: QR code can be scanned or ID entered manually

### 4. Quick Check-In System ⭐ NEW
- **Instant Verification**: Enter or scan PermanentID to verify student
- **Real-Time Information**: Shows student details, current balance, today's meals
- **One-Click Check-In**: Select meal type (Breakfast/Lunch/Dinner) to check in
- **Automatic Pricing**: Gets price from today's menu automatically
- **Duplicate Prevention**: Cannot check in twice for same meal on same day
- **Fast Processing**: Optimized for high-traffic meal times

### 5. Menu Management
- **Daily Menu Organization**: Menu items organized by day of week
- **Meal Types**: Support for Breakfast, Lunch, and Dinner
- **CRUD Operations**: Add, edit, and delete menu items
- **Price Management**: Set prices for each menu item
- **Active/Inactive Status**: Toggle menu items

### 6. Billing System
- **Unpaid Bills View**: See all students with unpaid bills
- **Total Calculation**: Automatic calculation of total amount per student
- **Meal Count**: Track number of meals consumed
- **Detailed History**: View individual student's attendance and billing history
- **Payment Status**: Track paid/unpaid status for each attendance

## Application URLs

- **Base URL**: http://localhost:5242
- **Admin Login**: http://localhost:5242/Admin/Login
- **Admin Dashboard**: http://localhost:5242/Admin/Dashboard
- **Student Registration**: http://localhost:5242/Student/Register
- **Student Login**: http://localhost:5242/Student/Login
- **Digital ID Card**: http://localhost:5242/Student/DigitalCard
- **Menu Management**: http://localhost:5242/AdminMenu/MenuManagement
- **Student Check-In**: http://localhost:5242/AdminMenu/CheckIn
- **View Students**: http://localhost:5242/AdminMenu/ViewStudents
- **View Bills**: http://localhost:5242/AdminMenu/ViewBills

## Database Structure

### Tables Created:
1. **Users** - Admin users with OTP fields
2. **Students** - Student users with PermanentID and QR code
3. **MenuItems** - Menu items with day, meal type, and prices
4. **Attendances** - Attendance records with billing information

### Database: MessManagementDB (SQL Server LocalDB)

## Email Configuration

**SMTP Settings (Gmail)**:
- Sender: tahirnawaz12194@gmail.com
- App Password: qqyk usmp gyir rfwy
- SMTP Server: smtp.gmail.com
- Port: 587
- SSL: Enabled

## Admin Dashboard Features

### Quick Access Cards:
1. **Menu Management** - Manage daily menu items and prices
2. **Registered Students** - View all registered students
3. **Student Check-In** - Quick QR/ID-based check-in system
4. **Student Bills** - View unpaid bills and payment history

## Usage Flow

### Admin Workflow:
1. Login with tahirnawaz12194@gmail.com
2. Receive OTP via email (first time)
3. Verify OTP and set password
4. Access Admin Dashboard
5. Add menu items for each day/meal with prices
6. Students register and get digital ID cards
7. At meal time: Open Check-In page
8. Enter/Scan student's PermanentID
9. Verify student information
10. Click meal type to check in (price auto-added)
11. View bills and payment status

### Student Workflow:
1. Register with @student.uet.edu.pk email
2. Receive PermanentID and password via email
3. Login with credentials
4. Change password on first login
5. Access Digital ID Card from dashboard
6. View/Print/Download ID card with QR code
7. Present ID at mess for check-in

## Technical Stack

- **Framework**: ASP.NET Core MVC (.NET 8.0)
- **ORM**: Entity Framework Core 8.0.0
- **Database**: SQL Server LocalDB
- **Email**: SMTP via Gmail
- **QR Code**: QRCoder 1.7.0
- **UI Framework**: Bootstrap 5 with Bootstrap Icons
- **Authentication**: Session-based
- **Password Security**: SHA256 hashing

## Key Files

### Models:
- `Models/User.cs` - Admin user model
- `Models/Student.cs` - Student user model with PermanentID
- `Models/MenuItem.cs` - Menu item model
- `Models/Attendance.cs` - Attendance tracking model

### Controllers:
- `Controllers/AdminController.cs` - Admin authentication and dashboard
- `Controllers/StudentController.cs` - Student registration, login, and digital card
- `Controllers/AdminMenuController.cs` - Menu, check-in, and billing management

### Services:
- `Services/EmailService.cs` - Gmail SMTP email service
- `Services/IEmailService.cs` - Email service interface
- `Services/QRCodeService.cs` - QR code generation service
- `Services/IQRCodeService.cs` - QR code service interface

### Database:
- `Data/ApplicationDbContext.cs` - EF Core context

## Running the Application

```powershell
cd "d:\Mess management system\Semester Project"
dotnet run
```

Application will start on: http://localhost:5242

## Database Migrations

```powershell
# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations to database
dotnet ef database update
```

## Security Features

- **Email Verification**: OTP-based verification for admin
- **Password Hashing**: SHA256 for secure password storage
- **Session Management**: Secure session handling
- **Email Domain Restriction**: Only allowed domains can register
- **CSRF Protection**: AntiForgeryToken on all forms

## Bill Calculation Logic

1. Student registers → Gets unique PermanentID → Receives digital ID card with QR code
2. Admin adds menu items with prices for each day/meal type
3. Student comes to mess → Shows ID card or QR code
4. Admin enters/scans PermanentID in Check-In system
5. System verifies student and displays current balance
6. Admin selects meal type (Breakfast/Lunch/Dinner)
7. System looks up menu price for today's meal
8. Creates attendance record with price as amount
9. Updates student's balance in real-time
10. Bill view shows sum of all unpaid attendance records

## Status

✅ **Complete and Operational**

All features implemented and tested:
- Admin authentication with OTP ✅
- Student registration with auto-generated PermanentID ✅
- Digital ID Card with QR Code generation ✅
- Quick Check-In system (scan/enter ID) ✅
- Menu management (CRUD) ✅
- Automated attendance with pricing ✅
- Real-time balance display ✅
- Billing system with detailed views ✅
- Email notifications working ✅
- Database tables created and configured ✅
- UI complete with Bootstrap 5 ✅

## Key Improvements Over Original Design

### Before (Manual Attendance):
- Admin had to manually select multiple students
- Bulk attendance marking was slow
- No student identification system
- Manual process prone to errors

### After (Digital ID + Quick Check-In):
- Each student has unique PermanentID with QR code
- Professional digital ID card
- Instant check-in by scanning/entering ID
- One student at a time (faster at peak times)
- Real-time balance display
- Duplicate prevention automatic
- Much more practical for daily operations

## Next Steps (Optional Enhancements)

1. **QR Scanner Integration** - Use webcam/phone camera to scan QR codes
2. **Payment processing integration** - Online payment gateway
3. **Export bills to PDF/Excel** - Generate reports
4. **Email reminders for unpaid bills** - Automated notifications
5. **Menu planning for multiple weeks** - Advanced scheduling
6. **Student feedback system** - Rate meals and service
7. **Reports and analytics** - Usage statistics and trends
8. **Admin user management** - Multiple admins with roles
9. **SMS notifications** - Alternative to email
10. **Mobile app integration** - Native iOS/Android apps
11. **RFID card support** - Physical card scanning
12. **Biometric check-in** - Fingerprint/face recognition
13. **Advance meal booking** - Students can book meals ahead
14. **Special dietary requirements** - Track allergies and preferences
15. **Meal plans/packages** - Monthly subscription options
