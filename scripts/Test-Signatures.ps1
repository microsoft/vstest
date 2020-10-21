[CmdletBinding()]
param (
    [String]
    $Path = "$PSScriptRoot\packages",

    [Switch]
    $CIBuild
)

Add-Type -AssemblyName System.IO.Compression.FileSystem

# Allowed certificate subjects
$script:ValidSubjects = @( 
    "CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US" 
    "CN=Microsoft Corporation, OU=MOPR, O=Microsoft Corporation, L=Redmond, S=Washington, C=US" 
)

# Allowed certificate thumbprints
$script:ValidThumbprints = @( 
    "B9EAA034C821C159B05D3521BCF7FEB796EBD6FF" 
    "98ED99A67886D020C564923B7DF25E9AC019DF26" 
    "5EAD300DC7E4D637948ECB0ED829A072BD152E17" 
    "67B1757863E3EFF760EA9EBB02849AF07D3A8080" 
    "9DC17888B5CFAD98B3CB35C1994E96227F061675" 
    "62009AAABDAE749FD47D19150958329BF6FF4B34" 
    "3BDA323E552DB1FDE5F4FBEE75D6D5B2B187EEDC" 
    "899FA016DEE8E665FF2A315A1151C43FB96C430B"  # Microsoft 3rd Party Application Component
)

# Allow-list for unsigned binaries
# No binaries allow to ship without a valid signature as per policy
$script:UnsignedAllowList = @{
    # "PackageId" = relative paths
    # "Microsoft.TestPlatform" = @( `
    #     ".\tools\net451\Common7\IDE\Extensions\TestPlatform\Newtonsoft.Json.dll" `
    # )
}

$script:Depth = 0

function Get-RelativePath($Path, $RelativeTo) {
    try {
        Push-Location
        Set-Location $RelativeTo
        $Path = Resolve-Path $Path -Relative
    }
    finally {
        Pop-Location
    }

    $Path
}

function Push-Depth {
    $script:Depth = $script:Depth + 1
}

function Pop-Depth {
    $script:Depth = $script:Depth - 1

    if($script:Depth -lt 0) {
        $script:Depth = 0
    }
}

function Clear-Line($Message = "") {
    if($CIBuild) {
        return
    }
    
    $x = [System.Console]::get_CursorLeft()
    Write-Host "`r$(' ' * $x)$Message`r" -NoNewLine
}

function Update-Operation($Operation, $Message, [switch]$NewLine, [switch]$Suppress) {
    if($Suppress) {
        return
    }
    
    Clear-Line
    Write-Host "$('  ' * $Depth)[$Operation] " -ForegroundColor Yellow -NoNewline
    Write-Host $Message -NoNewline

    if ($newline -or $CIBuild) {
        Write-Host
    }
}

function Submit-Operation($Operation, $Message, [switch]$NoNewLine, [switch]$Suppress) {
    if($Suppress) {
        return
    }
    
    Clear-Line
    Write-Host "$('  ' * $Depth)[$Operation] " -ForegroundColor Green -NoNewline
    Write-Host $Message -NoNewline

    if (-not $NoNewLine -or $CIBuild) {
        Write-Host
    }
}

function Undo-Operation($Operation, $Message, [switch]$NoNewLine, [switch]$Suppress) {
    if($Suppress) {
        return
    }
    
    Clear-Line
    Write-Host "$('  ' * $Depth)[$Operation] " -ForegroundColor Red -NoNewline
    Write-Host $Message -NoNewline

    if (-not $NoNewLine -or $CIBuild) {
        Write-Host
    }
}

Function Invoke-Command ($Command, $Arguments)
{
    $pinfo = New-Object System.Diagnostics.ProcessStartInfo
    $pinfo.FileName = $Command
    $pinfo.RedirectStandardError = $true
    $pinfo.RedirectStandardOutput = $true
    $pinfo.UseShellExecute = $false
    $pinfo.Arguments = $Arguments
    $p = New-Object System.Diagnostics.Process
    $p.StartInfo = $pinfo
    $p.Start() | Out-Null
    $p.WaitForExit()
    
    [pscustomobject]@{
        stdout = $p.StandardOutput.ReadToEnd()
        stderr = $p.StandardError.ReadToEnd()
        ExitCode = $p.ExitCode
    }
}

function Test-NuGetSignatures {
    $failed = @{}
    
    $nupgks = Get-ChildItem *.nupkg
 
    $nugetDir = "$PSScriptRoot\..\tools\nuget"
    $nugetPath = "$nugetDir\nuget.exe"
    
    Push-Depth
    if(-not (Test-Path $nugetDir))
    {
        # Create the directory for nuget.exe if it does not exist
        New-Item -ItemType Directory -Force -Path $nugetDir | Out-Null

        try {
            $progressPreference = 'silentlyContinue'
            $nugetUrl = "https://dist.nuget.org/win-x86-commandline/v4.6.1/nuget.exe"
            Update-Operation -operation "downloading" -message $nugetUrl -suppress:$CIBuild
            Invoke-WebRequest $nugetUrl -OutFile $nugetPath 
            Submit-Operation -operation "downloaded" -message $nugetUrl
        }
        finally {
            $progressPreference = 'Continue'
        }
    }

    $nupgks | ForEach-Object {
        $relativePath = $_.Name
        Update-Operation -operation "processing" -message $relativePath -suppress:$CIBuild

        $verificationProcess = Invoke-Command $nugetPath ("verify","-signature","-CertificateFingerprint","3F9001EA83C560D712C24CF213C3D312CB3BFF51EE89435D3430BD06B5D0EECE", $_.FullName)
        if ($verificationProcess.ExitCode -eq 0) {
            Submit-Operation -operation "valid" -message $relativePath
        }
        else {
            Undo-Operation -operation "unsigned" -message $relativePath -NewLine
            $failed.Add($_.FullName, $null)
        }
    }

    if($failed.Count -eq 0) {
        Submit-Operation -operation "done" -message "$($nupgks.Length) packages passed signature checks."
    } elseif ($failed.Count -eq $nupgks.Length) {
        Undo-Operation -operation "failed" -message "$($nupgks.Length) packages failed signature checks." 
    } else {
        Undo-Operation -operation "partial" -message "$($nupgks.Length - $failed.Count) packages passed signature checks and $($failed.Count) packages failed!" 
    }

    Write-Host
    Pop-Depth
    $failed.Count
}

function Expand-NugetPackages {
    $foldersToProcess = [System.Collections.Generic.HashSet[string]]@()
    $nupgks = Get-ChildItem *.nupkg

    Push-Depth
    $nupgks | ForEach-Object {
        $package = $_.FullName
        $file = $_.Name
        $expand = Join-Path -Path ([System.IO.Path]::GetDirectoryName($package)) -ChildPath ([System.IO.Path]::GetFileNameWithoutExtension($package))
        if (Test-Path -Path $expand) {
            Remove-Item $expand -Force -Recurse
        }

        Update-Operation -operation "extracting" -message $file
        [System.IO.Compression.ZipFile]::ExtractToDirectory($package, $expand)
        Submit-Operation -operation "done" -message $file -suppress:$CIBuild

        $foldersToProcess.Add($expand) | Out-Null
    }
    Pop-Depth
    Write-Host
    
    $foldersToProcess
}

function Test-Signatures($RootFolder) {
    $failed = @{}

    Update-Operation -operation "discovering" -message "Searching binaries..."  -suppress:$CIBuild
    $files = (Get-ChildItem $RootFolder -Recurse -Include "*.dll", "*.exe")
    Update-Operation -operation "discovered" -message "$($files.Length) binaries discovered"

    $packageId = ([XML](Get-Content $RootFolder\*.nuspec)).package.metadata.id
    $unsignedAllowList = @()
    if ($script:UnsignedAllowList.ContainsKey($packageId)) {
        $unsignedAllowList = $script:UnsignedAllowList[$packageId]
    }

    $files | ForEach-Object {
        $relativePath = Get-RelativePath -path $_.FullName -relativeTo $RootFolder
        Update-Operation -operation "processing" -message $relativePath -suppress:$CIBuild

        $signature = Get-AuthenticodeSignature -FilePath "$($_.FullName)"
        if ($signature.Status -eq "Valid") {
            $valid = ($script:ValidSubjects.Contains($signature.SignerCertificate.Subject) -or $script:ValidThumbprints.Contains($signature.SignerCertificate.Thumbprint))
            
            if ($valid) {
                Submit-Operation -operation "valid" -message "$relativePath" -NoNewLine -suppress:$CIBuild
            } else {
                Update-Operation -operation "inconclusive" -message "$relativePath ($($signature.SignerCertificate.Subject)) [$($signature.SignerCertificate.Thumbprint)]" -NewLine
                $failed.Add($_.FullName, $signature)
            }
        } elseif (-not $unsignedAllowList.Contains($relativePath)) {
            Undo-Operation -operation "unsigned" -message "$relativePath" -NewLine
            $failed.Add($_.FullName, $null)
        }
    }

    if($failed.Count -eq 0) {
        Submit-Operation -operation "done" -message "$($files.Length) binaries passed signature checks."
    } elseif ($failed.Count -eq $files.Length) {
        Undo-Operation -operation "failed" -message "$($files.Length) binaries failed signature checks." 
    } else {
        Undo-Operation -operation "partial" -message "$($files.Length - $failed.Count) binaries passed signature checks and $($failed.Count) binaries failed!" 
    }
    Write-Host
    $failed
}

Clear-Host
Push-Location
try {
    if (-not (Test-Path $Path)) {
        Write-Error "Cannot find path '$Path' because it does not exist."
        exit
    }
    
    Set-Location $Path
    
    Write-Host "Checking NuGet packages..."
    $nugetFailCount = Test-NuGetSignatures 
    
    Write-Host "Extracting NuGet packages..."
    $failCount = 0
    Expand-NugetPackages | ForEach-Object {
        Push-Depth
        Write-Host "Checking `"$([System.IO.Path]::GetFileName($_))`"..."
        $failures = Test-Signatures -rootFolder $_ 
        Pop-Depth

        Remove-Item $_ -Recurse -Force
        $failCount += $failures.Count
    }
    if($failCount -eq 0) {
        Write-Host "All binaries passed the check successfully."
    } else {
        Write-Error "$failCount binaries failed signature check!"
    }
    
    if($nugetFailCount -eq 0) {
        Write-Host "All NuGet packages passed the check successfully."
    } else {
        Write-Error "$nugetFailCount NuGet packages failed signature check!"
    }
}
finally {
    Pop-Location
}