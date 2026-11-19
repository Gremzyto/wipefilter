# Compile WipeFilter.exe (WinForms, .NET Framework 4, sans dépendance).
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc  = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$out  = Join-Path $root "out\WipeFilter.exe"
New-Item -ItemType Directory -Force (Join-Path $root "out") | Out-Null
& $csc /nologo /codepage:65001 /target:winexe /platform:x64 /optimize+ `
    /win32icon:"$root\src\app.ico" /win32manifest:"$root\src\app.manifest" /debug- /out:$out `
    /r:System.Windows.Forms.dll /r:System.Drawing.dll `
    "$root\src\WipeFilter.cs" "$root\src\UiKit.cs" "$root\src\Program.cs"
if ($LASTEXITCODE -ne 0) { throw "Compilation échouée ($LASTEXITCODE)" }
$h = (Get-FileHash $out -Algorithm SHA256).Hash
"OK : $out"
"Taille : $((Get-Item $out).Length) octets"
"SHA-256 : $h"
