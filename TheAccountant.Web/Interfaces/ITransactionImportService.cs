using TheAccountant.Models;

namespace TheAccountant.Interfaces
{
    public interface ITransactionImportService
    {
        Task<IEnumerable<TransactionDto>> ImportAsync(Stream fileStream, string fileType);
    }
}
