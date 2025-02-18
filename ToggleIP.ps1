# Function to check if a firewall rule exists
function RuleExists($ruleName) {
    $rule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
    return $rule -ne $null
}

# Function to get the Ethernet interface by name
function Get-NetworkAdapter {
    param (
        [string]$InterfaceName
    )
    $adapter = Get-NetAdapter -Name $InterfaceName -ErrorAction SilentlyContinue
    return $adapter
}

# Ask the user for the Ethernet interface name
$interfaceName = Read-Host "Enter the Ethernet interface name (e.g., 'Ethernet')"

# Check if the interface exists
$adapter = Get-NetworkAdapter -InterfaceName $interfaceName
if ($adapter -eq $null) {
    Write-Host "Ethernet interface '$interfaceName' not found. Please enter a valid interface name."
    exit
}

# Ask the user for the IP address
$ip = Read-Host "Enter the IP address to block or unblock"

# Validate IP format
if ($ip -notmatch "^(\d{1,3}\.){3}\d{1,3}$") {
    Write-Host "Invalid IP format. Please enter a valid IP address."
    exit
}

# Define rule names
$ruleNameOut = "Block Outbound Connection $ip on $interfaceName"
$ruleNameIn = "Block Inbound Connection $ip on $interfaceName"
$ruleNamePingOut = "Block Outbound Ping to $ip on $interfaceName"
$ruleNamePingIn = "Block Inbound Ping from $ip on $interfaceName"

# Loop to keep running and waiting for user input
while ($true) {
    Write-Host "`nOptions:"
    Write-Host "1 - Block IP ($ip) on Ethernet interface ($interfaceName)"
    Write-Host "2 - Unblock IP ($ip) on Ethernet interface ($interfaceName)"
    Write-Host "3 - Check status of IP on Ethernet interface"
    Write-Host "4 - Quit"

    $choice = Read-Host "Enter your choice (1-4)"

    switch ($choice) {
        "1" {
            if (RuleExists $ruleNameOut -or RuleExists $ruleNameIn -or RuleExists $ruleNamePingOut -or RuleExists $ruleNamePingIn) {
                Write-Host "The IP $ip is already BLOCKED on Ethernet interface '$interfaceName'."
            } else {
                Write-Host "Blocking $ip (Inbound & Outbound) on Ethernet interface '$interfaceName'..."
                try {
                    New-NetFirewallRule -DisplayName $ruleNameOut -Direction Outbound -Action Block -RemoteAddress $ip -Protocol Any -InterfaceAlias $interfaceName -Enabled True -ErrorAction Stop
                    New-NetFirewallRule -DisplayName $ruleNameIn -Direction Inbound -Action Block -RemoteAddress $ip -Protocol Any -InterfaceAlias $interfaceName -Enabled True -ErrorAction Stop
                    Write-Host "Successfully blocked IP $ip on interface '$interfaceName'."
                } catch {
                    Write-Host "Error blocking IP: $_"
                }
            }
        }
        "2" {
            if (RuleExists $ruleNameOut -or RuleExists $ruleNameIn -or RuleExists $ruleNamePingOut -or RuleExists $ruleNamePingIn) {
                Write-Host "Unblocking $ip on Ethernet interface '$interfaceName'..."
                try {
                    Remove-NetFirewallRule -DisplayName $ruleNameOut -ErrorAction Stop
                    Remove-NetFirewallRule -DisplayName $ruleNameIn -ErrorAction Stop
                    Write-Host "Successfully unblocked IP $ip on interface '$interfaceName'."
                } catch {
                    Write-Host "Error unblocking IP: $_"
                }
            } else {
                Write-Host "The IP $ip is already ALLOWED on Ethernet interface '$interfaceName'."
            }
        }
        "3" {
            if (RuleExists $ruleNameOut -or RuleExists $ruleNameIn -or RuleExists $ruleNamePingOut -or RuleExists $ruleNamePingIn) {
                Write-Host "The IP $ip is currently BLOCKED on Ethernet interface '$interfaceName'."
            } else {
                Write-Host "The IP $ip is currently ALLOWED on Ethernet interface '$interfaceName'."
            }
        }
        "4" {
            Write-Host "Exiting script."
            exit
        }
        default {
            Write-Host "Invalid option. Please enter a number between 1 and 4."
        }
    }
}
