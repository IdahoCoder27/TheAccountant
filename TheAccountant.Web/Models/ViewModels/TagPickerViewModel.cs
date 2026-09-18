namespace TheAccountant.Web.Models.ViewModels
{
    public class TagPickerViewModel
    {
        public string Id { get; init; } = "transactionTags";

        public string FieldName { get; init; } = "TagsText";

        public string? Value { get; init; }

        public IReadOnlyList<string> AvailableTags { get; init; }
            = Array.Empty<string>();
    }
}