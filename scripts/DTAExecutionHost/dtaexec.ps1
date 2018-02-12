
$root = $args[0]
$path = Join-Path $root "args.txt"
$testresults =  Join-Path $root "TestResults"
$args[2] > $path




Add-Type -AssemblyName System.IO.Compression.FileSystem

$makejson = cat ./make.json | ConvertFrom-Json
$url = echo $makejson.externals.archivePackages[1].url

(New-Object System.Net.WebClient).DownloadFile($url, "TestExecution.zip")
mkdir .\TestExecution

[System.IO.Compression.ZipFile]::ExtractToDirectory("TestExecution.zip", ".\TestExecution");

$p = New-Object System.Diagnostics.Process
$p.StartInfo.UseShellExecute = $false
$p.StartInfo.FileName = "TestExecution\\DTAExecutionHost.exe"
$p.StartInfo.WorkingDirectory = $root

$envjson = cat ./env.json | ConvertFrom-Json

$props=Get-Member -InputObject $envjson -MemberType NoteProperty

foreach($prop in $props) {
    $propValue=$envjson | Select-Object -ExpandProperty $prop.Name
    $p.StartInfo.EnvironmentVariables[$prop.Name] = $propValue
}

$p.StartInfo.EnvironmentVariables["DTA.VstestConsole"] = $args[1];
$p.StartInfo.EnvironmentVariables["DTA.TiaRunIdFile"] = $path;
$p.StartInfo.EnvironmentVariables["DTA.ResponseFile"] = $path;
$p.StartInfo.EnvironmentVariables["DTA.TestResultDirectory"] = $testresults;


$p.Start()
$p.WaitForExit()

Remove-Item -Path .\TestExecution.zip -Confirm:$false
Remove-Item -Path .\TestExecution -Confirm:$false -Recurse
