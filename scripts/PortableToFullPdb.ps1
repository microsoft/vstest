# Copyright (c) Microsoft. All rights reserved.
# Portable to Full PDB conversion script for Test Platform.

[CmdletBinding()]
Param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [System.String] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"

#
# Variables
#
Write-Verbose "Setup environment variables."
$TP_ROOT_DIR = (Get-Item (Split-Path $MyInvocation.MyCommand.Path)).Parent.FullName
$TP_PACKAGES_DIR = Join-Path $TP_ROOT_DIR "packages"
$TP_OUT_DIR = Join-Path $TP_ROOT_DIR "artifacts"

$PdbConverterToolVersion = "1.1.0-beta1-62316-01"

function Locate-PdbConverterTool
{
    $pdbConverter = Join-Path -path $TP_PACKAGES_DIR -ChildPath "Pdb2Pdb\$PdbConverterToolVersion\tools\Pdb2Pdb.exe"

       if (!(Test-Path -path $pdbConverter)) {
       throw "Unable to locate Pdb2Pdb converter exe in path '$pdbConverter'."
   }

    Write-Verbose "Pdb2Pdb converter path is : $pdbConverter"
    return $pdbConverter

}

function ConvertPortablePdbToWindowsPdb
{
    $portablePdbs = Get-ChildItem -path $TP_OUT_DIR\$Configuration *.pdb -Recurse | % {$_.FullName}
    $pdbConverter = Locate-PdbConverterTool

    #$portablePdbs = Get-ChildItem -path C:\Users\jagarg\Desktop\TestPlatform\TestPlatform\release *.pdb -Recurse | % {$_.FullName}
    #$pdbConverter = "C:\Users\jagarg\Desktop\Pdb2Pdb.1.1.0-beta1-62316-01\tools\Pdb2Pdb.exe"

    
    foreach($portablePdb in $portablePdbs)
    {
        $dll = $portablePdb -replace ".pdb",".dll"
        $fullpdb = $portablePdb -replace ".pdb",".pdbfull"

        Write-Verbose "$pdbConverter $dll /pdb $portablePdb /out $fullpdb"
        & $pdbConverter $dll /pdb $portablePdb /out $fullpdb
    }
}

Write-Verbose "Converting Portable pdbs to Windows(Full) Pdbs"
ConvertPortablePdbToWindowsPdb

