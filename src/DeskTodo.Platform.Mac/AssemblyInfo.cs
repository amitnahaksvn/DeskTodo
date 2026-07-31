using System.Runtime.CompilerServices;

// Lets tests construct MacAutoStartService against a scratch plist path (the internal
// constructor overload) instead of the real ~/Library/LaunchAgents.
[assembly: InternalsVisibleTo("DeskTodo.Tests")]
