# Database Design Document

Generated from database `ExamNest` on `DESKTOP-674VGSF\\SQLEXPRESS01`.

Excluded tables: `__EFMigrationsHistory`, `sysdiagrams`.

| Table Name | Column Name | Data Type | Constraint | Description |
|---|---|---|---|---|
| `dbo.CourseMedias` | `CourseMediaId` | `int` | PK; IDENTITY; NOT NULL | Primary key for the CourseMedias table. |
| `dbo.CourseMedias` | `FileName` | `nvarchar(max)` | NOT NULL | Uploaded file name. |
| `dbo.CourseMedias` | `FilePath` | `nvarchar(max)` | NOT NULL | Stored file path. |
| `dbo.CourseMedias` | `FileType` | `nvarchar(max)` | NOT NULL | Uploaded file content type/format. |
| `dbo.CourseMedias` | `CourseId` | `int` | NOT NULL; FK FK_CourseMedias_Courses_CourseId -> dbo.Courses(CourseId); ON DELETE CASCADE; ON UPDATE NO_ACTION | References related record in dbo.Courses(CourseId). |
| `dbo.Courses` | `CourseId` | `int` | PK; IDENTITY; NOT NULL | Primary key for the Courses table. |
| `dbo.Courses` | `Title` | `nvarchar(max)` | NOT NULL | Title or display name. |
| `dbo.Courses` | `Description` | `nvarchar(max)` | NOT NULL | Detailed description text. |
| `dbo.Courses` | `StartDate` | `datetime2(7)` | NOT NULL | Start date/time. |
| `dbo.Courses` | `EndDate` | `datetime2(7)` | NOT NULL | End date/time. |
| `dbo.Courses` | `IsPublished` | `bit` | NOT NULL | Publication status flag. |
| `dbo.Courses` | `Fees` | `real` | NOT NULL | Course fee amount. |
| `dbo.Courses` | `ThumbailUrl` | `nvarchar(max)` | NOT NULL | Thumbnail URL/path for the course. |
| `dbo.Courses` | `TeacherId` | `int` | NOT NULL; FK FK_Courses_users_TeacherId -> dbo.users(user_id); ON DELETE CASCADE; ON UPDATE NO_ACTION | References related record in dbo.users(user_id). |
| `dbo.Courses` | `CreatedAt` | `datetime2(7)` | NOT NULL | Record creation timestamp. |
| `dbo.email_otp` | `email_otp_id` | `int` | PK; IDENTITY; NOT NULL | Primary key for the email_otp table. |
| `dbo.email_otp` | `user_id` | `int` | NOT NULL; FK FK_email_otp_users_user_id -> dbo.users(user_id); ON DELETE CASCADE; ON UPDATE NO_ACTION | References related record in dbo.users(user_id). |
| `dbo.email_otp` | `otp_hash` | `nvarchar(255)` | NOT NULL | Hashed one-time password. |
| `dbo.email_otp` | `expires_at` | `datetime2(7)` | NOT NULL | Expiration timestamp. |
| `dbo.email_otp` | `is_used` | `bit` | NOT NULL; DEFAULT (CONVERT([bit],(0))) | Indicates whether OTP is already used. |
| `dbo.email_otp` | `created_at` | `datetime2(7)` | NOT NULL; DEFAULT (getdate()) | Record creation timestamp. |
| `dbo.ExamAttemptAnswers` | `ExamAttemptAnswerId` | `int` | PK; IDENTITY; NOT NULL | Primary key for the ExamAttemptAnswers table. |
| `dbo.ExamAttemptAnswers` | `ExamAttemptId` | `int` | NOT NULL; FK FK_ExamAttemptAnswers_ExamAttempts_ExamAttemptId -> dbo.ExamAttempts(ExamAttemptId); ON DELETE CASCADE; ON UPDATE NO_ACTION; UNIQUE IX_ExamAttemptAnswers_ExamAttemptId_ExamQuestionId (composite: ExamAttemptId, ExamQuestionId) | References related record in dbo.ExamAttempts(ExamAttemptId). |
| `dbo.ExamAttemptAnswers` | `ExamQuestionId` | `int` | NOT NULL; FK FK_ExamAttemptAnswers_ExamQuestions_ExamQuestionId -> dbo.ExamQuestions(ExamQuestionId); ON DELETE NO_ACTION; ON UPDATE NO_ACTION; UNIQUE IX_ExamAttemptAnswers_ExamAttemptId_ExamQuestionId (composite: ExamAttemptId, ExamQuestionId) | References related record in dbo.ExamQuestions(ExamQuestionId). |
| `dbo.ExamAttemptAnswers` | `SelectedOption` | `nvarchar(max)` | NOT NULL | Option selected by student. |
| `dbo.ExamAttemptAnswers` | `IsCorrect` | `bit` | NOT NULL | Whether selected answer is correct. |
| `dbo.ExamAttemptAnswers` | `MarksAwarded` | `int` | NOT NULL | Marks awarded for the answer. |
| `dbo.ExamAttemptAnswers` | `SubmittedAt` | `datetime2(7)` | NOT NULL | Submission timestamp. |
| `dbo.ExamAttempts` | `ExamAttemptId` | `int` | PK; IDENTITY; NOT NULL | Primary key for the ExamAttempts table. |
| `dbo.ExamAttempts` | `ExamId` | `int` | NOT NULL; FK FK_ExamAttempts_Exams_ExamId -> dbo.Exams(ExamId); ON DELETE CASCADE; ON UPDATE NO_ACTION | References related record in dbo.Exams(ExamId). |
| `dbo.ExamAttempts` | `StudentId` | `int` | NOT NULL; FK FK_ExamAttempts_users_StudentId -> dbo.users(user_id); ON DELETE NO_ACTION; ON UPDATE NO_ACTION | References related record in dbo.users(user_id). |
| `dbo.ExamAttempts` | `StartedAt` | `datetime2(7)` | NOT NULL | Stores s ta rt ed at. |
| `dbo.ExamAttempts` | `ExpiresAt` | `datetime2(7)` | NOT NULL | Stores e xp ir es at. |
| `dbo.ExamAttempts` | `SubmittedAt` | `datetime2(7)` | NULL | Submission timestamp. |
| `dbo.ExamAttempts` | `Status` | `nvarchar(max)` | NOT NULL | Current status value. |
| `dbo.ExamAttempts` | `TotalScore` | `int` | NOT NULL | Total score obtained in attempt. |
| `dbo.ExamAttempts` | `MaxScore` | `int` | NOT NULL | Maximum possible score in attempt. |
| `dbo.ExamAttempts` | `ViolationCount` | `int` | NOT NULL | Count of recorded exam violations. |
| `dbo.ExamAttempts` | `IsFlagged` | `bit` | NOT NULL | Indicates attempt is flagged for review. |
| `dbo.ExamAttempts` | `ClientSignature` | `nvarchar(max)` | NOT NULL | Client/browser signature used for integrity checks. |
| `dbo.ExamAttempts` | `QuestionOrderCsv` | `nvarchar(max)` | NOT NULL | Question order snapshot (CSV). |
| `dbo.ExamAttempts` | `OptionOrderJson` | `nvarchar(max)` | NOT NULL | Option order snapshot (JSON). |
| `dbo.ExamQuestions` | `ExamQuestionId` | `int` | PK; IDENTITY; NOT NULL | Primary key for the ExamQuestions table. |
| `dbo.ExamQuestions` | `ExamId` | `int` | NOT NULL; FK FK_ExamQuestions_Exams_ExamId -> dbo.Exams(ExamId); ON DELETE CASCADE; ON UPDATE NO_ACTION | References related record in dbo.Exams(ExamId). |
| `dbo.ExamQuestions` | `QuestionText` | `nvarchar(max)` | NOT NULL | Question statement text. |
| `dbo.ExamQuestions` | `OptionA` | `nvarchar(max)` | NOT NULL | Answer option A. |
| `dbo.ExamQuestions` | `OptionB` | `nvarchar(max)` | NOT NULL | Answer option B. |
| `dbo.ExamQuestions` | `OptionC` | `nvarchar(max)` | NOT NULL | Answer option C. |
| `dbo.ExamQuestions` | `OptionD` | `nvarchar(max)` | NOT NULL | Answer option D. |
| `dbo.ExamQuestions` | `CorrectOption` | `nvarchar(max)` | NOT NULL | Correct option key. |
| `dbo.ExamQuestions` | `Marks` | `int` | NOT NULL | Marks assigned to question. |
| `dbo.Exams` | `ExamId` | `int` | PK; IDENTITY; NOT NULL | Primary key for the Exams table. |
| `dbo.Exams` | `CourseId` | `int` | NOT NULL; FK FK_Exams_Courses_CourseId -> dbo.Courses(CourseId); ON DELETE CASCADE; ON UPDATE NO_ACTION | References related record in dbo.Courses(CourseId). |
| `dbo.Exams` | `TeacherId` | `int` | NOT NULL; FK FK_Exams_users_TeacherId -> dbo.users(user_id); ON DELETE NO_ACTION; ON UPDATE NO_ACTION | References related record in dbo.users(user_id). |
| `dbo.Exams` | `Title` | `nvarchar(max)` | NOT NULL | Title or display name. |
| `dbo.Exams` | `Description` | `nvarchar(max)` | NULL | Detailed description text. |
| `dbo.Exams` | `StartAt` | `datetime2(7)` | NOT NULL | Scheduled start date/time. |
| `dbo.Exams` | `EndAt` | `datetime2(7)` | NOT NULL | Scheduled end date/time. |
| `dbo.Exams` | `DurationMinutes` | `int` | NOT NULL | Exam duration in minutes. |
| `dbo.Exams` | `CreatedAt` | `datetime2(7)` | NOT NULL | Record creation timestamp. |
| `dbo.Exams` | `RandomQuestionCount` | `int` | NOT NULL; DEFAULT ((0)) | Number of random questions to serve. |
| `dbo.ExamViolationEvents` | `ExamViolationEventId` | `int` | PK; IDENTITY; NOT NULL | Primary key for the ExamViolationEvents table. |
| `dbo.ExamViolationEvents` | `ExamAttemptId` | `int` | NOT NULL; FK FK_ExamViolationEvents_ExamAttempts_ExamAttemptId -> dbo.ExamAttempts(ExamAttemptId); ON DELETE CASCADE; ON UPDATE NO_ACTION | References related record in dbo.ExamAttempts(ExamAttemptId). |
| `dbo.ExamViolationEvents` | `EventType` | `nvarchar(max)` | NOT NULL | Type/category of violation event. |
| `dbo.ExamViolationEvents` | `Details` | `nvarchar(max)` | NULL | Additional event details. |
| `dbo.ExamViolationEvents` | `CreatedAt` | `datetime2(7)` | NOT NULL | Record creation timestamp. |
| `dbo.Orders` | `Id` | `int` | PK; IDENTITY; NOT NULL | Primary key for the Orders table. |
| `dbo.Orders` | `OrderId` | `nvarchar(max)` | NOT NULL | Business order identifier (or related order id). |
| `dbo.Orders` | `StudentId` | `int` | NOT NULL; FK FK_Orders_users_StudentId -> dbo.users(user_id); ON DELETE NO_ACTION; ON UPDATE NO_ACTION | References related record in dbo.users(user_id). |
| `dbo.Orders` | `CourseId` | `int` | NOT NULL; FK FK_Orders_Courses_CourseId -> dbo.Courses(CourseId); ON DELETE NO_ACTION; ON UPDATE NO_ACTION | References related record in dbo.Courses(CourseId). |
| `dbo.Orders` | `Amount` | `decimal(18,2)` | NOT NULL | Monetary amount. |
| `dbo.Orders` | `Status` | `nvarchar(max)` | NOT NULL | Current status value. |
| `dbo.Orders` | `CreatedAt` | `datetime2(7)` | NOT NULL | Record creation timestamp. |
| `dbo.Payments` | `Id` | `int` | PK; IDENTITY; NOT NULL | Primary key for the Payments table. |
| `dbo.Payments` | `RazorpayPaymentId` | `nvarchar(max)` | NOT NULL | Razorpay payment transaction ID. |
| `dbo.Payments` | `RazorpayOrderId` | `nvarchar(max)` | NOT NULL | Razorpay order ID. |
| `dbo.Payments` | `Signature` | `nvarchar(max)` | NOT NULL | Payment signature/hash from gateway. |
| `dbo.Payments` | `Amount` | `decimal(18,2)` | NOT NULL | Monetary amount. |
| `dbo.Payments` | `Status` | `nvarchar(max)` | NOT NULL | Current status value. |
| `dbo.Payments` | `OrderId` | `int` | NOT NULL; FK FK_Payments_Orders_OrderId -> dbo.Orders(Id); ON DELETE NO_ACTION; ON UPDATE NO_ACTION | References related record in dbo.Orders(Id). |
| `dbo.Payments` | `CreatedAt` | `datetime2(7)` | NOT NULL | Record creation timestamp. |
| `dbo.roles` | `role_id` | `int` | PK; IDENTITY; NOT NULL | Primary key for the roles table. |
| `dbo.roles` | `role_name` | `nvarchar(50)` | NOT NULL; UNIQUE IX_roles_role_name | Role name label. |
| `dbo.roles` | `created_at` | `datetime2(7)` | NOT NULL; DEFAULT (getdate()) | Record creation timestamp. |
| `dbo.Subscriptions` | `Id` | `int` | PK; IDENTITY; NOT NULL | Primary key for the Subscriptions table. |
| `dbo.Subscriptions` | `StudentId` | `int` | NOT NULL; FK FK_Subscriptions_users_StudentId -> dbo.users(user_id); ON DELETE NO_ACTION; ON UPDATE NO_ACTION; UNIQUE IX_Subscriptions_StudentId_CourseId (composite: StudentId, CourseId) | References related record in dbo.users(user_id). |
| `dbo.Subscriptions` | `CourseId` | `int` | NOT NULL; FK FK_Subscriptions_Courses_CourseId -> dbo.Courses(CourseId); ON DELETE NO_ACTION; ON UPDATE NO_ACTION; UNIQUE IX_Subscriptions_StudentId_CourseId (composite: StudentId, CourseId) | References related record in dbo.Courses(CourseId). |
| `dbo.Subscriptions` | `Status` | `nvarchar(max)` | NOT NULL | Current status value. |
| `dbo.Subscriptions` | `CreatedAt` | `datetime2(7)` | NOT NULL | Record creation timestamp. |
| `dbo.user_google_auth` | `google_auth_id` | `int` | PK; IDENTITY; NOT NULL | Primary key for the user_google_auth table. |
| `dbo.user_google_auth` | `user_id` | `int` | NOT NULL; FK FK_user_google_auth_users_user_id -> dbo.users(user_id); ON DELETE CASCADE; ON UPDATE NO_ACTION | References related record in dbo.users(user_id). |
| `dbo.user_google_auth` | `google_sub` | `nvarchar(100)` | NOT NULL; UNIQUE IX_user_google_auth_google_sub | Google subject identifier (sub claim). |
| `dbo.user_google_auth` | `google_email` | `nvarchar(150)` | NOT NULL; UNIQUE IX_user_google_auth_google_email | Google account email. |
| `dbo.user_google_auth` | `created_at` | `datetime2(7)` | NOT NULL; DEFAULT (getdate()) | Record creation timestamp. |
| `dbo.user_google_auth` | `updated_at` | `datetime2(7)` | NULL | Record last update timestamp. |
| `dbo.users` | `user_id` | `int` | PK; IDENTITY; NOT NULL | Primary key for the users table. |
| `dbo.users` | `first_name` | `nvarchar(100)` | NOT NULL | User first name. |
| `dbo.users` | `middle_name` | `nvarchar(100)` | NULL | User middle name. |
| `dbo.users` | `last_name` | `nvarchar(100)` | NOT NULL | User last name. |
| `dbo.users` | `email` | `nvarchar(150)` | NOT NULL | Email address. |
| `dbo.users` | `username` | `nvarchar(100)` | NOT NULL; UNIQUE IX_users_username | Unique username for login/display. |
| `dbo.users` | `password_hash` | `nvarchar(255)` | NULL | Hashed password value. |
| `dbo.users` | `phone` | `nvarchar(20)` | NULL | Phone/mobile number. |
| `dbo.users` | `role_id` | `int` | NOT NULL; FK FK_users_roles_role_id -> dbo.roles(role_id); ON DELETE CASCADE; ON UPDATE NO_ACTION | References related record in dbo.roles(role_id). |
| `dbo.users` | `is_active` | `bit` | NOT NULL; DEFAULT (CONVERT([bit],(0))) | Indicates whether the user is active. |
| `dbo.users` | `last_login_at` | `datetime2(7)` | NULL | Last successful login timestamp. |
| `dbo.users` | `failed_login_attempts` | `int` | NOT NULL; DEFAULT ((0)) | Count of failed login attempts. |
| `dbo.users` | `created_at` | `datetime2(7)` | NOT NULL; DEFAULT (getdate()) | Record creation timestamp. |
| `dbo.users` | `updated_at` | `datetime2(7)` | NULL; DEFAULT (getdate()) | Record last update timestamp. |
| `dbo.users` | `profile_image_url` | `nvarchar(500)` | NULL | Stored profile image URL/path. |
