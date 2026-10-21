/*
    StayBNB - seed data for testing (plain INSERT statements with fixed IDs)
    ---------------------------------------------------------------------------------
    Run this on a FRESH database: the schema must exist (start the app once, or run
    Update-Database, or run Database/schema.sql), the StayBNB database must be selected
    (NOT master), and the data tables must still be empty because the IDs are fixed.

    Test accounts (password in brackets):
        superadmin@staybnb.com  SuperAdmin   (SuperAdmin123!)
        admin@staybnb.com       Admin        (Admin123!)
        host@staybnb.com        Host         (Host123!)   - Ballito Lodge, Cozy Forest Cabin
        host2@staybnb.com       Host         (Host123!)   - Sea Point Apartment, Karoo Cottage
        guest@staybnb.com       Guest        (Guest123!)  - bookings, a rejected host application
        guest2@staybnb.com      Guest        (Guest123!)  - a pending host application

    Password hashes are ASP.NET Core Identity (V3, PBKDF2-HMAC-SHA512) hashes.
    Sample property images: wwwroot/uploads/properties (seed01-seed10.svg).
    Sample check-in documents: PrivateUploads/documents. Keep both folders with the project.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
BEGIN
    DECLARE @CurrentDb nvarchar(128) = DB_NAME();
    RAISERROR(N'The StayBNB tables were not found in database "%s". Select the StayBNB database (the name after Database= in appsettings.json) and make sure the app has been started once so the schema exists, then run this script again.', 16, 1, @CurrentDb);
    RETURN;
END;

IF EXISTS (SELECT 1 FROM dbo.AspNetUsers WHERE NormalizedEmail = N'ADMIN@STAYBNB.COM')
   OR EXISTS (SELECT 1 FROM dbo.HostProperties)
BEGIN
    PRINT 'Seed data is already present, or the data tables are not empty - nothing was changed.';
    RETURN;
END;

BEGIN TRANSACTION;

---------------------------------------------------------------------------------
-- Roles (the app also creates these on startup; only added here if missing)
---------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE NormalizedName = N'GUEST')
    INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (N'role-guest', N'Guest', N'GUEST', N'role-guest-stamp');
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE NormalizedName = N'HOST')
    INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (N'role-host', N'Host', N'HOST', N'role-host-stamp');
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE NormalizedName = N'ADMIN')
    INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (N'role-admin', N'Admin', N'ADMIN', N'role-admin-stamp');
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE NormalizedName = N'SUPERADMIN')
    INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (N'role-superadmin', N'SuperAdmin', N'SUPERADMIN', N'role-superadmin-stamp');

---------------------------------------------------------------------------------
-- Users
---------------------------------------------------------------------------------
-- The SuperAdmin is normally created by the app on startup; only added if it does not exist yet.
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers WHERE NormalizedEmail = N'SUPERADMIN@STAYBNB.COM')
BEGIN
    INSERT INTO dbo.AspNetUsers
        (Id, FirstName, LastName, ProfilePictureUrl, CreatedAt, UserName, NormalizedUserName, Email, NormalizedEmail,
         EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
         TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount)
    VALUES
        (N'user-superadmin', N'Super', N'Admin', NULL, '2026-08-01T08:00:00', N'superadmin@staybnb.com', N'SUPERADMIN@STAYBNB.COM',
         N'superadmin@staybnb.com', N'SUPERADMIN@STAYBNB.COM', 1,
         N'AQAAAAIAAYagAAAAELroa8aJDi6q9sVSCvRe/A5WHo0H9bS8zlj/FygmHCSrgz0kcBicBklsOl/ydCte4w==',
         N'seed-stamp-superadmin', N'seed-concurrency-superadmin', NULL, 0, 0, NULL, 1, 0);

    INSERT INTO dbo.AspNetUserRoles (UserId, RoleId)
    SELECT N'user-superadmin', Id FROM dbo.AspNetRoles WHERE NormalizedName = N'SUPERADMIN';
END;

INSERT INTO dbo.AspNetUsers
    (Id, FirstName, LastName, ProfilePictureUrl, CreatedAt, UserName, NormalizedUserName, Email, NormalizedEmail,
     EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
     TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount)
VALUES
    (N'user-admin', N'Anele', N'Dube', NULL, '2026-08-01T09:00:00', N'admin@staybnb.com', N'ADMIN@STAYBNB.COM',
     N'admin@staybnb.com', N'ADMIN@STAYBNB.COM', 1,
     N'AQAAAAIAAYagAAAAEC34fOLC5yJ5NamOlM4keagnuiwdW19oMmQ4EE8ZtWAw/Ap/8eqqCXh2ll5V7y7sfQ==',
     N'seed-stamp-admin', N'seed-concurrency-admin', NULL, 0, 0, NULL, 1, 0),

    (N'user-host1', N'Thabo', N'Mokoena', NULL, '2026-08-15T10:00:00', N'host@staybnb.com', N'HOST@STAYBNB.COM',
     N'host@staybnb.com', N'HOST@STAYBNB.COM', 1,
     N'AQAAAAIAAYagAAAAEF5xZs05QsvlbRXEIIyy8KFyrW1RFo3PujBJnueGLkXVW6bUQVEMV9hJwDb7d2jHNg==',
     N'seed-stamp-host1', N'seed-concurrency-host1', NULL, 0, 0, NULL, 1, 0),

    (N'user-host2', N'Lerato', N'Dlamini', NULL, '2026-08-30T10:00:00', N'host2@staybnb.com', N'HOST2@STAYBNB.COM',
     N'host2@staybnb.com', N'HOST2@STAYBNB.COM', 1,
     N'AQAAAAIAAYagAAAAEF5xZs05QsvlbRXEIIyy8KFyrW1RFo3PujBJnueGLkXVW6bUQVEMV9hJwDb7d2jHNg==',
     N'seed-stamp-host2', N'seed-concurrency-host2', NULL, 0, 0, NULL, 1, 0),

    (N'user-guest1', N'Sipho', N'Khumalo', NULL, '2026-09-20T10:00:00', N'guest@staybnb.com', N'GUEST@STAYBNB.COM',
     N'guest@staybnb.com', N'GUEST@STAYBNB.COM', 1,
     N'AQAAAAIAAYagAAAAEHhhZgtmluHGJro+l3KbcX5UOtUsIaz3h5jYz4aAbeReUtv7AIVie+yW4NjJfOUJ7g==',
     N'seed-stamp-guest1', N'seed-concurrency-guest1', NULL, 0, 0, NULL, 1, 0),

    (N'user-guest2', N'Aisha', N'Patel', NULL, '2026-09-25T10:00:00', N'guest2@staybnb.com', N'GUEST2@STAYBNB.COM',
     N'guest2@staybnb.com', N'GUEST2@STAYBNB.COM', 1,
     N'AQAAAAIAAYagAAAAEHhhZgtmluHGJro+l3KbcX5UOtUsIaz3h5jYz4aAbeReUtv7AIVie+yW4NjJfOUJ7g==',
     N'seed-stamp-guest2', N'seed-concurrency-guest2', NULL, 0, 0, NULL, 1, 0);

INSERT INTO dbo.AspNetUserRoles (UserId, RoleId) SELECT N'user-admin',  Id FROM dbo.AspNetRoles WHERE NormalizedName = N'ADMIN';
INSERT INTO dbo.AspNetUserRoles (UserId, RoleId) SELECT N'user-host1',  Id FROM dbo.AspNetRoles WHERE NormalizedName = N'HOST';
INSERT INTO dbo.AspNetUserRoles (UserId, RoleId) SELECT N'user-host2',  Id FROM dbo.AspNetRoles WHERE NormalizedName = N'HOST';
INSERT INTO dbo.AspNetUserRoles (UserId, RoleId) SELECT N'user-guest1', Id FROM dbo.AspNetRoles WHERE NormalizedName = N'GUEST';
INSERT INTO dbo.AspNetUserRoles (UserId, RoleId) SELECT N'user-guest2', Id FROM dbo.AspNetRoles WHERE NormalizedName = N'GUEST';

---------------------------------------------------------------------------------
-- Amenities (database design only - no amenity features in the app)
---------------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.Amenities ON;
INSERT INTO dbo.Amenities (Id, Name, IconClass) VALUES
    (1, N'Wi-Fi',            N'wifi'),
    (2, N'Kitchen',          N'kitchen'),
    (3, N'Washer',           N'washer'),
    (4, N'Air conditioning', N'ac'),
    (5, N'Heating',          N'heating');
SET IDENTITY_INSERT dbo.Amenities OFF;

---------------------------------------------------------------------------------
-- Properties (PropertyType: 0 Apartment, 1 House, 2 Cabin, 3 Villa, 4 Cottage)
-- Properties 5 and 6 belong to guests whose host applications are still pending / rejected.
---------------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.HostProperties ON;
INSERT INTO dbo.HostProperties (Id, Title, Description, PricePerNight, HostId, Address, City, PropertyType, IsActive) VALUES
    (1, N'Ballito Lodge',
        N'A stylish four-bedroom lodge a short walk from the Ballito beaches. Open-plan living, a large entertainment deck with a built-in braai and a sparkling pool make it perfect for family holidays and weekends away with friends.',
        2100.00, N'user-host1', N'12 Dolphin Coast Road, Ballito', N'Ballito', 3, 1),
    (2, N'Cozy Forest Cabin',
        N'A warm, rustic cabin tucked away among the trees in the Champagne Valley. Enjoy a log fireplace, a private deck with mountain views and quiet evenings under the stars.',
        1230.00, N'user-host1', N'Forest Trail Road, Champagne Valley', N'Drakensberg', 2, 1),
    (3, N'Sea Point Apartment',
        N'A bright one-bedroom apartment a few minutes from the Sea Point promenade. Floor-to-ceiling windows, a fully equipped kitchen, fast Wi-Fi and secure parking.',
        1850.00, N'user-host2', N'48 Beach Road, Sea Point', N'Cape Town', 0, 1),
    (4, N'Karoo Cottage',
        N'A charming restored cottage in the heart of Prince Albert, with thick white walls that keep cool in summer and a lovely shaded courtyard. Walk to restaurants, galleries and the Saturday market.',
        950.00, N'user-host2', N'7 Church Street, Prince Albert', N'Prince Albert', 4, 1),
    (5, N'Stellenbosch Garden Cottage',
        N'A peaceful garden cottage surrounded by vineyards, with a private patio, a cosy lounge and easy access to the Stellenbosch wine routes.',
        1400.00, N'user-guest2', N'21 Vineyard Lane, Stellenbosch', N'Stellenbosch', 4, 0),
    (6, N'Hillcrest Studio',
        N'A compact, modern studio close to Hatfield and the Gautrain station, ideal for business travellers and students.',
        780.00, N'user-guest1', N'5 Jacaranda Avenue, Hatfield', N'Pretoria', 0, 0);
SET IDENTITY_INSERT dbo.HostProperties OFF;

SET IDENTITY_INSERT dbo.PropertyImages ON;
INSERT INTO dbo.PropertyImages (Id, PropertyId, ImageUrl) VALUES
    (1,  1, N'20260901T000000000_seed01.svg'),
    (2,  1, N'20260901T000000000_seed02.svg'),
    (3,  2, N'20260901T000000000_seed03.svg'),
    (4,  2, N'20260901T000000000_seed04.svg'),
    (5,  3, N'20260901T000000000_seed05.svg'),
    (6,  3, N'20260901T000000000_seed06.svg'),
    (7,  4, N'20260901T000000000_seed07.svg'),
    (8,  4, N'20260901T000000000_seed08.svg'),
    (9,  5, N'20260901T000000000_seed09.svg'),
    (10, 6, N'20260901T000000000_seed10.svg');
SET IDENTITY_INSERT dbo.PropertyImages OFF;

-- Amenity links (AmenitiesId, PropertiesId)
INSERT INTO dbo.AmenityHostProperty (AmenitiesId, PropertiesId) VALUES
    (1, 1), (1, 2), (1, 3), (1, 4),
    (2, 1), (2, 3), (2, 4),
    (3, 1), (3, 3),
    (4, 1), (4, 3),
    (5, 2);

---------------------------------------------------------------------------------
-- Host applications (Status: 0 Pending, 1 Approved, 2 Rejected)
---------------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.HostApplications ON;
INSERT INTO dbo.HostApplications (Id, ApplicationUserId, PropertyId, Status, AppliedAt, RejectionReason) VALUES
    (1, N'user-host1',  1, 1, '2026-08-20T10:00:00', NULL),
    (2, N'user-host1',  2, 1, '2026-08-28T11:00:00', NULL),
    (3, N'user-host2',  3, 1, '2026-09-02T09:30:00', NULL),
    (4, N'user-host2',  4, 1, '2026-09-10T14:00:00', NULL),
    (5, N'user-guest2', 5, 0, '2026-10-18T15:20:00', NULL),
    (6, N'user-guest1', 6, 2, '2026-10-05T12:00:00', N'Please add clearer photos of the whole property and resubmit.');
SET IDENTITY_INSERT dbo.HostApplications OFF;

---------------------------------------------------------------------------------
-- Check-in setup (rules and required documents stored as JSON)
---------------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.CheckInProcesses ON;
INSERT INTO dbo.CheckInProcesses (Id, PropertyId, Title, StepsJson, CreatedAt) VALUES
    (1, 1, N'Check-in for Ballito Lodge',
        N'{"rules":["No smoking indoors","Quiet hours from 22:00 to 06:00","No more than 8 guests at any time"],"requiredDocuments":["ID document"]}',
        '2026-08-22T09:00:00'),
    (2, 2, N'Check-in for Cozy Forest Cabin',
        N'{"rules":["No open fires outside the fire pit","Please keep noise low after 21:00"],"requiredDocuments":["ID document"]}',
        '2026-08-29T09:00:00'),
    (3, 3, N'Check-in for Sea Point Apartment',
        N'{"rules":["No parties or events","Check-out by 10:00"],"requiredDocuments":["ID document"]}',
        '2026-09-03T09:00:00'),
    (4, 4, N'Check-in for Karoo Cottage',
        N'{"rules":["Quiet after 20:00 out of respect for the neighbours","Please close all gates behind you"],"requiredDocuments":["ID document"]}',
        '2026-09-11T09:00:00');
SET IDENTITY_INSERT dbo.CheckInProcesses OFF;

---------------------------------------------------------------------------------
-- Bookings (Status: 0 Pending, 1 Approved, 2 Rejected, 3 CheckedIn)
-- TotalPrice = nights x price + 5% cleaning fee + 15% service fee
---------------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.Bookings ON;
INSERT INTO dbo.Bookings (Id, PropertyId, GuestId, CheckInDate, CheckOutDate, NumberOfGuests, TotalPrice, Status, CreatedAt) VALUES
    (1, 1, N'user-guest1', '2026-11-20', '2026-11-23', 4, 7560.00, 0, '2026-10-15T09:30:00'),
    (2, 2, N'user-guest1', '2026-12-05', '2026-12-08', 2, 4428.00, 1, '2026-10-12T14:10:00'),
    (3, 3, N'user-guest2', '2026-10-18', '2026-10-22', 2, 8880.00, 3, '2026-10-05T11:00:00'),
    (4, 4, N'user-guest1', '2026-11-02', '2026-11-05', 2, 3420.00, 1, '2026-10-10T08:45:00'),
    (5, 1, N'user-guest2', '2026-12-20', '2026-12-22', 3, 5040.00, 2, '2026-10-14T16:20:00');
SET IDENTITY_INSERT dbo.Bookings OFF;

---------------------------------------------------------------------------------
-- Guest check-ins (Status: 0 InProgress, 1 Submitted, 2 Verified) and documents (0 Pending, 1 Verified)
---------------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.GuestCheckIns ON;
INSERT INTO dbo.GuestCheckIns (Id, BookingId, CheckInProcessId, Status, CreatedAt) VALUES
    (1, 3, 3, 2, '2026-10-18T09:00:00'),
    (2, 4, 4, 1, '2026-10-19T10:15:00');
SET IDENTITY_INSERT dbo.GuestCheckIns OFF;

SET IDENTITY_INSERT dbo.GuestDocuments ON;
INSERT INTO dbo.GuestDocuments (Id, GuestCheckInId, DocumentType, FileName, Status) VALUES
    (1, 1, N'ID document', N'20261018T090000000_seeddoc1.pdf', 1),
    (2, 2, N'ID document', N'20261030T101500000_seeddoc2.pdf', 0);
SET IDENTITY_INSERT dbo.GuestDocuments OFF;

---------------------------------------------------------------------------------
-- Messages
---------------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.Messages ON;
INSERT INTO dbo.Messages (Id, SenderId, ReceiverId, Content, Timestamp, IsRead) VALUES
    (1, N'user-guest1', N'user-admin',  N'Hi, my host application for Hillcrest Studio was rejected. Can you tell me what photos are needed?', '2026-10-06T09:00:00', 1),
    (2, N'user-admin',  N'user-guest1', N'Hi Sipho, thanks for reaching out. Please upload clear photos of every room and the exterior, then resubmit your application.', '2026-10-06T11:30:00', 0),
    (3, N'user-guest1', N'user-host1',  N'Hi Thabo, is the deck at Ballito Lodge suitable for a braai with family?', '2026-10-15T09:35:00', 1),
    (4, N'user-host1',  N'user-guest1', N'Hi Sipho, yes - the deck has a built-in braai and plenty of seating. See you in November!', '2026-10-15T12:00:00', 0);
SET IDENTITY_INSERT dbo.Messages OFF;

---------------------------------------------------------------------------------
-- Notifications (Type: 0 HostApplication, 1 Booking, 2 CheckIn, 3 Message)
-- The SuperAdmin id is looked up, because the app may have created that account with its own id.
---------------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.Notifications ON;
INSERT INTO dbo.Notifications (Id, UserId, Title, Type, CreatedAt, IsRead) VALUES
    (1,  N'user-host1',  N'Host application approved: Ballito Lodge',     0, '2026-08-21T08:30:00', 1),
    (2,  N'user-host1',  N'Host application approved: Cozy Forest Cabin', 0, '2026-08-29T08:30:00', 1),
    (3,  N'user-host1',  N'New booking request: Ballito Lodge',           1, '2026-10-15T09:30:00', 0),
    (4,  N'user-host1',  N'New message from Sipho Khumalo',               3, '2026-10-15T09:35:00', 1),
    (5,  N'user-host2',  N'Check-in submitted: Karoo Cottage',            2, '2026-10-19T10:15:00', 0),
    (6,  N'user-host2',  N'New booking request: Karoo Cottage',           1, '2026-10-10T08:45:00', 1),
    (7,  N'user-guest1', N'Booking approved: Cozy Forest Cabin',          1, '2026-10-13T09:00:00', 0),
    (8,  N'user-guest1', N'Booking approved: Karoo Cottage',              1, '2026-10-11T09:00:00', 1),
    (9,  N'user-guest1', N'Booking requested: Ballito Lodge',             1, '2026-10-15T09:30:00', 1),
    (10, N'user-guest1', N'Host application rejected: Hillcrest Studio',  0, '2026-10-06T08:00:00', 1),
    (11, N'user-guest1', N'New message from Anele Dube',                  3, '2026-10-06T11:30:00', 0),
    (12, N'user-guest1', N'New message from Thabo Mokoena',               3, '2026-10-15T12:00:00', 0),
    (13, N'user-guest2', N'Check-in verified: Sea Point Apartment',       2, '2026-10-18T16:00:00', 0),
    (14, N'user-guest2', N'Booking rejected: Ballito Lodge',              1, '2026-10-15T10:00:00', 1),
    (15, N'user-guest2', N'Host application submitted',                   0, '2026-10-18T15:20:00', 1),
    (16, N'user-admin',  N'New host application from Aisha Patel',        0, '2026-10-18T15:20:00', 0),
    (17, (SELECT Id FROM dbo.AspNetUsers WHERE NormalizedEmail = N'SUPERADMIN@STAYBNB.COM'),
         N'New host application from Aisha Patel',                         0, '2026-10-18T15:20:00', 0);
SET IDENTITY_INSERT dbo.Notifications OFF;

---------------------------------------------------------------------------------
-- Activity log (ActivityType: 0 Login, 1 Registration, 2 RoleChange, 3 HostApplication,
--               4 BookingStatusChange, 5 CheckIn, 6 Property)
---------------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.ActivityLogs ON;
INSERT INTO dbo.ActivityLogs (Id, UserId, Action, ActivityType, CreatedAt) VALUES
    (1,  N'user-host1',  N'User registered as Guest',                                    1, '2026-08-15T10:00:00'),
    (2,  N'user-host1',  N'Submitted host application: Ballito Lodge',                   3, '2026-08-20T10:00:00'),
    (3,  N'user-admin',  N'Approved application: Ballito Lodge',                         3, '2026-08-21T08:30:00'),
    (4,  N'user-admin',  N'Role changed Guest to Host: host@staybnb.com',                2, '2026-08-21T08:30:00'),
    (5,  N'user-host1',  N'Set up check-in: Ballito Lodge',                              6, '2026-08-22T09:00:00'),
    (6,  N'user-host1',  N'Submitted additional property application: Cozy Forest Cabin', 3, '2026-08-28T11:00:00'),
    (7,  N'user-admin',  N'Approved application: Cozy Forest Cabin',                     3, '2026-08-29T08:30:00'),
    (8,  N'user-guest1', N'User registered as Guest',                                    1, '2026-09-20T10:00:00'),
    (9,  N'user-guest2', N'User registered as Guest',                                    1, '2026-09-25T10:00:00'),
    (10, N'user-guest1', N'Submitted host application: Hillcrest Studio',                3, '2026-10-05T12:00:00'),
    (11, N'user-admin',  N'Rejected application: Hillcrest Studio',                      3, '2026-10-06T08:00:00'),
    (12, N'user-guest2', N'Booking requested (Pending): Sea Point Apartment',            4, '2026-10-05T11:00:00'),
    (13, N'user-host2',  N'Booking Pending to Approved: Karoo Cottage',                  4, '2026-10-11T09:00:00'),
    (14, N'user-host1',  N'Booking Pending to Approved: Cozy Forest Cabin',              4, '2026-10-13T09:00:00'),
    (15, N'user-guest1', N'Booking requested (Pending): Ballito Lodge',                  4, '2026-10-15T09:30:00'),
    (16, N'user-host1',  N'Booking Pending to Rejected: Ballito Lodge',                  4, '2026-10-15T10:00:00'),
    (17, N'user-guest2', N'Started check-in for booking at Sea Point Apartment',         5, '2026-10-18T09:00:00'),
    (18, N'user-host2',  N'Verified check-in documents for Sea Point Apartment',         5, '2026-10-18T16:00:00'),
    (19, N'user-host2',  N'Booking Approved to CheckedIn: Sea Point Apartment',          4, '2026-10-18T16:00:00'),
    (20, N'user-guest1', N'Submitted check-in for booking at Karoo Cottage',             5, '2026-10-19T10:15:00'),
    (21, N'user-guest2', N'Submitted host application: Stellenbosch Garden Cottage',     3, '2026-10-18T15:20:00'),
    (22, N'user-admin',  N'User logged in',                                              0, '2026-10-19T08:00:00'),
    (23, N'user-host1',  N'User logged in',                                              0, '2026-10-19T09:00:00'),
    (24, N'user-guest1', N'User logged in',                                              0, '2026-10-19T10:00:00');
SET IDENTITY_INSERT dbo.ActivityLogs OFF;

COMMIT TRANSACTION;

PRINT 'StayBNB seed data inserted.';
PRINT 'Accounts: superadmin@ / admin@ / host@ / host2@ / guest@ / guest2@ staybnb.com';
