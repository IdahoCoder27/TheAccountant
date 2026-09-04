namespace TheAccountant.Web.Interfaces
{
    public interface ITransactionImportService
    {
        Task<Guid> CreatePreviewAsync(
            string userId,
            int accountId,
            string fileName,
            Stream fileStream);

        Task<int> ConfirmImportAsync(
            string userId,
            Guid batchId);
    }
}