using System.Runtime.CompilerServices;

// Lets tests call WidgetViewModel.OnDayRolloverTick directly (with a faked TimeProvider)
// to exercise the midnight-rollover logic deterministically, without waiting on a real
// timer or the wall clock.
[assembly: InternalsVisibleTo("DeskTodo.Tests")]
