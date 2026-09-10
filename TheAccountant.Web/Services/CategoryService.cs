using Microsoft.EntityFrameworkCore;
using TheAccountant.Interfaces;
using TheAccountant.Web.Data;
using TheAccountant.Web.Interfaces;
using TheAccountant.Web.Models;

namespace TheAccountant.Web.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        private string? _loadedUserId;

        private List<CategoryRule>? _rules;

        public CategoryService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> CategorizeAsync(
            string userId,
            string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return "Uncategorized";
            }

            await EnsureRulesLoadedAsync(userId);

            var normalizedDescription =
                Normalize(description);

            var matchingRule = _rules!
                .Where(r =>
                    normalizedDescription.Contains(
                        Normalize(r.Pattern),
                        StringComparison.Ordinal))
                .OrderBy(r => r.Priority)
                .ThenByDescending(r => r.Pattern.Length)
                .FirstOrDefault();

            if (matchingRule is not null)
            {
                return matchingRule.Category;
            }

            //
            // Fall back to our existing built-in categorizer.
            //
            return TransactionCategorizer.Categorize(
                description);
        }

        private async Task EnsureRulesLoadedAsync(
            string userId)
        {
            if (_rules is not null &&
                _loadedUserId == userId)
            {
                return;
            }

            _rules = await _context.CategoryRules
                .AsNoTracking()
                .Where(r =>
                    r.UserId == userId &&
                    r.IsActive)
                .OrderBy(r => r.Priority)
                .ThenByDescending(r => r.Pattern.Length)
                .ToListAsync();

            _loadedUserId = userId;
        }

        private static string Normalize(
            string value)
        {
            return string.Join(
                    " ",
                    value.Split(
                        (char[]?)null,
                        StringSplitOptions.RemoveEmptyEntries))
                .Trim()
                .ToUpperInvariant();
        }
    }
}
