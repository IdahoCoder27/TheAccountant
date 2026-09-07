using CsvHelper.Configuration;

namespace TheAccountant.Web.Services.Import
{
    internal sealed class ChaseCsvRecordMap
        : ClassMap<ChaseCsvRecord>
    {
        public ChaseCsvRecordMap()
        {
            Map(m => m.Details)
                .Name("Details");

            Map(m => m.PostingDate)
                .Name("Posting Date");

            Map(m => m.Description)
                .Name("Description");

            Map(m => m.Amount)
                .Name("Amount");

            Map(m => m.Type)
                .Name("Type");

            Map(m => m.Balance)
                .Name("Balance");

            Map(m => m.CheckOrSlipNumber)
                .Name("Check or Slip #");
        }
    }
}
