using Partnerly.Infrastructure.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models
{
    public class Transaction : IAuditableEntity
    {
        #region Id
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();
        #endregion
        #region UserId
        [Required]
        [Display(Name = "User Id")]
        public Guid UserId { get; set; }
        #endregion
        #region Points
        [Display(Name = "Points")]
        public decimal Points { get; set; }
        #endregion
        #region Descriptions
        [Display(Name = "Description")]
        public string Description { get; set; }
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
        public bool IsDeleted { get; set; } = false;
        #endregion

        public User? User { get; set; }
    }

}
