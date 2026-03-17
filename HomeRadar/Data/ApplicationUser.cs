using HomeRadar.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace HomeRadar.Data
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<SavedFilter> SavedFilters { get; set; } = [];
    }
}
