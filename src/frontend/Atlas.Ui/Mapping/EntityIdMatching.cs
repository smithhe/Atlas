namespace Atlas.Ui.Mapping
{
    public static class EntityIdMatching
    {
        public const string All = "All";
        public const string None = "";

        public static bool MatchesIdFilter(Guid? entityId, string filterValue)
        {
            if (filterValue == All)
            {
                return true;
            }

            if (filterValue == None)
            {
                return entityId is null;
            }

            return Guid.TryParse(filterValue, out Guid id) && entityId == id;
        }

        public static int CompareLinkedDisplay(
            Guid? firstId,
            Guid? secondId,
            string? firstDisplay,
            string? secondDisplay,
            int direction)
        {
            int byName = string.Compare(firstDisplay ?? "", secondDisplay ?? "", StringComparison.Ordinal);
            if (byName != 0)
            {
                return byName * direction;
            }

            return Comparer<Guid?>.Default.Compare(firstId, secondId) * direction;
        }

        public static string? DisplayNameById<T>(
            Guid? id,
            IEnumerable<T> items,
            Func<T, Guid> idSelector,
            Func<T, string> nameSelector)
            where T : class
        {
            if (id is not Guid value)
            {
                return null;
            }

            T? match = items.FirstOrDefault(item => idSelector(item) == value);
            return match is null ? null : nameSelector(match);
        }
    }
}
