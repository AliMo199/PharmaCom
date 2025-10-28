using System.ComponentModel.DataAnnotations;

namespace PharmaCom.Domain.ViewModels
{
    public class LogInViewModel
    {
        [Required(ErrorMessage = "*")]
        [Display(Name = "User Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "*")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }
    }
}