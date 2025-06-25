using System.ComponentModel.DataAnnotations;

namespace TheAccountant.Models.ViewModels
{
    public class RegisterPartialModel
    {
        public RegisterInputModel Input { get; set; } = new();

        public class RegisterInputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }
    }
}
