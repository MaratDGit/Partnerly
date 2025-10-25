namespace Partnerly.Models.ViewModels
{
    public class FormActionsViewModel
    {
        public ActionBack Back { get; set; }
        public ActionUndo Undo { get; set; }
        public ActionSave Save { get; set; }
        public ActionCreate Create { get; set; }
        public ActionDelete Delete { get; set; }
        public ActionMenu Menu { get; set; }
        public ActionNote Note { get; set; }
        public List<MenuItemAction> MenuActions { get; set; } = new List<MenuItemAction>();
    }
    public class FormActionBase
    {
        public Guid? Id { get; set; }
        public string? Icon { get; set; }
        public bool? Visible { get; set; }
        public string? DisplayName { get; set; }
        public string? Title { get; set; }
        public string? FromController { get; set; }
        public string? ToController { get; set; }
        public string? FromAction { get; set; }
        public string? ToAction { get; set; }
        public bool? IsModal { get; set; }
        public string? ModalTarget { get; set; }
        public string? OnClick { get; set; }
        public Guid? RouteID { get; set; }
        public object? RouteValues { get; set; }

    }
    public class ActionBack : FormActionBase
    {
        public ActionBack(Guid? id = null, bool? visible = null, string? fromController = null, string? toController = null, string? fromAction = null, string? toAction = null)
        {
            Id = id;
            Icon = "bx bx-arrow-back";
            Visible = visible ?? false;
            Title = "Назад";
            FromController = fromController; 
            ToController = toController;
            FromAction = fromAction;
            ToAction = toAction;
        }
    }
    public class ActionUndo : FormActionBase
    {
        public ActionUndo(Guid? id = null, bool? visible = null, string? fromController = null, string? toController = null, string? fromAction = null, string? toAction = null)
        {
            Id = id;
            Icon = "bx bx-undo";
            Visible = visible ?? false;
            Title = "Редактировать";
            FromController = fromController;
            ToController = toController;
            FromAction = fromAction;
            ToAction = toAction;
        }
    }
    public class ActionSave : FormActionBase
    {
        public ActionSave(Guid? id = null, bool? visible = null, string? fromController = null, string? toController = null, string? fromAction = null, string? toAction = null)
        {
            Id = id;
            Icon = "bx bx-save";
            Visible = visible ?? false;
            Title = "Сохранить";
            FromController = fromController;
            ToController = toController;
            FromAction = fromAction;
            ToAction = toAction;
        }
    }
    public class ActionCreate : FormActionBase
    {
        public ActionCreate(Guid? id = null, bool? visible = null, string? fromController = null, string? toController = null, string? fromAction = null, string? toAction = null)
        {
            Id = id;
            Icon = "bx bx-plus";
            Visible = visible ?? false;
            Title = "Создать";
            FromController = fromController;
            ToController = toController;
            FromAction = fromAction;
            ToAction = toAction;
        }
    }
    public class ActionDelete : FormActionBase
    {
        public ActionDelete(Guid? id = null, bool? visible = null, string? fromController = null, string? toController = null, string? fromAction = null, string? toAction = null)
        {
            Id = id;
            Icon = "bx bx-trash";
            Visible = visible ?? false;
            Title = "Удалить";
            FromController = fromController;
            ToController = toController;
            FromAction = fromAction;
            ToAction = toAction;
        }
    }

    public class ActionNote : FormActionBase
    {
        public string Note { get; set; }
        public ActionNote(string? note = null, Guid? id = null, bool visible = false)
        {
            Id = id;
            Note = note;
            Visible = visible;
        }
    }

    public class ActionMenu : FormActionBase
    {
        public ActionMenu(bool? visible = null)
        {
            Icon = "bx bx-dots-horizontal-rounded";
            Visible = visible ?? false;
            Title = "Дополнительно";
        }
    }

    public class MenuItemAction : FormActionBase
    {
        public MenuItemAction(Guid? id = null, string? icon = null, bool? visible = null, bool? isModal = null, 
            string? modalTarget = null, string? displayName = null, string? onClick = null, 
            string? fromController = null, string? toController = null, string? fromAction = null,
            string? toAction = null, Guid? routID = null, object? routeValues = null)
        {
            Id = id;
            Icon = icon;
            Visible = visible ?? false;
            IsModal = isModal;
            ModalTarget = modalTarget;
            DisplayName = displayName;
            OnClick = onClick;
            FromController = fromController;
            ToController = toController;
            FromAction = fromAction;
            ToAction = toAction;
            RouteID = routID;
            RouteValues = routeValues;
        }
    }
}
