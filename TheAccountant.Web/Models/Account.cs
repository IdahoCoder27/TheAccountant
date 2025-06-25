namespace TheAccountant.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string UserId { get; set; }  // FK to IdentityUser
        public string Institution { get; set; } // e.g., Chase, Amex
        public string AccountName { get; set; } // e.g., "Checking", "Platinum Card"
        public decimal Balance { get; set; }
        public DateTime LastUpdated { get; set; }

        public List<Transaction> Transactions { get; set; }
    }

}
