# Save this as UTF-8 WITHOUT BOM
# Run in project root: .\Export-LinuxTree.ps1

function Get-LinuxTree {
    param(
        [string]$Path = ".",
        [string]$Prefix = ""
    )
    
    $items = Get-ChildItem $Path | 
        Where-Object { $_.Name -notmatch '^(obj|bin|\.git)$' } | 
        Sort-Object Name
    
    for ($i = 0; $i -lt $items.Count; $i++) {
        $item = $items[$i]
        $isLast = ($i -eq $items.Count - 1)
        
        # PowerShell 5.1 compatible (no ternary operators)
        $connector = if ($isLast) { "└── " } else { "├── " }
        $relativePath = $item.FullName.Substring((Get-Item .).FullName.Length + 1) -replace '\\', '/'
        Write-Output "${Prefix}${connector}${relativePath}"
        
        if ($item.PSIsContainer) {
            $childPrefix = if ($isLast) { "    " } else { "│   " }
            Get-LinuxTree -Path $item.FullName -Prefix "${Prefix}${childPrefix}"
        }
    }
}

# Output header
Write-Output "PhantomExe.VM/"

# Generate tree
Get-LinuxTree