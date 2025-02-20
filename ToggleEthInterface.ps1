# Function to get the Ethernet interface by name
function Get-NetworkAdapter {
    param (
        [string]$InterfaceName
    )
    $adapter = Get-NetAdapter -Name $InterfaceName -ErrorAction SilentlyContinue
    return $adapter
}

# Function to disable the Ethernet interface
function DisableEthernetInterface($interfaceName) {
    $interface = Get-NetAdapter -Name $interfaceName -ErrorAction SilentlyContinue
    if ($interface) {
        Disable-NetAdapter -Name $interfaceName -Confirm:$false
        Write-Host "Ethernet interface '$interfaceName' is now DISABLED."
    } else {
        Write-Host "Ethernet interface '$interfaceName' not found."
    }
}

# Function to enable the Ethernet interface
function EnableEthernetInterface($interfaceName) {
    $interface = Get-NetAdapter -Name $interfaceName -ErrorAction SilentlyContinue
    if ($interface) {
        Enable-NetAdapter -Name $interfaceName -Confirm:$false
        Write-Host "Ethernet interface '$interfaceName' is now ENABLED."
    } else {
        Write-Host "Ethernet interface '$interfaceName' not found."
    }
}

# Ask the user for the Ethernet interface name
$interfaceName = Read-Host "Enter the Interface name (e.g., 'Ethernet')"

# Check if the interface exists
$adapter = Get-NetworkAdapter -InterfaceName $interfaceName
if ($adapter -eq $null) {
    Write-Host "Ethernet interface '$interfaceName' not found. Please enter a valid interface name."
    exit
}

# Loop to keep running and waiting for user input
while ($true) {
    Write-Host "`nOptions:"
    Write-Host "1 - Disable Ethernet Interface"
    Write-Host "2 - Enable Ethernet Interface"
    Write-Host "3 - Quit"

    $choice = Read-Host "Enter your choice (1-3)"

    switch ($choice) {
        "1" {
            DisableEthernetInterface($interfaceName)
        }
        "2" {
            EnableEthernetInterface($interfaceName)
        }
        "3" {
            Write-Host "Exiting script."
            exit
        }
        default {
            Write-Host "Invalid option. Please enter a number between 1 and 3."
        }
    }
}
