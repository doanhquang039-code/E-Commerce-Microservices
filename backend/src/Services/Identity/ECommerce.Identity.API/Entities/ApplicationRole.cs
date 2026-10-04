using Microsoft.AspNetCore.Identity;

namespace ECommerce.Identity.API.Entities;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
