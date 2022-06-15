$ethernetAlias = (Get-NetAdapterHardwareInfo | Where-Object {$_.Slot -eq "0"} | Where-Object {$_.Name -Match "^E.*"}).Name
$ethernetIndex = (Get-NetAdapter | Where-Object {$_.Name -eq $ethernetAlias}).InterfaceIndex

Remove-NetIPAddress -InterfaceIndex $ethernetIndex -Confirm:$false
Remove-NetRoute -InterfaceIndex $ethernetIndex -Confirm:$false
Set-NetIPInterface -InterfaceIndex $ethernetIndex -Dhcp Disabled

$ipParameters = @{
InterfaceIndex = $ethernetIndex
IPAddress = "192.168.1.3"
PrefixLength = 24
AddressFamily = "IPv4"
DefaultGateway = "192.168.1.1"
}

New-NetIPAddress @ipParameters 

$dnsParameters = @{
InterfaceIndex = $ethernetIndex
ServerAddresses = ("8.8.8.8", "8.8.4.4")
}

Set-DnsClientServerAddress @dnsParameters

Restart-NetAdapter -InterfaceAlias $ethernetAlias