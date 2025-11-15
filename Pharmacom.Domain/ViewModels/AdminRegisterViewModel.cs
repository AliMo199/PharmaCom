using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.ViewModels
{
    public class AdminRegisterViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "User Name")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long.", MinimumLength = 6)]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Please select at least one role")]
        [Display(Name = "User Roles")]
        public List<string> SelectedRoles { get; set; } = new List<string>();

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; } = true;

        [Display(Name = "Phone Confirmed")]
        public bool PhoneNumberConfirmed { get; set; } = false;

        // Address Information (Optional)
        [Display(Name = "Address Line")]
        public string Line1 { get; set; }

        public string City { get; set; }

        public string Governorate { get; set; }
    }

}
