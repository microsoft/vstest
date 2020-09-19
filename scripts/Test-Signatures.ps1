[CmdletBinding()]
param (
    [Parameter()]
    [String]
    $Path = "$PSScriptRoot\packages",

    [Parameter()]
    [Switch]
    $CIBuild
)

Add-Type -AssemblyName System.IO.Compression.FileSystem

# Allowed certificate subjects
$global:ValidSubjects = @( `
    "CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US", `
    "CN=Microsoft Corporation, OU=MOPR, O=Microsoft Corporation, L=Redmond, S=Washington, C=US" `
);

# Allowed certificate thumbprints
$global:ValidThumbprints = @( `
    "B9EAA034C821C159B05D3521BCF7FEB796EBD6FF", `
    "98ED99A67886D020C564923B7DF25E9AC019DF26", `
    "5EAD300DC7E4D637948ECB0ED829A072BD152E17", `
    "67B1757863E3EFF760EA9EBB02849AF07D3A8080", `
    "9DC17888B5CFAD98B3CB35C1994E96227F061675", `
    "62009AAABDAE749FD47D19150958329BF6FF4B34", `
    "3BDA323E552DB1FDE5F4FBEE75D6D5B2B187EEDC" `
)

# Allow-list for unsigned binaries
$global:UnsignedAllowList = @{
    # "PackageId" = relative paths
    "Microsoft.TestPlatform" = @( `
        ".\tools\net451\Common7\IDE\Extensions\TestPlatform\Newtonsoft.Json.dll" `
    )

    "Microsoft.TestPlatform.CLI" = @( `
        ".\contentFiles\any\netcoreapp2.1\TestHost\Newtonsoft.Json.dll", `
        ".\contentFiles\any\netcoreapp2.1\Newtonsoft.Json.dll"
    )
    
    "Microsoft.TestPlatform.Portable" = @( `
        ".\tools\net451\Newtonsoft.Json.dll", `
        ".\tools\netcoreapp2.1\TestHost\Newtonsoft.Json.dll", `
        ".\tools\netcoreapp2.1\Newtonsoft.Json.dll"
    )
}


$global:Depth = 0;

function Get-RelativePath($path, $relativeTo) {
    Push-Location
    Set-Location $relativeTo
    $path = Resolve-Path $path -Relative
    Pop-Location
    
    return $path
}

function Push-Depth {
    $global:Depth = $global:Depth + 1
}

function Pop-Depth {
    $global:Depth = $global:Depth - 1

    if($global:Depth -lt 0) {
        $global:Depth = 0;
    }
}

function Clear-Line($message = "") {
    if(-not $CIBuild) {
        $x = [System.Console]::get_CursorLeft()
        Write-Host "`r$(' ' * $x)$message`r" -NoNewline
    }
}

function Update-Operation($operation, $message, [switch]$NewLine, $suppress=$false) {
    if($suppress) {
        return;
    }
    
    Clear-Line;
    Write-Host "$('  ' * $Depth)[$operation] " -ForegroundColor Yellow -NoNewline
    Write-Host $message -NoNewline

    if ($newline -or $CIBuild) {
        Write-Host;
    }
}

function Submit-Operation($operation, $message, [switch]$NoNewLine, $suppress=$false) {
    if($suppress) {
        return;
    }
    
    Clear-Line;
    Write-Host "$('  ' * $Depth)[$operation] " -ForegroundColor Green -NoNewline
    Write-Host $message -NoNewline

    if (-not $NoNewLine -or $CIBuild) {
        Write-Host;
    }
}

function Undo-Operation($operation, $message, [switch]$NoNewLine, $suppress=$false) {
    if($suppress) {
        return;
    }
    
    Clear-Line;
    Write-Host "$('  ' * $Depth)[$operation] " -ForegroundColor Red -NoNewline
    Write-Host $message -NoNewline

    if (-not $NoNewLine -or $CIBuild) {
        Write-Host;
    }
}

function Expand-NugetPackages {
    $foldersToProcess = [System.Collections.Generic.HashSet[string]]@()

    $nupgks = Get-ChildItem *.nupkg

    Push-Depth
    $nupgks | ForEach-Object {
        $package = $_.FullName
        $expand = Join-Path -Path ([System.IO.Path]::GetDirectoryName($package)) -ChildPath ([System.IO.Path]::GetFileNameWithoutExtension($package))
        ((Test-Path -Path $expand) -and (Remove-Item $expand -Force -Recurse)) | Out-Null

        Update-Operation -operation "extracting" -message "$([System.IO.Path]::GetFileName($package))"
        [System.IO.Compression.ZipFile]::ExtractToDirectory($package, $expand)
        Submit-Operation -operation "done" -message "$([System.IO.Path]::GetFileName($package))" -suppress $CIBuild

        $foldersToProcess.Add($expand) | Out-Null
    }
    Pop-Depth
    Write-Host;
    return $foldersToProcess
}

function Test-Signatures($rootFolder) {
    $fail = @{}

    Update-Operation -operation "discovering" -message "Searching binaries..."  -suppress $CIBuild
    $files = (Get-ChildItem $rootFolder -Recurse -Include "*.dll", "*.exe")
    Update-Operation -operation "discovered" -message "$($files.Length) binaries discovered"

    $packageId = ([XML](Get-Content $rootFolder\*.nuspec)).package.metadata.id;
    $unsignedAllowList = @();
    if ($global:UnsignedAllowList.ContainsKey($packageId)) {
        $unsignedAllowList = $global:UnsignedAllowList[$packageId];
    }

    $files | ForEach-Object {
        $relativePath = Get-RelativePath -path $_.FullName -relativeTo $rootFolder
        Update-Operation -operation "processing" -message $relativePath -suppress $CIBuild

        $signature = Get-AuthenticodeSignature -FilePath "$($_.FullName)"
        if ($signature.Status -eq "Valid") {
            $valid = ($ValidSubjects.Contains($signature.SignerCertificate.Subject) -or $ValidThumbprints.Contains($signature.SignerCertificate.Thumbprint))
            
            if ($valid) {
                Submit-Operation -operation "valid" -message "$relativePath" -NoNewLine -suppress $CIBuild
            } else {
                Update-Operation -operation "inconclusive" -message "$relativePath ($($signature.SignerCertificate.Subject)) [$($signature.SignerCertificate.Thumbprint)]" -NewLine
                $fail.Add($_.FullName, $signature)
            }
        } elseif (-not $unsignedAllowList.Contains($relativePath)) {
            Undo-Operation -operation "unsigned" -message "$relativePath" -NewLine
            $fail.Add($_.FullName, $null)
        }
    }

    if($fail.Count -eq 0) {
        Submit-Operation -operation "done" -message "$($files.Length) binaries passed signature checks."
    } elseif ($fail.Count -eq $files.Length) {
        Undo-Operation -operation "failed" -message "$($files.Length) binaries failed signature checks." 
    } else {
        Undo-Operation -operation "partial" -message "$($files.Length - $fail.Count) binaries passed signature checks and $($fail.Count) binaries failed!" 
    }
    Write-Host;
    $fail;
}


Clear-Host
Push-Location
Set-Location $Path
$failCount = 0
Write-Host "Extracting NuGet packages..."
Expand-NugetPackages | ForEach-Object {
    Write-Host "Checking `"$([System.IO.Path]::GetFileName($_))`"..."
    Push-Depth
    $failures = Test-Signatures -rootFolder $_ 
    Pop-Depth

    Remove-Item $_ -Recurse -Force
    $failCount += $failures.Count
}
Pop-Location
if($failCount -eq 0) {
    Write-Host "All binaries passed the check successfully."
} else {
    Write-Error "$failCount binaries failed signature check!"
}
