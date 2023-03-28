namespace SyxPack.Tests;

public class ManufacturerTests
{
    [Fact]
    public void ManufacturerDefinition_IsCorrectGroup()
    {
        var definition = new ManufacturerDefinition(new byte[] { 0x40 });
        Assert.Equal(ManufacturerGroup.Japanese, definition.Group);
    }
}