using BeejaServer.Models;

namespace BeejaServer.Tests;

// run tests instruction:
// > dotnet restore Beerja.slnx
// > dotnet test Beerja.slnx

public class ProductTests
{
    // example shitty test
    // fact tells xUnit that's it an actual test
    [Fact]
    public void NewProduct_HasEmptyName()
    {
        var product = new Product();
        
        // assert esample
        Assert.Equal(string.Empty, product.ProductName);
    }
    
    // if u want to use the same test on
    // different data use [Theory] param:
    // ----------------------------------
    // [Theory]
    // [InlineData(100, 1, 100)]
    // [InlineData(100, 2, 200)]
    // [InlineData(200, 0.5, 100)]
    // public void Calculate_ReturnsExpectedPrice(
    //     double basePrice,
    //     double multiplier,
    //     double expected)
    // {
    //     var actual = basePrice * multiplier;
    // 
    //     Assert.Equal(expected, actual);
    // }
    // -----------------------------------
    // 
}