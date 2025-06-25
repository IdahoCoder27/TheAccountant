namespace TheAccountant.Models
{
    public class RecurringPayment
    {
        public int Id { get; set; }
        public string UserId { get; set; }  // FK to IdentityUser
        public string Name { get; set; } // e.g., "Netflix"
        public decimal Amount { get; set; }
        public string Frequency { get; set; } // e.g., "Monthly"
        public DateTime NextDue { get; set; }
    }

}
