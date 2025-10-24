param(
   [string[]] $Path
)

dotnet nuget verify $Path
if ($LASTEXITCODE -ne 0) {
    Write-Error "The verify tool found some problems. See above."
} else {
    Write-Output "The verify tool succeeded."
}
