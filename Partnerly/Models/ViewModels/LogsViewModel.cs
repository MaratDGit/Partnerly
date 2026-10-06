using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class LogsViewModel : IAuditableViewModel
    {
        public Guid Id { get; set; }

        #region Action
        [Display(Name = nameof(FieldsDisplayNames.Action), ResourceType = typeof(FieldsDisplayNames))]
        public string? Action { get; set; }
        public string? ActionView { get => AttributeDropdownHelper.GetValue<LogActionsAttribute>(Action); }
        #endregion
        #region Type
        [Display(Name = nameof(FieldsDisplayNames.Type), ResourceType = typeof(FieldsDisplayNames))]
        [LogType]
        public string? Type { get; set; }
        public string? TypeView { get => AttributeDropdownHelper.GetValue<LogTypeAttribute>(Type);}
        #endregion
        #region LogMessage
        [Display(Name = nameof(FieldsDisplayNames.Message), ResourceType = typeof(FieldsDisplayNames))]
        public string? LogMessage { get; set; }
        #endregion
        #region FilePath 
        [Display(Name = nameof(FieldsDisplayNames.FilePath), ResourceType = typeof(FieldsDisplayNames))]
        public string? FilePath { get; set; }
        #endregion
        #region Method
        [Display(Name = nameof(FieldsDisplayNames.Method), ResourceType = typeof(FieldsDisplayNames))]
        public string? Method { get; set; }
        #endregion
        #region LineNumber 
        [Display(Name = nameof(FieldsDisplayNames.LineNumber), ResourceType = typeof(FieldsDisplayNames))]
        public int? LineNumber { get; set; }
        #endregion

        #region System Columns
        public string? Note { get; set; }
        public bool IsDeleted { get; set; } = false;
        #endregion

        #region Audit fields
        [Display(Name = nameof(FieldsDisplayNames.CreatorName), ResourceType = typeof(FieldsDisplayNames))]
        public string? CreatedByUserName { get; set; }
        public Guid? CreatedBy { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.CreatedDate), ResourceType = typeof(FieldsDisplayNames))]
        public DateTime? CreatedDate { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.UpdaterName), ResourceType = typeof(FieldsDisplayNames))]
        public string? UpdatedByUserName { get; set; }
        public Guid? UpdatedBy { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.UpdatedDate), ResourceType = typeof(FieldsDisplayNames))]
        public DateTime? UpdatedDate { get; set; }
        #endregion
    }
}
