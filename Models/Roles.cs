namespace staybnb_csharp.Models
{
    // Role names
    public static class Roles
    {
        public const string Guest = "Guest";
        public const string Host = "Host";
        public const string Admin = "Admin";
        public const string SuperAdmin = "SuperAdmin";

        public const string AdminOrSuperAdmin = Admin + "," + SuperAdmin;
    }
}
