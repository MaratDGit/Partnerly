using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class LogsViewModel
    {
        public Guid Id { get; set; }

        #region Action
        [Display(Name = FieldsDisplayNames.Action)]
        public string? Action { get; set; }
        public string? ActionView { get => AttributeDropdownHelper.GetValue<LogActionsAttribute>(Action); }
        #endregion
        #region Type
        [Display(Name = FieldsDisplayNames.Type)]
        [LogType]
        public string? Type { get; set; }
        public string? TypeView { get => AttributeDropdownHelper.GetValue<LogTypeAttribute>(Type);}
        #endregion
        #region LogMessage
        [Display(Name = FieldsDisplayNames.Message)]
        public string? LogMessage { get; set; }
        #endregion
        #region FilePath 
        [Display(Name = FieldsDisplayNames.FilePath)]
        public string? FilePath { get; set; }
        #endregion
        #region Method
        [Display(Name = FieldsDisplayNames.Method)]
        public string? Method { get; set; }
        #endregion
        #region LineNumber 
        [Display(Name = FieldsDisplayNames.LineNumber)]
        public int? LineNumber { get; set; }
        #endregion

        #region System Columns
        public bool IsDeleted { get; set; } = false;
        public Guid? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        #endregion
    }
}
