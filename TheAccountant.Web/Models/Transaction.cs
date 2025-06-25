namespace TheAccountant.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public Account Account { get; set; }

        public DateTime Date { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; } // + = income, - = expense
        public string Category { get; set; } // optional
        public bool IsRecurring { get; set; }
    }
}
