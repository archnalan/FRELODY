namespace FRELODYUI.Shared.Services
{
    public class HeroDataService
    {
        private string _searchQuery = string.Empty;
        private bool IsFromLanding;

        public event Func<string, Task>? OnSearchChanged;

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery != value)
                {
                    _searchQuery = value;
                    OnSearchChanged?.Invoke(_searchQuery);
                }
            }
        }

        public async Task UpdateSearchQuery(string query)
        {
            SearchQuery = query;
            if (OnSearchChanged != null)
            {
                await OnSearchChanged(query);
            }
        }

        public bool GetRenderOrigin() => IsFromLanding;

        public void SetRenderOrigin(bool isFromLanding)
        {
            IsFromLanding = isFromLanding;
        }
    }
}