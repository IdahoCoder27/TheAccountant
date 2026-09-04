namespace TheAccountant.Models
{
    public class AppSettings
    {
        public int Id { get; set; }

        public bool ImportReminderEnabled { get; set; } = true;

        public int ImportReminderDays { get; set; } = 7;

        public DateTime? LastSuccessfulImportUtc { get; set; }

        public DateTime? LastReminderUtc { get; set; }

        public DateTime? SnoozedUntilUtc { get; set; }
    }
}
