using System.ComponentModel.DataAnnotations;

namespace PharmaCom.Domain.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "*")]
        [Display(Name = "User Name")]
        public string UserName { get; set; }

        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "*")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "*")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "*")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
        [Required(ErrorMessage = "*")]
        [Phone(ErrorMessage = "Invalid Phone Number")]
        public string PhoneNumber { get; set; }


        [Display(Name = "Address Line")]
        public string Line1 { get; set; }

        public string City { get; set; }

        public string Governorate { get; set; }

    }
}
