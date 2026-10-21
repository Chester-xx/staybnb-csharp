namespace staybnb_csharp.Models.Enums
{
    public enum ApplicationStatus { Pending, Approved, Rejected }
    public enum BookingStatus { Pending, Approved, Rejected, CheckedIn, Cancelled, Completed }
    public enum PaymentStatus { Pending, Paid, Failed, Refunded }
    public enum PropertyType { Apartment, House, Cabin, Villa, Cottage }
    public enum CheckInStatus { InProgress, Submitted, Verified }
    public enum DocumentStatus { Pending, Verified }

    public enum ActivityType
    {
        Login,
        Registration,
        RoleChange,
        HostApplication,
        BookingStatusChange,
        CheckIn,
        Property
    }

    public enum NotificationType { HostApplication, Booking, CheckIn, Message }
}
