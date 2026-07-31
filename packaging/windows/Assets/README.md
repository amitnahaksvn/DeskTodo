# MSIX logo assets

`scripts/package-windows.ps1` expects real DeskTodo branding here before it
will pack a package (see the script's own doc comment — it checks for these
and stops early with a clear error if they're missing, rather than letting
`makeappx.exe` fail on a manifest referencing nonexistent files):

| File | Size |
|---|---|
| `StoreLogo.png` | 50×50 |
| `Square150x150Logo.png` | 150×150 |
| `Square44x44Logo.png` | 44×44 |
| `Wide310x150Logo.png` | 310×150 |

This repo currently only has `src/DeskTodo.App/Assets/avalonia-logo.ico`
(the Avalonia template placeholder, not real DeskTodo branding) — not a
substitute for these. Generate proper PNGs at each size from real artwork
before packaging.
