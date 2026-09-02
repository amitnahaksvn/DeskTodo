namespace DeskTodo.Application.Services;

/// <summary>Subscribes to the app's <see cref="Events.IEventBus"/> and fans matching events out to enabled webhooks. Resolved once and started at app startup (see <c>App.axaml.cs</c>'s <c>SetupWebhookDispatcher</c>).</summary>
public interface IWebhookDispatcher
{
    void Start();
}
