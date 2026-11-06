function Assert-NormalAcceptanceTestResults {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable[]] $Runs
    )

    if ($Runs.Count -ne 2 -or (@($Runs.TargetFramework | Sort-Object -Unique) -join ';') -ne 'net451;netcoreapp2.1') {
        throw 'Normal acceptance validation requires both net451 and netcoreapp2.1.'
    }

    $failures = [System.Collections.Generic.List[string]]::new()
    foreach ($run in $Runs) {
        if ($run.ExitCode -ne 0) {
            $failures.Add("$($run.TargetFramework): patched runner exited $($run.ExitCode).")
        }
        if (-not (Test-Path -LiteralPath $run.Results -PathType Leaf)) {
            $failures.Add("$($run.TargetFramework): no TRX produced at $($run.Results).")
            continue
        }

        [xml] $trx = Get-Content -LiteralPath $run.Results -Raw
        $ns = [Xml.XmlNamespaceManager]::new($trx.NameTable)
        $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
        $counters = $trx.SelectSingleNode('/t:TestRun/t:ResultSummary/t:Counters', $ns)
        $results = @($trx.SelectNodes('/t:TestRun/t:Results//t:UnitTestResult', $ns))
        $definitions = @($trx.SelectNodes('/t:TestRun/t:TestDefinitions/t:UnitTest/t:TestMethod', $ns))
        if ($null -eq $counters -or [int] $counters.total -le 0 -or [int] $counters.executed -le 0 -or $results.Count -eq 0) {
            $failures.Add("$($run.TargetFramework): TRX contains no executed tests.")
            continue
        }
        if ($definitions.Count -eq 0 -or @($definitions | Where-Object { $_.className -notlike 'Microsoft.TestPlatform.AcceptanceTests.*' }).Count -ne 0) {
            $failures.Add("$($run.TargetFramework): TRX does not identify the acceptance suite.")
        }
        if ([int] $counters.failed -ne 0 -or @($results | Where-Object { $_.outcome -notin @('Passed', 'NotExecuted') }).Count -ne 0) {
            $failures.Add("$($run.TargetFramework): acceptance tests failed.")
        }
        Write-Host "$($run.TargetFramework): total=$($counters.total), executed=$($counters.executed), passed=$($counters.passed), failed=$($counters.failed)"
    }

    if ($failures.Count -ne 0) {
        throw "Normal acceptance validation failed:`n$($failures -join "`n")"
    }
}
