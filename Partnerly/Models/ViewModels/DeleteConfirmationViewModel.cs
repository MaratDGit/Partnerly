namespace Partnerly.Models.ViewModels
{
    public class DeleteConfirmationViewModel
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Message { get; set; }

        public string DeleteAction { get; set; } = "DeleteConfirmed";

        public string? DeleteController { get; set; }

        public string CancelAction { get; set; } = "Index";

        public string? CancelController { get; set; }

        public string Icon { get; set; } = "bx bx-trash";
    }
}