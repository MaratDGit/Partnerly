using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Электронная почта oбязательное поле")]
        [EmailAddress(ErrorMessage = "")]
        [Display(Name = "Электронная почта")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль oбязательное поле")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [Display(Name = "Запомнить меня")]
        public bool RememberMe { get; set; }
    }
}
