using Microsoft.EntityFrameworkCore.Metadata;
using Partnerly.Descriptors.Messages;

namespace Partnerly.Models.ViewModels
{
    public class FormActionsViewModel
    {
        public ActionBack Back { get; set; } = new();
        public ActionUndo Undo { get; set; } = new();
        public ActionSave Save { get; set; } = new();
        public ActionCreate Create { get; set; } = new();
        public ActionDelete Delete { get; set; } = new();
        public ActionMenu Menu { get; set; } = new();
        public ActionNote Note { get; set; } = new();
        public ActionAttachements Attachements { get; set; } = new();
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
            Title = FieldsDisplayNames.Back;
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
            Title = FieldsDisplayNames.Edit;
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
            Title = FieldsDisplayNames.Save;
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
            Title = FieldsDisplayNames.Create;
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
            Title = FieldsDisplayNames.Delete;
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

    public class ActionAttachements : FormActionBase
    {
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }

        public ActionAttachements(string entityType = null, Guid? entityId = null, bool visible = false)
        {
            EntityType = entityType;
            EntityId = entityId;
            Visible = visible;
        }
    }

    public class ActionMenu : FormActionBase
    {
        public ActionMenu(bool? visible = null)
        {
            Icon = "bx bx-dots-horizontal-rounded";
            Visible = visible ?? false;
            Title = FieldsDisplayNames.More;
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
