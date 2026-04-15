namespace FRELODYSHRD.Dtos.AuthDtos
{
    public class UsernameSuggestionsRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? TenantId { get; set; }
        public string? ExcludeUserId { get; set; }
    }

    public class UsernameSuggestionsResponseDto
    {
        public List<string> Suggestions { get; set; } = new();
    }

    public class UsernameAvailabilityRequestDto
    {
        public string Username { get; set; } = string.Empty;
        public string? TenantId { get; set; }
        public string? ExcludeUserId { get; set; }
    }

    public class UsernameAvailabilityResponseDto
    {
        public bool IsAvailable { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Message { get; set; }
    }
}
