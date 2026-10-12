using System.ComponentModel.DataAnnotations;
using EventTixHub.Web.ViewModels;

namespace EventTixHub.Tests.Unit;

public class ShowtimeFormVMTests
{
    private static List<string> InvalidMembers(ShowtimeFormVM vm)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(vm, new ValidationContext(vm), results, validateAllProperties: true);
        return results.SelectMany(r => r.MemberNames).ToList();
    }

    private static ShowtimeFormVM Valid() =>
        new() { EventId = 1, VenueId = 1, StartsAt = DateTime.Now.AddDays(1), StandardPrice = 10m, VipPrice = 20m };

    [Fact]
    public void NewForm_HasNoPricePrefilled() =>
        Assert.Equal((null, null), (new ShowtimeFormVM().StandardPrice, new ShowtimeFormVM().VipPrice));

    [Fact]
    public void MissingOrZeroPrices_AreRejected()
    {
        var missing = Valid();
        missing.StandardPrice = missing.VipPrice = null;
        var zero = Valid();
        zero.StandardPrice = zero.VipPrice = 0m;

        Assert.Equal(["StandardPrice", "VipPrice"], InvalidMembers(missing).Order());
        Assert.Equal(["StandardPrice", "VipPrice"], InvalidMembers(zero).Order());
        Assert.Empty(InvalidMembers(Valid()));
    }
}
