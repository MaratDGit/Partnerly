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
        [Display(Name = FieldsDisplayNames.User)]
        public string? CreatorName { get; set; }

        [Display(Name = FieldsDisplayNames.TaskNumber)]
        public string? TicketID { get; set; }

        [CusromStringLenght(max:50)]
        [Required(ErrorMessage = $"{FieldsDisplayNames.Subject} {ErrorMessages.FieldRequired}")]
        [Display(Name = FieldsDisplayNames.Subject)]
        public string? Subject { get; set; }

        [SupportTicketStatus]
        [Required(ErrorMessage = $"{FieldsDisplayNames.Subject} {ErrorMessages.FieldRequired}")]
        [Display(Name = FieldsDisplayNames.Message)]
        public string? Message { get; set; }

        [Required(ErrorMessage = $"{FieldsDisplayNames.Status} {ErrorMessages.FieldRequired}")]
        [Display(Name = FieldsDisplayNames.Status)]
        public string? Status { get; set; }
        public string? StatusAsView { get => AttributeDropdownHelper.GetValue<SupportTicketStatusAttribute>(Status);}
        public List<SelectListItem> TicketStatuses { get => AttributeDropdownHelper.FromAttribute<SupportTicketStatusAttribute>(); }

        public Guid? AssignedTo { get; set; }
        [Display(Name = FieldsDisplayNames.AssignedTo)]
        public string? AssignedToName { get; set; }

        [Display(Name = FieldsDisplayNames.IsRead)]
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
