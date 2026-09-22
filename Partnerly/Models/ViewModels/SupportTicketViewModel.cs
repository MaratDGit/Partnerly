using Microsoft.AspNetCore.Mvc.Rendering;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class SupportTicketViewModel
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid? UserId { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.User), ResourceType = typeof(FieldsDisplayNames))]
        public string? CreatorName { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.TaskNumber), ResourceType = typeof(FieldsDisplayNames))]
        public string? TicketID { get; set; }

        [CusromStringLenght(max:50)]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Subject), ResourceType = typeof(FieldsDisplayNames))]
        public string? Subject { get; set; }

        [SupportTicketStatus]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Message), ResourceType = typeof(FieldsDisplayNames))]
        public string? Message { get; set; }

        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Status), ResourceType = typeof(FieldsDisplayNames))]
        public string? Status { get; set; }
        public string? StatusAsView { get => AttributeDropdownHelper.GetValue<SupportTicketStatusAttribute>(Status);}
        public List<SelectListItem> TicketStatuses { get => AttributeDropdownHelper.FromAttribute<SupportTicketStatusAttribute>(); }

        public Guid? AssignedTo { get; set; }
        [Display(Name = nameof(FieldsDisplayNames.AssignedTo), ResourceType = typeof(FieldsDisplayNames))]
        public string? AssignedToName { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.IsRead), ResourceType = typeof(FieldsDisplayNames))]
        public bool? IsRead { get; set; } = false;
        public bool IsReadBool { get => IsRead ?? false; set => IsRead = value; }

        #region System Columns
        public string? Note { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        #endregion
    }
}
