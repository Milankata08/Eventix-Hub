using System.ComponentModel.DataAnnotations;

namespace EventTixHub.Web.Models;

public class PromoCode : BaseEntity, IValidatableObject
{
    [Required, StringLength(20, MinimumLength = 3)]
    [RegularExpression("^[A-Z0-9]+$")]
    public string Code { get; set; } = null!;

    [Range(1, 100)]
    public int DiscountPercent { get; set; }

    [Range(1, int.MaxValue)]
    public int UsageLimit { get; set; }

    [ConcurrencyCheck]
    public int TimesUsed { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ValidUntil <= ValidFrom)
            yield return new ValidationResult("ValidUntil must be after ValidFrom.", [nameof(ValidUntil)]);
    }
}
