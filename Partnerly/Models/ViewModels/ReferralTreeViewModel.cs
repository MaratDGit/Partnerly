namespace Partnerly.Models.ViewModels
{
    public class ReferralTreePageViewModel
    {
        public Guid ViewerUserId { get; set; }
        public Guid RootUserId { get; set; }
        public string RootDisplayName { get; set; } = string.Empty;
        public bool CanPickUser { get; set; }
        public ReferralTreeNodeViewModel? Tree { get; set; }
    }

    public class ReferralTreeNodeViewModel
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? ReferralCode { get; set; }
        public string PhotoUrl { get; set; } = "/assets/img/profiles/default-user.png";
        public string? RoleName { get; set; }
        public int DirectCount { get; set; }
        public int TotalCount { get; set; }
        public int Level { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? CreatedDate { get; set; }
        public List<ReferralTreeNodeViewModel> Children { get; set; } = new();
    }

    public class ReferralTreeQueryResult
    {
        public bool AccessDenied { get; set; }
        public ReferralTreeNodeViewModel? Tree { get; set; }
    }
}
