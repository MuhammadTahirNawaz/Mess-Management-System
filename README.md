# 🎓 UET Mess Management System
**A Comprehensive Digital Dining & Identity Solution for University Hostels**

[![Live Demo](https://img.shields.io/badge/Live-Demo-brightgreen?style=for-the-badge&logo=googlechrome&logoColor=white)](https://uetmessmanagementsystem.runasp.net)
[![Framework](https://img.shields.io/badge/ASP.NET%20Core%20MVC-8.0-blue?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Database](https://img.shields.io/badge/SQL%20Server-2022-red?style=for-the-badge&logo=microsoftsqlserver)](https://www.microsoft.com/en-us/sql-server/)

## 📝 Project Overview
The **UET Mess Management System** is a modern web application designed to digitize hostel dining operations. It eliminates manual record-keeping by implementing **Digital ID Cards**, **QR Code Scanning**, and **Real-Time Billing**. The system is specifically tailored for UET Lahore requirements, featuring domain-restricted registration and professional academic ID card designs.

---

## 🚀 Key Modules & Features

### 1. Advanced Identity System ⭐
* **Unique PermanentID**: Automated generation in university standard format (`UET-YYYY-XXXX`).
* **Dynamic QR Codes**: Each student receives a unique QR code generated from their UID.
* **Digital ID Card**: A professional, branded digital card displaying student info, photos, and current unpaid balance.

### 2. High-Speed Check-In System
* **Dual Entry Mode**: Support for instant QR scanning or manual ID entry.
* **Real-Time Data**: Admins see student status, balance, and photo immediately upon scan.
* **Intelligent Pricing**: System automatically fetches the current day's price from the active menu.
* **Validation**: Built-in duplicate prevention (cannot check in for the same meal twice).

### 3. Menu & Dining Management
* **Daily Scheduling**: Manage Breakfast, Lunch, and Dinner menus for each day of the week.
* **Dynamic Pricing**: Update meal prices in real-time which reflects instantly at the check-in counter.
* **Status Control**: Toggle visibility and availability of menu items.

### 4. Billing & Financial Ledger
* **Unpaid Bills View**: Global view of all outstanding dues across the student body.
* **Attendance Tracking**: Granular logs of every meal consumed, including date, time, and cost.
* **Automated Summation**: Real-time calculation of total dues without manual intervention.

---

## 🔐 Security & Verification
* **Domain Restriction**: Registration is strictly limited to `@student.uet.edu.pk` email addresses.
* **MFA Admin Login**: 6-digit OTP verification via SMTP (Gmail) for admin account security.
* **Credential Privacy**: All passwords are stored using high-entropy SHA256 hashing.
* **Session Guard**: Secure session management to prevent unauthorized administrative access.

---

## 🛠 Technical Stack
* **Backend**: C# / ASP.NET Core MVC 8.0
* **Frontend**: Razor Pages, Bootstrap 5, Bootstrap Icons
* **ORM**: Entity Framework Core 8.0
* **Database**: Microsoft SQL Server
* **Third-Party Libraries**: 
    * `QRCoder` (QR Code generation)
    * `Newtonsoft.Json` (Data serialization)
    * `EF Core Tools` (Database migrations)

---

## 📂 Project Structure

| Directory | Description |
| :--- | :--- |
| **`Controllers/`** | Logic for Admin, Student, Menu, and Face Recognition modules. |
| **`Models/`** | Database schemas for Users, Students, MenuItems, and Attendance. |
| **`Views/`** | Razor templates for dashboards, billing, and the Digital ID system. |
| **`Services/`** | Implementation of Email (SMTP) and QR Code generation logic. |
| **`wwwroot/`** | Static assets including custom CSS, JS, and University logos. |

---
