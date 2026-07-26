using Avalonia.Headless;

namespace DeskTodo.Tests.Views;

/// <summary>
/// One shared <see cref="HeadlessUnitTestSession"/> for every headless UI render test in
/// this assembly — starting Avalonia's platform/dispatcher thread is expensive enough that
/// a fresh session per test would meaningfully slow the suite down.
/// </summary>
public sealed class HeadlessSessionFixture : IDisposable
{
    public HeadlessUnitTestSession Session { get; } = HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder));

    public void Dispose() => Session.Dispose();
}

[CollectionDefinition(nameof(HeadlessCollection))]
public sealed class HeadlessCollection : ICollectionFixture<HeadlessSessionFixture>;
