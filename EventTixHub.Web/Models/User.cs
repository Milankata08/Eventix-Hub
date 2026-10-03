using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace EventTixHub.Web.Models;

public class User : IdentityUser<int>
{
    [Required, StringLength(50)]
    public string FirstName { get; set; } = null!;

    [Required, StringLength(50)]
    public string LastName { get; set; } = null!;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

public class Role : IdentityRole<int>
{
    public Role() { }
    public Role(string name) : base(name) { }
}
