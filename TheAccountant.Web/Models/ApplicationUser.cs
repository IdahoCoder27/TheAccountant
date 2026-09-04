using Microsoft.AspNetCore.Identity;

namespace TheAccountant.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<Account> Accounts { get; set; }
            = new List<Account>();
    }
}