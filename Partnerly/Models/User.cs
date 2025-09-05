using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models
{
    public class User
    {
        #region ID
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();
        #endregion
        #region Email
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }
        #endregion
        #region PasswordHash
        [Required]
        [Display(Name = "Password")]
        public string PasswordHash { get; set; }
        #endregion
        #region Phone
        [Required]
        [Display(Name = "Phone")]
        public string Phone { get; set; }
        #endregion
        #region FirstName
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        #endregion
        #region LastName
        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
        #endregion
        #region PhotoUrl
        public string? PhotoUrl { get; set; }
        #endregion
        #region Balance
        [Display(Name = "Balance")]
        public decimal Balance { get; set; }
        #endregion
        #region RoleId
        [Required]
        [Display(Name = "Role")]
        public Guid RoleId { get; set; }
        #endregion
        #region ReferrerId
        [Display(Name = "Referrer")]
        public Guid? ReferrerId { get; set; }
        #endregion

        #region System Columns
        public bool IsDeleted { get; set; } = false;
        [Required]
        public Guid? CreatedBy { get; set; }
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        [Required]
        public Guid? UpdatedBy { get; set; }
        [Required]
        public DateTime? UpdatedDate { get; set; }
        #endregion

        public Role Role { get; set; }
        public User? Referrer { get; set; }
        public ICollection<User> Referrals { get; set; }
        public ICollection<Transaction> Transactions { get; set; }
        public ICollection<Payment> Payments { get; set; }
        public ICollection<Log> Logs { get; set; }
    }
}
