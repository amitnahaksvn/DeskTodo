using DeskTodo.App.ViewModels;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Security;
using DeskTodo.Application.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class LockScreenViewModelTests
{
    private readonly Mock<ISettingsService> _settingsService = new();
    private readonly LockScreenViewModel _sut;

    public LockScreenViewModelTests()
    {
        _sut = new LockScreenViewModel(_settingsService.Object, NullLogger<LockScreenViewModel>.Instance);
    }

    [Fact]
    public async Task UnlockAsync_WithTheCorrectPin_RaisesUnlocked_AndClearsAnyError()
    {
        var (salt, hash) = PinHasher.Hash("4242");
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { PinLockEnabled = true, PinSalt = salt, PinHash = hash });
        _sut.EnteredPin = "4242";
        var unlocked = false;
        _sut.Unlocked += (_, _) => unlocked = true;

        await _sut.UnlockCommand.ExecuteAsync(null);

        Assert.True(unlocked);
        Assert.Equal(string.Empty, _sut.ErrorMessage);
    }

    [Fact]
    public async Task UnlockAsync_WithTheWrongPin_DoesNotRaiseUnlocked_AndSetsAnErrorMessage_AndClearsTheField()
    {
        var (salt, hash) = PinHasher.Hash("4242");
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { PinLockEnabled = true, PinSalt = salt, PinHash = hash });
        _sut.EnteredPin = "0000";
        var unlocked = false;
        _sut.Unlocked += (_, _) => unlocked = true;

        await _sut.UnlockCommand.ExecuteAsync(null);

        Assert.False(unlocked);
        Assert.NotEqual(string.Empty, _sut.ErrorMessage);
        Assert.Equal(string.Empty, _sut.EnteredPin);
    }
}
