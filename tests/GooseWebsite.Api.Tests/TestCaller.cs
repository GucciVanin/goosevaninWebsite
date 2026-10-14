using GooseWebsite.Api.Shared.Persistence;

namespace GooseWebsite.Api.Tests;

// A caller a test can switch at will, to check what each kind of caller may read and write.
internal sealed class TestCaller(CallerKind kind = CallerKind.System) : ICurrentCaller
{
    public CallerKind Kind { get; set; } = kind;

    public bool InHttpRequest { get; set; }
}
