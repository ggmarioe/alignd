using Alignd.Domain.ValueObjects;

namespace Alignd.Tests.Domain;

[TestFixture]
public sealed class RoomCodeTests
{
    [Test]
    public void Generate_ReturnsCode_WithExpectedAdjectiveNounNumberFormat()
    {
        var code = RoomCode.Generate();

        var parts = code.Value.Split('-');
        Assert.That(parts, Has.Length.EqualTo(3),
            "Room code must have exactly three parts separated by hyphens: ADJECTIVE-NOUN-NUMBER");
    }

    [Test]
    public void Generate_ReturnsCode_WhereNumberIsBetween10And98Inclusive()
    {
        var code = RoomCode.Generate();

        var numberPart = code.Value.Split('-')[2];
        var parsed = int.Parse(numberPart);
        Assert.That(parsed, Is.InRange(10, 98),
            "The numeric suffix must be between 10 and 98 (exclusive upper bound of Random.Next(10, 99))");
    }

    [Test]
    public void Generate_ReturnsCode_ThatIsAllUpperCase()
    {
        var code = RoomCode.Generate();

        Assert.That(code.Value, Is.EqualTo(code.Value.ToUpperInvariant()),
            "Generated room code must be in uppercase");
    }

    [Test]
    public void Generate_CalledMultipleTimes_ProducesUniqueCodesInPractice()
    {
        var codes = Enumerable.Range(0, 50)
            .Select(_ => RoomCode.Generate().Value)
            .ToHashSet();

        Assert.That(codes.Count, Is.GreaterThan(1),
            "Generating 50 room codes should produce more than one distinct value");
    }

    [Test]
    public void From_GivenValidLowercaseCode_ConvertsItToUpperCase()
    {
        var code = RoomCode.From("swift-tiger-42");

        Assert.That(code.Value, Is.EqualTo("SWIFT-TIGER-42"),
            "From() must normalise the input to uppercase");
    }

    [Test]
    public void From_GivenAlreadyUpperCaseCode_RetainsValue()
    {
        var code = RoomCode.From("BRAVE-EAGLE-77");

        Assert.That(code.Value, Is.EqualTo("BRAVE-EAGLE-77"));
    }

    [Test]
    public void From_GivenNullOrWhitespace_ThrowsArgumentException(
        [Values("", "   ", "\t")] string invalid)
    {
        Assert.Throws<ArgumentException>(() => RoomCode.From(invalid),
            "From() must reject blank or whitespace-only strings");
    }

    [Test]
    public void ToString_ReturnsTheSameValueAsTheValueProperty()
    {
        var code = RoomCode.From("CALM-WOLF-55");

        Assert.That(code.ToString(), Is.EqualTo(code.Value));
    }
}
