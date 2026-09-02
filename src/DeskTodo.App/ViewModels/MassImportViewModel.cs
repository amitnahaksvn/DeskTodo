using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Mass Import Wizard window — Feature 89 (arbitrary CSV columns mapped to task
/// fields, with a Preview/Validate/Duplicate-Check step before anything is created) and
/// Feature 90 (the same pipeline's run history, since Feature 90's "central migration
/// framework" is this feature's own pipeline generalized — see that entry's roadmap note).
/// The window owns opening the file (same "View picks the file, ViewModel takes a Stream"
/// split <c>ImportExportWindow</c>/<c>ImportExportViewModel</c> already use) and re-opens a
/// fresh read stream from the same picked file for each step (headers, preview, import).
/// </summary>
public sealed partial class MassImportViewModel : ViewModelBase
{
    private static readonly string[] FieldNames =
        ["Title", "Description", "PlanDate", "DueDate", "Priority", "Category", "Notes", "IsCompleted", "IsPinned", "EstimatedMinutes"];

    private readonly IMassImportService _massImportService;
    private readonly ILogger<MassImportViewModel> _logger;

    public MassImportViewModel(IMassImportService massImportService, ILogger<MassImportViewModel> logger)
    {
        _massImportService = massImportService;
        _logger = logger;
        FieldMappings = [.. FieldNames.Select(field => new MassImportFieldMappingRow(field, HeaderOptions))];
    }

    public ObservableCollection<string?> HeaderOptions { get; } = [null];

    public ObservableCollection<MassImportFieldMappingRow> FieldMappings { get; }

    public ObservableCollection<string> PreviewErrorLines { get; } = [];

    public ObservableCollection<string> MigrationRunSummaries { get; } = [];

    [ObservableProperty]
    public partial string SelectedFileName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int? PreviewTotalRows { get; set; }

    [ObservableProperty]
    public partial int? PreviewDuplicateCount { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var runs = await _massImportService.GetMigrationRunsAsync(cancellationToken);
            MigrationRunSummaries.Clear();
            foreach (var run in runs)
            {
                var outcome = run.Status == MigrationStatus.Completed
                    ? $"{run.ImportedCount} imported, {run.SkippedCount} skipped"
                    : "failed validation — nothing imported";
                MigrationRunSummaries.Add($"{run.StartedAt:yyyy-MM-dd HH:mm} — {run.SourceDescription} ({run.TotalRecords} rows): {outcome}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load migration run history");
        }
    }

    public async Task LoadHeadersAsync(Stream source, string fileName, CancellationToken cancellationToken = default)
    {
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;
        PreviewTotalRows = null;
        PreviewDuplicateCount = null;
        PreviewErrorLines.Clear();

        try
        {
            var headers = await _massImportService.ReadCsvHeadersAsync(source, cancellationToken);
            SelectedFileName = fileName;

            HeaderOptions.Clear();
            HeaderOptions.Add(null);
            foreach (var header in headers)
            {
                HeaderOptions.Add(header);
            }

            foreach (var row in FieldMappings)
            {
                // Auto-map when the CSV already has a column with exactly the target field's name.
                row.SelectedHeader = headers.FirstOrDefault(h => string.Equals(h, row.FieldName, StringComparison.OrdinalIgnoreCase));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read CSV headers from '{FileName}'", fileName);
            ErrorMessage = "Couldn't read that file's columns.";
        }
    }

    internal Dictionary<string, string> BuildColumnToFieldMapping() =>
        FieldMappings
            .Where(row => row.SelectedHeader is not null)
            .ToDictionary(row => row.SelectedHeader!, row => row.FieldName);

    public async Task PreviewAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;

        try
        {
            var result = await _massImportService.PreviewAsync(source, BuildColumnToFieldMapping(), cancellationToken);
            PreviewTotalRows = result.TotalRows;
            PreviewDuplicateCount = result.DuplicateCount;

            PreviewErrorLines.Clear();
            foreach (var row in result.Rows.Where(r => r.ValidationErrors.Count > 0))
            {
                PreviewErrorLines.Add($"Row {row.RowNumber}: {string.Join("; ", row.ValidationErrors)}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preview a mass import");
            ErrorMessage = "Couldn't preview that file.";
        }
    }

    public async Task ImportAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;

        try
        {
            var run = await _massImportService.ImportAsync(source, BuildColumnToFieldMapping(), SelectedFileName, cancellationToken);
            StatusMessage = run.Status == MigrationStatus.Completed
                ? $"Imported {run.ImportedCount} of {run.TotalRecords} row(s); {run.SkippedCount} skipped as duplicates."
                : "Import aborted — some rows failed validation. Fix the mapping and preview again.";

            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import '{FileName}'", SelectedFileName);
            ErrorMessage = "Couldn't import that file.";
        }
    }

    [RelayCommand]
    private void Reset()
    {
        SelectedFileName = string.Empty;
        HeaderOptions.Clear();
        HeaderOptions.Add(null);
        foreach (var row in FieldMappings)
        {
            row.SelectedHeader = null;
        }

        PreviewTotalRows = null;
        PreviewDuplicateCount = null;
        PreviewErrorLines.Clear();
        StatusMessage = string.Empty;
        ErrorMessage = string.Empty;
    }
}
