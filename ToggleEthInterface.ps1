# Function to disable the Ethernet interface
function DisableEthernetInterface {
    $interfaceName = "Ethernet"  # Replace with the actual name of your Ethernet interface if different
    $interface = Get-NetAdapter -Name $interfaceName -ErrorAction SilentlyContinue
    if ($interface) {
        Disable-NetAdapter -Name $interfaceName -Confirm:$false
        Write-Host "Ethernet interface '$interfaceName' is now DISABLED."
    } else {
        Write-Host "Ethernet interface '$interfaceName' not found."
    }
}

# Function to enable the Ethernet interface
function EnableEthernetInterface {
    $interfaceName = "Ethernet"  # Replace with the actual name of your Ethernet interface if different
    $interface = Get-NetAdapter -Name $interfaceName -ErrorAction SilentlyContinue
    if ($interface) {
        Enable-NetAdapter -Name $interfaceName -Confirm:$false
        Write-Host "Ethernet interface '$interfaceName' is now ENABLED."
    } else {
        Write-Host "Ethernet interface '$interfaceName' not found."
    }
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
            DisableEthernetInterface
        }
        "2" {
            EnableEthernetInterface
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
