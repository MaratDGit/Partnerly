using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models
{
    public class Log
    {
        #region ID
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();
        #endregion
        #region UserID
        [Required]
        [Display(Name = "User ID")]
        public Guid? UserId { get; set; }
        #endregion
        #region Action
        [Required]
        [Display(Name = "Action")]
        public string Action { get; set; }
        #endregion
        #region IsDeleted
        public bool IsDeleted { get; set; } = false;
        #endregion
        #region System Columns
        [Required]
        public Guid? CreatedBy { get; set; }
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        [Required]
        public Guid? UpdatedBy { get; set; }
        [Required]
        public DateTime? UpdatedDate { get; set; }
        #endregion

        public User User { get; set; }
    }
}
