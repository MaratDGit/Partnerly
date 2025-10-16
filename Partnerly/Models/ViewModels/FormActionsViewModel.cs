namespace Partnerly.Models.ViewModels
{
    public class FormActionsViewModel
    {
        public Guid? Id { get; set; }
        public bool ShowBack { get; set; } = true;
        public bool ShowUndo { get; set; } = true;
        public bool ShowSave { get; set; } = false;
        public bool ShowCreate { get; set; } = false;
        public bool ShowDelete { get; set; } = false;
        public bool ShowMenu { get; set; } = false;
        public List<MenuAction> MenuActions { get; set; } = new List<MenuAction>();
    }
    public class MenuAction
    {
        public string Icon { get; set; } = "bx bx-circle"; // по умолчанию
        public string Name { get; set; } = "";
        public string Controller { get; set; } = "";
        public bool IsModal { get; set; } = false;     // если true - открываем modal
        public string? ModalTarget { get; set; }       // id модалки, например "#previewModal"
        public string? OnClick { get; set; }           // JS функция, например "showPreview()"
        public string Action { get; set; } = "";
        public Guid? RouteID { get; set; } = null;
        public object? RouteValues { get; set; } = null; // для asp-route-* параметров
    }
}
