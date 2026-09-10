namespace TheAccountant.Interfaces
{
    public interface ICategoryService
    {
        Task<string> CategorizeAsync(
            string userId,
            string? description);
    }
}
