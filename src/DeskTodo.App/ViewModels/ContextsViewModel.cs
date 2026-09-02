using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Focus Contexts management window (Feature 63, Roadmap-39-100.md) — create/delete
/// the Work/Personal/Learning-style labels the task editor's "Context" picker offers.
/// </summary>
public sealed partial class ContextsViewModel(IFocusContextRepository contextRepository, ILogger<ContextsViewModel> logger) : ViewModelBase
{
    public ObservableCollection<FocusContext> Contexts { get; } = [];

    public IReadOnlyList<string> ColorPresets { get; } = ["#EF4444", "#F97316", "#F59E0B", "#22C55E", "#3B82F6", "#8B5CF6", "#EC4899", "#64748B"];

    [ObservableProperty]
    public partial string NewContextName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewContextColorHex { get; set; } = "#3B82F6";

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var contexts = await contextRepository.GetAllAsync(cancellationToken);
            Contexts.Clear();
            foreach (var context in contexts)
            {
                Contexts.Add(context);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load Focus Contexts");
        }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var name = NewContextName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        try
        {
            await contextRepository.AddAsync(new FocusContext { Name = name, ColorHex = NewContextColorHex });
            NewContextName = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add Focus Context '{Name}'", name);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(Guid contextId)
    {
        try
        {
            await contextRepository.DeleteAsync(contextId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete Focus Context {ContextId}", contextId);
        }
    }
}
