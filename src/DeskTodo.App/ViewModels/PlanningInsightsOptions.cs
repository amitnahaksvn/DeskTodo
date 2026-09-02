namespace DeskTodo.App.ViewModels;

/// <summary>One project's health, display-ready — Feature 51.</summary>
public sealed record ProjectHealthOption(string ProjectName, string Status, string ReasonsDisplay);

/// <summary>One at-risk task, display-ready — Feature 52.</summary>
public sealed record DeadlineRiskOption(string Title, string RiskLevel, string Reason);

/// <summary>One forecast day, display-ready — Feature 53.</summary>
public sealed record WorkloadDayOption(string DateDisplay, string HoursDisplay, bool IsOverloaded);

/// <summary>One accuracy grouping, display-ready — Feature 55.</summary>
public sealed record EstimationAccuracyOption(string GroupName, string AccuracyDisplay, int SampleSize);
