namespace staybnb_csharp.Models
{
    public static class Roles
    {
        public const string Guest = "Guest";
        public const string Host = "Host";
        public const string Admin = "Admin";
        public const string SuperAdmin = "SuperAdmin";

        // For [Authorize(Roles = Roles.AdminOrSuperAdmin)] on admin dashboard routes.
        public const string AdminOrSuperAdmin = Admin + "," + SuperAdmin;
    }
}
