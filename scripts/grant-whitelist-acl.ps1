# NAV INDEX
# 1-5    header
# 6-16   grants Carlos_Ortiz full control on Openness Whitelist key + runs whitelist.ps1
# Run elevated once; afterwards whitelist.ps1 runs unprivileged.

$key = "HKLM:\SOFTWARE\Siemens\Automation\Openness\21.0\Whitelist"
$acl = Get-Acl $key
$rule = New-Object System.Security.AccessControl.RegistryAccessRule(
    "TITANXNEXUS\Carlos_Ortiz", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.AddAccessRule($rule)
Set-Acl $key $acl
& "c:\Scripts\TIA Portal\scripts\whitelist.ps1" | Out-File "c:\Scripts\TIA Portal\workspace\whitelist-check.txt"
