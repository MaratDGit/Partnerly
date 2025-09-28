namespace Partnerly.Models.ViewModels
{
    public class DashboardsViewModel
    {
        public string? UserPhotoUrl { get; set; }
        public string? UserFirstName { get; set; }
        public string? UserLastName { get; set; }
        public string? UserFullName { get { return $"{UserFirstName} {UserLastName}"; } }

        public string? UserRefCode { get; set; }
        public int? UserRefCount { get; set; }
    }
}
