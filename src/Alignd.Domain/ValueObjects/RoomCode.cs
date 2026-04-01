namespace Alignd.Domain.ValueObjects;

public sealed record RoomCode
{
    private static readonly string[] Adjectives =
        ["SWIFT", "BRAVE", "CALM", "DARK", "EPIC", "FAST", "GOLD", "IRON", "JADE", "KEEN"];
    private static readonly string[] Nouns =
        ["TIGER", "EAGLE", "SHARK", "WOLF", "RAVEN", "COBRA", "LYNX", "VIPER", "HAWK", "BEAR"];

    public string Value { get; }

    private RoomCode(string value) => Value = value;

    public static RoomCode Generate()
    {
        var rng  = Random.Shared;
        var adj  = Adjectives[rng.Next(Adjectives.Length)];
        var noun = Nouns[rng.Next(Nouns.Length)];
        var num  = rng.Next(10, 99);
        return new RoomCode($"{adj}-{noun}-{num}");
    }

    public static RoomCode From(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Invalid room code");
        return new RoomCode(value.ToUpperInvariant());
    }

    public override string ToString() => Value;
}
