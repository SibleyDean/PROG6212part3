-- Make sure we are in master so we can drop the database if it exists
USE master;
GO

-- If the DB exists, force single-user (terminate connections) and drop it
IF DB_ID('ClaimManagement') IS NOT NULL
BEGIN
    ALTER DATABASE ClaimManagement SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ClaimManagement;
END
GO

-- Create fresh database
CREATE DATABASE ClaimManagement;
GO

-- Switch to the new database
USE ClaimManagement;
GO

-- If tables exist (e.g. script run multiple times), drop Claims first (FK) then Users
IF OBJECT_ID('dbo.Claims', 'U') IS NOT NULL
    DROP TABLE dbo.Claims;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
    DROP TABLE dbo.Users;
GO

-- Create Users table
CREATE TABLE dbo.Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Surname NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    Password NVARCHAR(255) NOT NULL,
    HourlyRate DECIMAL(10,2) NULL,
    Role NVARCHAR(50) NOT NULL CHECK (Role IN ('HR', 'Lecturer', 'ProgrammeCoordinator', 'AcademicManager')),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

-- Create Claims table with ALL required columns
CREATE TABLE dbo.Claims (
    ClaimId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Title NVARCHAR(255) NOT NULL DEFAULT 'Untitled Claim',
    Description NVARCHAR(MAX) NULL,
    HoursWorked DECIMAL(5,2) NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Submitted' CHECK (Status IN ('Submitted', 'ApprovedByProgrammeCoordinator', 'ApprovedByAcademicManager', 'Rejected', 'Paid')),
    SubmissionDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    Documentation NVARCHAR(MAX) NULL,
    OriginalFileName NVARCHAR(500) NULL, -- NEW COLUMN for file uploads
    ApprovedByProgrammeCoordinator BIT NULL,
    ApprovedByAcademicManager BIT NULL,
    RejectionReason NVARCHAR(500) NULL,
    CONSTRAINT FK_Claims_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);
GO

-- Seed Users
INSERT INTO dbo.Users (Name, Surname, Email, Password, Role)
VALUES ('HR', 'Admin', 'hr@university.com', 'HR123', 'HR');

INSERT INTO dbo.Users (Name, Surname, Email, Password, HourlyRate, Role)
VALUES ('John', 'Lecturer', 'lecturer@university.com', 'Lecturer123', 150.00, 'Lecturer');

INSERT INTO dbo.Users (Name, Surname, Email, Password, Role)
VALUES ('Jane', 'Coordinator', 'coordinator@university.com', 'Coordinator123', 'ProgrammeCoordinator');

INSERT INTO dbo.Users (Name, Surname, Email, Password, Role)
VALUES ('Bob', 'Manager', 'manager@university.com', 'Manager123', 'AcademicManager');
GO

-- Insert sample claims with ALL required fields
INSERT INTO dbo.Claims (UserId, Title, Description, HoursWorked, Amount, Documentation, Status)
VALUES 
(2, 'Computer Science 101 - October Lectures', 'Monthly lecture delivery and student consultation for introductory programming course', 40, 6000.00, 'Lecture preparation and delivery for Computer Science 101', 'Submitted'),
(2, 'Advanced Programming Workshop - November', 'Advanced programming concepts workshop for final year computer science students', 25, 3750.00, 'Conducted advanced programming workshop for final year students covering design patterns and best practices.', 'Submitted'),
(2, 'Research Paper Supervision - December', 'Research guidance and paper review for postgraduate computer science students', 15, 2250.00, 'Supervised 3 research papers for postgraduate students in computer science department.', 'ApprovedByProgrammeCoordinator');
GO

-- Verify the claims
SELECT 
    c.ClaimId,
    u.Name + ' ' + u.Surname as Lecturer,
    c.Title,
    c.Description,
    c.HoursWorked,
    c.Amount,
    c.Status,
    c.OriginalFileName,
    c.SubmissionDate
FROM Claims c
INNER JOIN Users u ON c.UserId = u.UserId
ORDER BY c.ClaimId;
GO