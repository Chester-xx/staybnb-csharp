# StayBNB

An Airbnb-inspired property rental marketplace built with ASP.NET Core MVC, EF Core, TypeScript, Tailwind and SQL Server.
Guests browse and book stays, Hosts list properties, and Admins moderate the platform.

## Implementation

| Area | Technology |
|---|---|
| Web framework | ASP.NET Core MVC (.NET 10) |
| Data | Entity Framework Core 10, SQL Server (LocalDB by default) |
| Authentication | ASP.NET Core Identity with roles |
| Front end | Tailwind CSS v4 and TypeScript, built with npm |

## Dependencies

- **Visual Studio 26**
- **.NET 10 SDK** and **Visual Studio** with the *ASP.NET and web development* workload. That workload includes
  **SQL Server Express LocalDB**, which the project uses by default.
- **Node.js** (optional). The compiled CSS and JavaScript are already in `wwwroot`, so the app runs without it.
  If Node is installed, the project rebuilds them automatically on every build.

## Quick start

1. Open `staybnb-csharp.slnx` in Visual Studio and press **F5** (or run `dotnet run` in the project folder).
2. On first start the app automatically creates the database (EF Core migrations) and seeds the four roles
   and the **SuperAdmin** account.
3. *(Optional)* load the sample data: see **Sample data** below.

## Database

The connection string is `DefaultConnection` in `appsettings.json`. By default it points to LocalDB:

```
Server=(localdb)\mssqllocaldb;Database=aspnet-staybnb_csharp-...;Trusted_Connection=True;MultipleActiveResultSets=true
```

To use a full SQL Server or SQL Server Express instance instead, change the `Server=` part, for example
`Server=.\SQLEXPRESS;Database=StayBNB;Trusted_Connection=True;TrustServerCertificate=True;`. The database is created
on first run.

### Create the database from SQL (optional)

`Database/schema.sql` is the EF Core migration script (every table and constraint). Use it if you would rather not
let the app create the database:

1. Create an empty database, for example `CREATE DATABASE StayBNB;`, and select it.
2. Run `Database/schema.sql`.
3. Point `DefaultConnection` in `appsettings.json` at that database.
4. Run `Database/database.sql` for the sample data (see below).

The script also records the migration, so the app will not try to create the tables again.

### Sample data

`Database/database.sql` inserts test users, properties with images, bookings, check-ins, messages, notifications
and activity logs.

1. Start the app once so the tables exist.
2. Open a query window **on the StayBNB database** (not `master`). In Visual Studio: *View > SQL Server Object
   Explorer*, expand `(localdb)\MSSQLLocalDB > Databases`, right-click the database, then *New Query*.
   Or run: `sqlcmd -S "(localdb)\mssqllocaldb" -d <database name> -i Database\database.sql`
3. Run the script.

The script uses fixed IDs, so it needs a **fresh database**. If the tables already contain data, or the admin test
account already exists, it does nothing. To start fresh, run `Drop-Database` in the Package Manager Console, then
start the app again before running the script.

## Test accounts

| Role | Email | Password |
|---|---|---|
| SuperAdmin | superadmin@staybnb.com | `SuperAdmin123!` |
| Admin | admin@staybnb.com | `Admin123!` |
| Host | host@staybnb.com | `Host123!` |
| Host | host2@staybnb.com | `Host123!` |
| Guest | guest@staybnb.com | `Guest123!` |
| Guest | guest2@staybnb.com | `Guest123!` |

The SuperAdmin is created by the app on startup. The other accounts come from `database.sql`. Anyone can also
register, and new accounts start as a Guest.

## Roles and main pages

| Role | What they can do | Main pages |
|---|---|---|
| Guest | Browse and filter listings, book, check in, apply to become a Host, message | `/`, `/Bookings`, `/HostApplication/Apply`, `/HostApplication/Status` |
| Host | Manage properties and the IsActive toggle, add or remove properties, set up check-in, approve or reject bookings, verify check-ins | `/Host/Dashboard`, `/Host/Properties`, `/Host/BookingRequests`, `/Host/CheckIns` |
| Admin | Review host applications, view the activity log | `/Admin/Dashboard`, `/Admin/HostRequests`, `/Admin/ActivityLogs` |
| SuperAdmin | Everything an Admin can do, plus promote Guests to Admin and change Hosts or Admins back to Guests | `/Admin/Users` |

Every role also has Messages, Notifications (with the bell icon) and Account settings (name and profile picture,
password). Routes are protected with `[Authorize(Roles = ...)]`, so a Host cannot open Guest-only pages and the
reverse.

## Configuration

Platform fees are percentages of the nightly subtotal, set in `appsettings.json`:

```json
"Pricing": { "CleaningFeePercent": 5, "ServiceFeePercent": 15 }
```

## Uploaded files

| Folder | Contents | Access |
|---|---|---|
| `wwwroot/uploads/properties` | Property photos | Public |
| `PrivateUploads/documents` | Guest check-in documents | Only the guest and that property's Host |
| `PrivateUploads/profile-pictures` | Profile pictures and `default.svg` (fallback) | Signed-in users |

Files are stored under a generated name (upload timestamp plus a short random suffix). Images are JPG, PNG or WebP
up to 5 MB, and documents may also be PDF. Keep both folders with the project, since the sample data refers to the
files in them.

## Frontend

The source is in `Frontend` (`css/app.css` and `ts/site.ts`). To rebuild manually run `npm install` once, then
`npm run build`. Use `npm run watch:css` while styling. The colours are defined in one place, the
`@theme` block of `Frontend/css/app.css`.

## Project structure

```
staybnb-csharp
├── Areas
│   ├── Admin                          Admin dashboard (controllers and views)
│   ├── Host                           Host dashboard (controllers and views)
│   └── Identity                       Login, registration and account settings
│     
├── Controllers                        Guest-facing controllers (home, properties, bookings, check-in, messages, notifications)
│
├── Data                               DbContext and the role and SuperAdmin seeder
│
├── Database
│   ├── schema.sql                     Schema script (EF Core migration)
│   └── database.sql                   Sample data script
│
├── Frontend                           Tailwind CSS and TypeScript sources
├── Migrations                         EF Core migrations
├── Models                             Entities and enums
├── Services                           Activity log, notifications, pricing, file storage, host applications      
├── ViewComponents                     Header icons
├── ViewModels                         View models
│
├── Program.cs                         Application startup, services and middleware pipeline
│
├── appsettings.json                   Connection string, pricing and other configuration
├── appsettings.Development.json
├── package.json                       npm dependencies and the front end build scripts
├── package-lock.json
├── tsconfig.json                      TypeScript compiler options
│
├── staybnb-csharp.csproj              Project file
├── staybnb-csharp.slnx                Solution file
│
└── LICENSE                            MIT license
```


## Design notes

- A user is either a Guest or a Host, never both. Approving a host application swaps the roles in one transaction.
- Approval does not list a property by itself. The Host must first complete the check-in setup, and saving it for the
  first time lists the property.
- Approving a booking leaves the property listed. Overlapping dates are blocked, and the property page shows the
  dates that are already booked.
- Guests and Hosts can start conversations only with Admins or the SuperAdmin, except that a Guest can message a
  Host from that Host's property page. Admins and the SuperAdmin can message anyone, and anyone can reply.
- A property with any booking history cannot be deleted, only deactivated.
- The `Payment`, `Review`, `Amenity` and `WishlistItem` tables exist in the database design but have no features
  in the app.

<br>

# License

![MIT License](https://github.com/Chester-xx/staybnb-csharp/blob/main/LICENSE)
