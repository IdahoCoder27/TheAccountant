using TheAccountant.Web.Models.DTOs;

namespace TheAccountant.Web.Interfaces
{
    public interface ITransactionFileParser
    {
        string InstitutionName { get; }

        bool CanParse(string fileName);

        Task<IReadOnlyList<TransactionImportRowDto>> ParseAsync(
            Stream fileStream);
    }
}