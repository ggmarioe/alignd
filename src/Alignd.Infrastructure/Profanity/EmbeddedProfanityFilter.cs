using System.Collections.Frozen;
using System.Reflection;
using Alignd.Application.Interfaces;

namespace Alignd.Infrastructure.Profanity;

public sealed class EmbeddedProfanityFilter : IProfanityFilter
{
    private readonly FrozenSet<string> _words;

    public EmbeddedProfanityFilter()
    {
        var asm  = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames()
                      .FirstOrDefault(n => n.EndsWith("profanity.txt"));

        if (name is null)
        {
            _words = FrozenSet<string>.Empty;
            return;
        }

        using var stream = asm.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);

        _words = reader.ReadToEnd()
                       .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                       .Select(w => w.Trim().ToLowerInvariant())
                       .Where(w => !string.IsNullOrWhiteSpace(w) && !w.StartsWith('#'))
                       .ToFrozenSet();
    }

    public bool IsProfane(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        var normalized = input.Trim().ToLowerInvariant();
        return _words.Contains(normalized) || _words.Any(w => normalized.Contains(w));
    }
}
