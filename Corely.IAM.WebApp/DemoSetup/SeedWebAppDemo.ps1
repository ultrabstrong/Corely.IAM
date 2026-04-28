[CmdletBinding()]
param(
    [switch]$Resume,
    [string]$SeedRoot = (Join-Path $env:TEMP 'corely-webapp-demo'),
    [string]$DeviceId = 'webapp-demo-seed'
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$seedRoot = $SeedRoot
$requestsDir = Join-Path $seedRoot 'requests'
$resultsDir = Join-Path $seedRoot 'results'
$authTokenFile = Join-Path $env:USERPROFILE 'Corely\corely-iam-auth-token.json'
$statePath = Join-Path $resultsDir 'state.json'

if ((-not $Resume) -and (Test-Path $seedRoot)) {
    Remove-Item -Path $seedRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $requestsDir | Out-Null
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

if ($Resume) {
    if (-not (Test-Path $statePath)) {
        throw "Cannot resume seed without existing state at $statePath"
    }

    $state = Get-Content -Path $statePath -Raw | ConvertFrom-Json -AsHashtable
}
else {
    $state = [ordered]@{
        SeedRoot = $seedRoot
        Users = [ordered]@{}
        Accounts = [ordered]@{}
        Validation = [ordered]@{}
    }
}

function Save-State {
    $state | ConvertTo-Json -Depth 100 | Set-Content -Path $statePath
}

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Test-MapContainsKey {
    param(
        [object]$Map,
        [string]$Key
    )

    if ($null -eq $Map) {
        return $false
    }

    return @($Map.Keys) -contains $Key
}

function Write-RequestArray {
    param(
        [string]$FileName,
        [object]$Payload
    )

    $path = Join-Path $requestsDir $FileName
    @($Payload) | ConvertTo-Json -Depth 100 -AsArray | Set-Content -Path $path
    return $path
}

function Write-RequestObject {
    param(
        [string]$FileName,
        [object]$Payload
    )

    $path = Join-Path $requestsDir $FileName
    $Payload | ConvertTo-Json -Depth 100 | Set-Content -Path $path
    return $path
}

function Invoke-CorelyText {
    param(
        [string]$Label,
        [string[]]$Arguments
    )

    Write-Host "==> corely $($Arguments -join ' ')"
    $output = & corely @Arguments 2>&1
    $text = ($output | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine
    Set-Content -Path (Join-Path $resultsDir "$Label.txt") -Value $text

    if ($LASTEXITCODE -ne 0) {
        throw "corely command failed for $Label`n$text"
    }

    return $text
}

function Invoke-CorelyJson {
    param(
        [string]$Label,
        [string[]]$Arguments
    )

    $text = Invoke-CorelyText -Label $Label -Arguments $Arguments
    $trimmedText = $text.Trim()
    $arrayStart = $trimmedText.IndexOf('[')
    $objectStart = $trimmedText.IndexOf('{')

    if ($arrayStart -ge 0 -and ($objectStart -lt 0 -or $arrayStart -lt $objectStart)) {
        $jsonStart = $arrayStart
        $jsonEnd = $trimmedText.LastIndexOf(']')
    }
    else {
        $jsonStart = $objectStart
        $jsonEnd = $trimmedText.LastIndexOf('}')
    }

    if ($jsonStart -lt 0 -or $jsonEnd -lt $jsonStart) {
        throw "No JSON output found for $Label`n$text"
    }

    $jsonPayload = $trimmedText.Substring($jsonStart, ($jsonEnd - $jsonStart) + 1)

    Set-Content -Path (Join-Path $resultsDir "$Label.json") -Value $jsonPayload
    return $jsonPayload | ConvertFrom-Json -Depth 100
}

function Invoke-CorelyJsonLines {
    param(
        [string]$Label,
        [string[]]$Arguments
    )

    $text = Invoke-CorelyText -Label $Label -Arguments $Arguments
    $jsonLines = @(
        $text -split "`r?`n" |
            ForEach-Object { $_.Trim() } |
            Where-Object { $_.StartsWith('{') -and $_.EndsWith('}') }
    )

    if ($jsonLines.Count -eq 0) {
        throw "No JSON lines found for $Label`n$text"
    }

    Set-Content -Path (Join-Path $resultsDir "$Label.jsonl") -Value ($jsonLines -join [Environment]::NewLine)
    return @($jsonLines | ForEach-Object { $_ | ConvertFrom-Json -Depth 100 })
}

function Assert-AllowedResultCode {
    param(
        [int]$ResultCode,
        [int[]]$AllowedCodes,
        [string]$Context
    )

    if ($AllowedCodes -contains $ResultCode) {
        return
    }

    throw "$Context failed with ResultCode=$ResultCode"
}

function Assert-BatchRegistrationResult {
    param(
        [object]$Result,
        [string]$CountProperty,
        [string]$InvalidItemsProperty,
        [int]$ExpectedCount,
        [string]$Context
    )

    $registeredCount = [int]($Result.$CountProperty ?? 0)
    $invalidCount = @($Result.$InvalidItemsProperty).Count

    if ($registeredCount -eq $ExpectedCount) {
        return
    }

    if ($Resume -and (($registeredCount + $invalidCount) -eq $ExpectedCount)) {
        return
    }

    throw "$Context did not register the expected item count. Registered=$registeredCount Invalid=$invalidCount Expected=$ExpectedCount"
}

function Get-NonEmptyGuid {
    param(
        [object]$Value,
        [string]$Context
    )

    $guid = [guid]$Value
    if ($guid -eq [guid]::Empty) {
        throw "$Context returned Guid.Empty"
    }

    return $guid
}

function Sign-InUser {
    param(
        [string]$Username,
        [string]$AccountId
    )

    $payload = [ordered]@{
        Username = $Username
        Password = 'admin'
        DeviceId = $DeviceId
    }

    $suffix = ''
    if (-not [string]::IsNullOrWhiteSpace($AccountId)) {
        $payload.AccountId = $AccountId
        $suffix = "-$AccountId"
    }

    $safeUserName = $Username.Replace('.', '-')
    $requestPath = Write-RequestObject -FileName "signin-$safeUserName$suffix.json" -Payload $payload
    $text = Invoke-CorelyText -Label "signin-$safeUserName$suffix" -Arguments @('auth', 'signin', $requestPath)

    Assert-True -Condition ($text -match 'Sign in successful') -Message "Sign in failed for $Username"
    Assert-True -Condition (Test-Path $authTokenFile) -Message "Auth token file not found after sign in for $Username"

    $token = Get-Content -Path $authTokenFile -Raw | ConvertFrom-Json -Depth 20
    Assert-True -Condition (-not [string]::IsNullOrWhiteSpace($token.AuthToken)) -Message "Auth token missing after sign in for $Username"
}

function Sign-OutCurrentUser {
    param([string]$Label)

    $result = Invoke-CorelyJson -Label $Label -Arguments @('auth', 'signout-all')
    Assert-True -Condition ($result.Success -eq $true) -Message "Sign out all failed for $Label"
}

function New-PermissionSpec {
    param(
        [string]$Description,
        [string]$ResourceType,
        [string]$Action
    )

    $spec = [ordered]@{
        Description = $Description
        ResourceType = $ResourceType
        ResourceId = '00000000-0000-0000-0000-000000000000'
        Create = $false
        Read = $false
        Update = $false
        Delete = $false
        Execute = $false
    }

    switch ($Action) {
        'create' { $spec.Create = $true }
        'read' { $spec.Read = $true }
        'update' { $spec.Update = $true }
        'delete' { $spec.Delete = $true }
        'execute' { $spec.Execute = $true }
        default { throw "Unknown permission action: $Action" }
    }

    return $spec
}

$bootstrapUsers = @(
    @{ Username = 'admin'; Email = 'admin@demo.local' }
)

$users = @(
    @{ Username = 'alice.johnson'; Email = 'alice.johnson@demo.local' }
    @{ Username = 'ben.carter'; Email = 'ben.carter@demo.local' }
    @{ Username = 'cara.nguyen'; Email = 'cara.nguyen@demo.local' }
    @{ Username = 'daniel.lee'; Email = 'daniel.lee@demo.local' }
    @{ Username = 'elena.morris'; Email = 'elena.morris@demo.local' }
    @{ Username = 'farah.khan'; Email = 'farah.khan@demo.local' }
    @{ Username = 'gavin.brooks'; Email = 'gavin.brooks@demo.local' }
    @{ Username = 'hannah.price'; Email = 'hannah.price@demo.local' }
    @{ Username = 'isaac.reed'; Email = 'isaac.reed@demo.local' }
    @{ Username = 'julia.woods'; Email = 'julia.woods@demo.local' }
    @{ Username = 'kevin.bell'; Email = 'kevin.bell@demo.local' }
    @{ Username = 'leo.turner'; Email = 'leo.turner@demo.local' }
    @{ Username = 'maya.shah'; Email = 'maya.shah@demo.local' }
    @{ Username = 'nolan.gray'; Email = 'nolan.gray@demo.local' }
    @{ Username = 'olivia.ward'; Email = 'olivia.ward@demo.local' }
    @{ Username = 'paula.ramos'; Email = 'paula.ramos@demo.local' }
    @{ Username = 'quinn.hughes'; Email = 'quinn.hughes@demo.local' }
    @{ Username = 'rafael.gomez'; Email = 'rafael.gomez@demo.local' }
    @{ Username = 'sara.kim'; Email = 'sara.kim@demo.local' }
    @{ Username = 'tyler.adams'; Email = 'tyler.adams@demo.local' }
    @{ Username = 'uma.patel'; Email = 'uma.patel@demo.local' }
    @{ Username = 'victor.ross'; Email = 'victor.ross@demo.local' }
    @{ Username = 'willa.ford'; Email = 'willa.ford@demo.local' }
    @{ Username = 'xavier.young'; Email = 'xavier.young@demo.local' }
    @{ Username = 'yasmine.ali'; Email = 'yasmine.ali@demo.local' }
    @{ Username = 'zane.cooper'; Email = 'zane.cooper@demo.local' }
    @{ Username = 'seasonal.01'; Email = 'seasonal.01@demo.local' }
    @{ Username = 'seasonal.02'; Email = 'seasonal.02@demo.local' }
    @{ Username = 'seasonal.03'; Email = 'seasonal.03@demo.local' }
    @{ Username = 'seasonal.04'; Email = 'seasonal.04@demo.local' }
)

$naPermissions = @()
foreach ($resourceType in @('account', 'user', 'group', 'role', 'permission')) {
    foreach ($action in @('create', 'read', 'update', 'delete', 'execute')) {
        $naPermissions += New-PermissionSpec -Description "$resourceType.$action" -ResourceType $resourceType -Action $action
    }
}
$naPermissions += New-PermissionSpec -Description 'all.read' -ResourceType '*' -Action 'read'
$naPermissions += New-PermissionSpec -Description 'all.execute' -ResourceType '*' -Action 'execute'

$accounts = @(
    [ordered]@{
        Key = 'na'
        Name = 'Acme Retail - North America'
        Owner = 'alice.johnson'
        Members = $users.Username
        CustomRoles = @(
            'Account Admin',
            'Operations Manager',
            'Support Agent',
            'Billing Manager',
            'Fulfillment Lead',
            'Content Editor',
            'Read Only Analyst',
            'Security Auditor',
            'Contractor Limited'
        )
        Groups = @(
            'Leadership',
            'Operations',
            'Support',
            'Finance',
            'Fulfillment',
            'Analytics',
            'Contractors',
            'All Hands'
        )
        Permissions = $naPermissions
        RolePermissions = [ordered]@{
            'Account Admin' = $naPermissions.Description
            'Operations Manager' = @('account.read', 'user.read', 'user.update', 'group.read', 'group.update', 'role.read', 'permission.read')
            'Support Agent' = @('account.read', 'user.read', 'user.update', 'group.read', 'permission.read')
            'Billing Manager' = @('account.read', 'account.update', 'user.read', 'permission.read', 'permission.update')
            'Fulfillment Lead' = @('account.read', 'user.read', 'user.update', 'group.read', 'group.update')
            'Content Editor' = @('account.read', 'group.read', 'role.read', 'role.update', 'permission.read', 'permission.update')
            'Read Only Analyst' = @('account.read', 'user.read', 'group.read', 'role.read', 'permission.read', 'all.read')
            'Security Auditor' = @('account.read', 'user.read', 'group.read', 'role.read', 'permission.read', 'all.read', 'all.execute')
            'Contractor Limited' = @('account.read', 'user.read', 'group.read')
        }
        GroupRoles = [ordered]@{
            'Leadership' = @('Owner Role', 'Account Admin')
            'Operations' = @('Operations Manager', 'Content Editor')
            'Support' = @('Support Agent')
            'Finance' = @('Billing Manager')
            'Fulfillment' = @('Fulfillment Lead')
            'Analytics' = @('Read Only Analyst', 'Security Auditor')
            'Contractors' = @('Contractor Limited')
            'All Hands' = @('Read Only Analyst')
        }
        DirectUserRoles = [ordered]@{
            'ben.carter' = @('Owner Role')
            'gavin.brooks' = @('Account Admin')
            'hannah.price' = @('Account Admin')
        }
        GroupUsers = [ordered]@{
            'Leadership' = @('alice.johnson', 'farah.khan')
            'Operations' = @('gavin.brooks', 'maya.shah', 'victor.ross')
            'Support' = @('hannah.price', 'isaac.reed', 'tyler.adams')
            'Finance' = @('paula.ramos', 'willa.ford')
            'Fulfillment' = @('leo.turner', 'olivia.ward', 'zane.cooper')
            'Analytics' = @('julia.woods', 'quinn.hughes', 'sara.kim')
            'Contractors' = @('seasonal.01', 'seasonal.02', 'seasonal.03', 'seasonal.04')
            'All Hands' = $users.Username
        }
        ExpectedMembers = 30
        ExpectedRoles = 10
        ExpectedGroups = 8
        ExpectedPermissions = 28
    }
    [ordered]@{
        Key = 'eu'
        Name = 'Acme Retail - Europe'
        Owner = 'ben.carter'
        Members = @('ben.carter', 'alice.johnson', 'farah.khan', 'gavin.brooks', 'hannah.price', 'isaac.reed', 'quinn.hughes', 'sara.kim', 'tyler.adams')
        CustomRoles = @('Account Admin', 'Support Lead', 'Finance Reviewer', 'Regional Editor')
        Groups = @('Leadership', 'Support', 'Finance')
        Permissions = @(
            New-PermissionSpec -Description 'account.read' -ResourceType 'account' -Action 'read'
            New-PermissionSpec -Description 'account.update' -ResourceType 'account' -Action 'update'
            New-PermissionSpec -Description 'user.read' -ResourceType 'user' -Action 'read'
            New-PermissionSpec -Description 'user.update' -ResourceType 'user' -Action 'update'
            New-PermissionSpec -Description 'group.read' -ResourceType 'group' -Action 'read'
            New-PermissionSpec -Description 'role.read' -ResourceType 'role' -Action 'read'
            New-PermissionSpec -Description 'permission.read' -ResourceType 'permission' -Action 'read'
            New-PermissionSpec -Description 'permission.update' -ResourceType 'permission' -Action 'update'
        )
        RolePermissions = [ordered]@{
            'Account Admin' = @('account.read', 'account.update', 'user.read', 'user.update', 'group.read', 'role.read', 'permission.read', 'permission.update')
            'Support Lead' = @('account.read', 'user.read', 'user.update', 'group.read', 'role.read')
            'Finance Reviewer' = @('account.read', 'user.read', 'role.read', 'permission.read')
            'Regional Editor' = @('account.read', 'user.read', 'permission.read', 'permission.update')
        }
        GroupRoles = [ordered]@{
            'Leadership' = @('Account Admin')
            'Support' = @('Support Lead')
            'Finance' = @('Finance Reviewer')
        }
        DirectUserRoles = [ordered]@{
            'isaac.reed' = @('Owner Role')
            'farah.khan' = @('Account Admin')
        }
        GroupUsers = [ordered]@{
            'Leadership' = @('ben.carter', 'alice.johnson')
            'Support' = @('gavin.brooks', 'hannah.price', 'tyler.adams')
            'Finance' = @('farah.khan', 'sara.kim')
        }
        ExpectedMembers = 9
        ExpectedRoles = 5
        ExpectedGroups = 3
        ExpectedPermissions = 9
    }
    [ordered]@{
        Key = 'contoso'
        Name = 'Contoso Manufacturing'
        Owner = 'cara.nguyen'
        Members = @('cara.nguyen', 'alice.johnson', 'julia.woods', 'kevin.bell', 'leo.turner', 'maya.shah', 'uma.patel', 'victor.ross')
        CustomRoles = @('Account Admin', 'Plant Supervisor', 'QA Reviewer', 'Read Only Analyst')
        Groups = @('Leadership', 'Operations', 'Quality')
        Permissions = @(
            New-PermissionSpec -Description 'account.read' -ResourceType 'account' -Action 'read'
            New-PermissionSpec -Description 'user.read' -ResourceType 'user' -Action 'read'
            New-PermissionSpec -Description 'user.update' -ResourceType 'user' -Action 'update'
            New-PermissionSpec -Description 'group.read' -ResourceType 'group' -Action 'read'
            New-PermissionSpec -Description 'role.read' -ResourceType 'role' -Action 'read'
            New-PermissionSpec -Description 'permission.read' -ResourceType 'permission' -Action 'read'
            New-PermissionSpec -Description 'all.read' -ResourceType '*' -Action 'read'
        )
        RolePermissions = [ordered]@{
            'Account Admin' = @('account.read', 'user.read', 'user.update', 'group.read', 'role.read', 'permission.read', 'all.read')
            'Plant Supervisor' = @('account.read', 'user.read', 'user.update', 'group.read')
            'QA Reviewer' = @('user.read', 'group.read', 'permission.read', 'all.read')
            'Read Only Analyst' = @('account.read', 'user.read', 'group.read', 'role.read', 'permission.read', 'all.read')
        }
        GroupRoles = [ordered]@{
            'Leadership' = @('Account Admin')
            'Operations' = @('Plant Supervisor')
            'Quality' = @('QA Reviewer', 'Read Only Analyst')
        }
        DirectUserRoles = [ordered]@{
            'julia.woods' = @('Account Admin')
            'kevin.bell' = @('Account Admin')
        }
        GroupUsers = [ordered]@{
            'Leadership' = @('cara.nguyen', 'alice.johnson')
            'Operations' = @('leo.turner', 'maya.shah', 'uma.patel')
            'Quality' = @('julia.woods', 'kevin.bell', 'victor.ross')
        }
        ExpectedMembers = 8
        ExpectedRoles = 5
        ExpectedGroups = 3
        ExpectedPermissions = 8
    }
    [ordered]@{
        Key = 'fabrikam'
        Name = 'Fabrikam Professional Services'
        Owner = 'daniel.lee'
        Members = @('daniel.lee', 'alice.johnson', 'nolan.gray', 'olivia.ward', 'paula.ramos', 'zane.cooper')
        CustomRoles = @('Account Admin', 'Project Lead', 'Read Only Analyst')
        Groups = @('Leadership', 'Delivery')
        Permissions = @(
            New-PermissionSpec -Description 'account.read' -ResourceType 'account' -Action 'read'
            New-PermissionSpec -Description 'user.read' -ResourceType 'user' -Action 'read'
            New-PermissionSpec -Description 'user.update' -ResourceType 'user' -Action 'update'
            New-PermissionSpec -Description 'group.read' -ResourceType 'group' -Action 'read'
            New-PermissionSpec -Description 'role.read' -ResourceType 'role' -Action 'read'
            New-PermissionSpec -Description 'permission.read' -ResourceType 'permission' -Action 'read'
        )
        RolePermissions = [ordered]@{
            'Account Admin' = @('account.read', 'user.read', 'user.update', 'group.read', 'role.read', 'permission.read')
            'Project Lead' = @('account.read', 'user.read', 'user.update', 'group.read')
            'Read Only Analyst' = @('account.read', 'user.read', 'group.read', 'role.read', 'permission.read')
        }
        GroupRoles = [ordered]@{
            'Leadership' = @('Account Admin')
            'Delivery' = @('Project Lead')
        }
        DirectUserRoles = [ordered]@{
            'paula.ramos' = @('Read Only Analyst')
        }
        GroupUsers = [ordered]@{
            'Leadership' = @('daniel.lee', 'alice.johnson')
            'Delivery' = @('nolan.gray', 'olivia.ward', 'zane.cooper')
        }
        ExpectedMembers = 6
        ExpectedRoles = 4
        ExpectedGroups = 2
        ExpectedPermissions = 7
    }
    [ordered]@{
        Key = 'northwind'
        Name = 'Northwind Internal IT'
        Owner = 'elena.morris'
        Members = @('elena.morris', 'alice.johnson', 'quinn.hughes', 'rafael.gomez', 'sara.kim')
        CustomRoles = @('Account Admin', 'Helpdesk', 'Read Only Analyst')
        Groups = @('Leadership', 'Support')
        Permissions = @(
            New-PermissionSpec -Description 'account.read' -ResourceType 'account' -Action 'read'
            New-PermissionSpec -Description 'user.read' -ResourceType 'user' -Action 'read'
            New-PermissionSpec -Description 'user.update' -ResourceType 'user' -Action 'update'
            New-PermissionSpec -Description 'group.read' -ResourceType 'group' -Action 'read'
            New-PermissionSpec -Description 'role.read' -ResourceType 'role' -Action 'read'
            New-PermissionSpec -Description 'permission.read' -ResourceType 'permission' -Action 'read'
        )
        RolePermissions = [ordered]@{
            'Account Admin' = @('account.read', 'user.read', 'user.update', 'group.read', 'role.read', 'permission.read')
            'Helpdesk' = @('user.read', 'user.update', 'group.read')
            'Read Only Analyst' = @('account.read', 'user.read', 'group.read', 'role.read', 'permission.read')
        }
        GroupRoles = [ordered]@{
            'Leadership' = @('Account Admin')
            'Support' = @('Helpdesk')
        }
        DirectUserRoles = [ordered]@{
            'sara.kim' = @('Read Only Analyst')
        }
        GroupUsers = [ordered]@{
            'Leadership' = @('elena.morris', 'alice.johnson')
            'Support' = @('quinn.hughes', 'rafael.gomez')
        }
        ExpectedMembers = 5
        ExpectedRoles = 4
        ExpectedGroups = 2
        ExpectedPermissions = 7
    }
)

Write-Host '==> Preflight'
Invoke-CorelyText -Label 'config-show' -Arguments @('config', 'show') | Out-Null
Invoke-CorelyText -Label 'config-test-connection' -Arguments @('config', 'test-connection') | Out-Null

Write-Host '==> Creating admin and demo users'
if (-not $Resume) {
    $pendingUsers = @($bootstrapUsers + $users)
}
else {
    $pendingUsers = @(
        ($bootstrapUsers + $users) | Where-Object {
            -not (Test-MapContainsKey -Map $state.Users -Key $_.Username)
        }
    )
}

if ($pendingUsers.Count -gt 0) {
    $userRequests = @(
        $pendingUsers | ForEach-Object {
            [ordered]@{
                Username = $_.Username
                Email = $_.Email
                Password = 'admin'
            }
        }
    )

    $requestPath = Write-RequestArray -FileName 'register-users-batch.json' -Payload $userRequests
    $results = Invoke-CorelyJsonLines -Label 'register-users-batch' -Arguments @('register', 'user', $requestPath)
    Assert-True -Condition ($results.Count -eq $pendingUsers.Count) -Message 'User batch result count mismatch'

    for ($i = 0; $i -lt $pendingUsers.Count; $i++) {
        $result = $results[$i]
        $user = $pendingUsers[$i]
        Assert-AllowedResultCode -ResultCode ([int]$result.ResultCode) -AllowedCodes @(0) -Context "Register user $($user.Username)"
        $createdUserId = Get-NonEmptyGuid -Value $result.CreatedUserId -Context "Register user $($user.Username)"
        $state.Users[$user.Username] = [string]$createdUserId
    }

    Save-State
}

Write-Host '==> Creating accounts'
foreach ($account in $accounts) {
    if ((Test-MapContainsKey -Map $state.Accounts -Key $account.Key) -and -not [string]::IsNullOrWhiteSpace($state.Accounts[$account.Key].AccountId)) {
        continue
    }

    Sign-InUser -Username $account.Owner -AccountId $null

    $requestPath = Write-RequestArray -FileName "register-account-$($account.Key).json" -Payload ([ordered]@{
            AccountName = $account.Name
            OwnerUserId = $state.Users[$account.Owner]
        })
    $result = Invoke-CorelyJson -Label "register-account-$($account.Key)" -Arguments @('register', 'account', $requestPath)
    $createdAccountId = Get-NonEmptyGuid -Value $result.CreatedAccountId -Context "Register account $($account.Name)"

    $state.Accounts[$account.Key] = [ordered]@{
        Name = $account.Name
        Owner = $account.Owner
        AccountId = [string]$createdAccountId
        Roles = [ordered]@{}
        Groups = [ordered]@{}
        Permissions = [ordered]@{}
    }

    Save-State
    Sign-OutCurrentUser -Label "signout-create-account-$($account.Key)"
}

Write-Host '==> Building account surfaces'
foreach ($account in $accounts) {
    $accountState = $state.Accounts[$account.Key]
    $accountId = $accountState.AccountId
    $membersAdded = ((Test-MapContainsKey -Map $accountState -Key 'MembersAdded') -and $accountState.MembersAdded) -or ($accountState.Roles.Count -gt 0) -or ($accountState.Groups.Count -gt 0) -or ($accountState.Permissions.Count -gt 0)
    $rolesCreated = ((Test-MapContainsKey -Map $accountState -Key 'RolesCreated') -and $accountState.RolesCreated) -or ($accountState.Roles.Count -gt 0)
    $groupsCreated = ((Test-MapContainsKey -Map $accountState -Key 'GroupsCreated') -and $accountState.GroupsCreated) -or ($accountState.Groups.Count -gt 0)
    $permissionsCreated = ((Test-MapContainsKey -Map $accountState -Key 'PermissionsCreated') -and $accountState.PermissionsCreated) -or ($accountState.Permissions.Count -gt 0)
    $assignmentsComplete = (Test-MapContainsKey -Map $accountState -Key 'AssignmentsComplete') -and $accountState.AssignmentsComplete

    Sign-InUser -Username $account.Owner -AccountId $accountId

    if (-not $membersAdded) {
        $membershipUserNames = @($account.Members | Where-Object { $_ -ne $account.Owner })
        if ($membershipUserNames.Count -gt 0) {
            $membershipRequests = @(
                $membershipUserNames | ForEach-Object {
                    [ordered]@{
                        UserId = $state.Users[$_]
                        AccountId = $accountId
                    }
                }
            )
            $requestPath = Write-RequestArray -FileName "register-user-with-account-$($account.Key)-batch.json" -Payload $membershipRequests
            $results = Invoke-CorelyJsonLines -Label "register-user-with-account-$($account.Key)-batch" -Arguments @('register', 'user-with-account', $requestPath)
            Assert-True -Condition ($results.Count -eq $membershipUserNames.Count) -Message "Membership result count mismatch for $($account.Name)"

            for ($i = 0; $i -lt $membershipUserNames.Count; $i++) {
                Assert-AllowedResultCode -ResultCode ([int]$results[$i].ResultCode) -AllowedCodes @(0, 3) -Context "Add $($membershipUserNames[$i]) to $($account.Name)"
            }
        }

        $accountState['MembersAdded'] = $true
        Save-State
    }

    if (-not $rolesCreated) {
        $roleRequests = @(
            $account.CustomRoles | ForEach-Object {
                [ordered]@{
                    RoleName = $_
                    AccountId = $accountId
                }
            }
        )
        $requestPath = Write-RequestArray -FileName "register-role-$($account.Key)-batch.json" -Payload $roleRequests
        $results = Invoke-CorelyJsonLines -Label "register-role-$($account.Key)-batch" -Arguments @('register', 'role', $requestPath)
        Assert-True -Condition ($results.Count -eq $account.CustomRoles.Count) -Message "Role result count mismatch for $($account.Name)"

        for ($i = 0; $i -lt $account.CustomRoles.Count; $i++) {
            $roleName = $account.CustomRoles[$i]
            $result = $results[$i]
            Assert-AllowedResultCode -ResultCode ([int]$result.ResultCode) -AllowedCodes @(0) -Context "Register role $roleName for $($account.Name)"
            $createdRoleId = Get-NonEmptyGuid -Value $result.CreatedRoleId -Context "Register role $roleName for $($account.Name)"
            $accountState.Roles[$roleName] = [string]$createdRoleId
        }

        $accountState['RolesCreated'] = $true
        Save-State
    }

    if (-not $groupsCreated) {
        $groupRequests = @(
            $account.Groups | ForEach-Object {
                [ordered]@{
                    GroupName = $_
                    AccountId = $accountId
                }
            }
        )
        $requestPath = Write-RequestArray -FileName "register-group-$($account.Key)-batch.json" -Payload $groupRequests
        $results = Invoke-CorelyJsonLines -Label "register-group-$($account.Key)-batch" -Arguments @('register', 'group', $requestPath)
        Assert-True -Condition ($results.Count -eq $account.Groups.Count) -Message "Group result count mismatch for $($account.Name)"

        for ($i = 0; $i -lt $account.Groups.Count; $i++) {
            $groupName = $account.Groups[$i]
            $result = $results[$i]
            Assert-AllowedResultCode -ResultCode ([int]$result.ResultCode) -AllowedCodes @(0) -Context "Register group $groupName for $($account.Name)"
            $createdGroupId = Get-NonEmptyGuid -Value $result.CreatedGroupId -Context "Register group $groupName for $($account.Name)"
            $accountState.Groups[$groupName] = [string]$createdGroupId
        }

        $accountState['GroupsCreated'] = $true
        Save-State
    }

    if (-not $permissionsCreated) {
        $permissionRequests = @(
            $account.Permissions | ForEach-Object {
                [ordered]@{
                    AccountId = $accountId
                    ResourceType = $_.ResourceType
                    ResourceId = $_.ResourceId
                    Create = $_.Create
                    Read = $_.Read
                    Update = $_.Update
                    Delete = $_.Delete
                    Execute = $_.Execute
                    Description = $_.Description
                }
            }
        )
        $requestPath = Write-RequestArray -FileName "register-permission-$($account.Key)-batch.json" -Payload $permissionRequests
        $results = Invoke-CorelyJsonLines -Label "register-permission-$($account.Key)-batch" -Arguments @('register', 'permission', $requestPath)
        Assert-True -Condition ($results.Count -eq $account.Permissions.Count) -Message "Permission result count mismatch for $($account.Name)"

        for ($i = 0; $i -lt $account.Permissions.Count; $i++) {
            $permissionSpec = $account.Permissions[$i]
            $result = $results[$i]
            Assert-AllowedResultCode -ResultCode ([int]$result.ResultCode) -AllowedCodes @(0) -Context "Register permission $($permissionSpec.Description) for $($account.Name)"
            $createdPermissionId = Get-NonEmptyGuid -Value $result.CreatedPermissionId -Context "Register permission $($permissionSpec.Description) for $($account.Name)"
            $accountState.Permissions[$permissionSpec.Description] = [string]$createdPermissionId
        }

        $accountState['PermissionsCreated'] = $true
        Save-State
    }

    if (-not (Test-MapContainsKey -Map $accountState.Roles -Key 'Owner Role')) {
        $listedRoles = Invoke-CorelyJson -Label "list-roles-$($account.Key)" -Arguments @('retrieval', 'list-roles', '-a', $accountId, '-s', '0', '-t', '50')
        Assert-True -Condition ($listedRoles.ResultCode -eq 0) -Message "Failed to list roles for $($account.Name)"
        $ownerRole = $listedRoles.Data.Items | Where-Object { $_.Name -eq 'Owner Role' -and $_.IsSystemDefined -eq $true } | Select-Object -First 1
        Assert-True -Condition ($null -ne $ownerRole) -Message "Owner Role not found for $($account.Name)"
        $accountState.Roles['Owner Role'] = [string]$ownerRole.Id
        Save-State
    }

    if (-not $assignmentsComplete) {
        $rolePermissionNames = @($account.RolePermissions.Keys)
        if ($rolePermissionNames.Count -gt 0) {
            $permissionAssignmentRequests = @(
                $rolePermissionNames | ForEach-Object {
                    $roleName = $_
                    [ordered]@{
                        PermissionIds = @($account.RolePermissions[$roleName] | ForEach-Object { $accountState.Permissions[$_] })
                        RoleId = $accountState.Roles[$roleName]
                        AccountId = $accountId
                    }
                }
            )
            $requestPath = Write-RequestArray -FileName "register-permissions-with-role-$($account.Key)-batch.json" -Payload $permissionAssignmentRequests
            $results = Invoke-CorelyJsonLines -Label "register-permissions-with-role-$($account.Key)-batch" -Arguments @('register', 'permissions-with-role', $requestPath)
            Assert-True -Condition ($results.Count -eq $rolePermissionNames.Count) -Message "Permission assignment result count mismatch for $($account.Name)"

            for ($i = 0; $i -lt $rolePermissionNames.Count; $i++) {
                $roleName = $rolePermissionNames[$i]
                $expectedCount = @($account.RolePermissions[$roleName]).Count
                Assert-BatchRegistrationResult -Result $results[$i] -CountProperty 'RegisteredPermissionCount' -InvalidItemsProperty 'InvalidPermissionIds' -ExpectedCount $expectedCount -Context "Permission assignment for role $roleName in $($account.Name)"
            }
        }

        $groupRoleNames = @($account.GroupRoles.Keys)
        if ($groupRoleNames.Count -gt 0) {
            $groupRoleRequests = @(
                $groupRoleNames | ForEach-Object {
                    $groupName = $_
                    [ordered]@{
                        RoleIds = @($account.GroupRoles[$groupName] | ForEach-Object { $accountState.Roles[$_] })
                        GroupId = $accountState.Groups[$groupName]
                        AccountId = $accountId
                    }
                }
            )
            $requestPath = Write-RequestArray -FileName "register-roles-with-group-$($account.Key)-batch.json" -Payload $groupRoleRequests
            $results = Invoke-CorelyJsonLines -Label "register-roles-with-group-$($account.Key)-batch" -Arguments @('register', 'roles-with-group', $requestPath)
            Assert-True -Condition ($results.Count -eq $groupRoleNames.Count) -Message "Group role assignment result count mismatch for $($account.Name)"

            for ($i = 0; $i -lt $groupRoleNames.Count; $i++) {
                $groupName = $groupRoleNames[$i]
                $expectedCount = @($account.GroupRoles[$groupName]).Count
                Assert-BatchRegistrationResult -Result $results[$i] -CountProperty 'RegisteredRoleCount' -InvalidItemsProperty 'InvalidRoleIds' -ExpectedCount $expectedCount -Context "Role assignment for group $groupName in $($account.Name)"
            }
        }

        $directUserNames = @($account.DirectUserRoles.Keys)
        if ($directUserNames.Count -gt 0) {
            $directUserRoleRequests = @(
                $directUserNames | ForEach-Object {
                    $userName = $_
                    [ordered]@{
                        RoleIds = @($account.DirectUserRoles[$userName] | ForEach-Object { $accountState.Roles[$_] })
                        UserId = $state.Users[$userName]
                        AccountId = $accountId
                    }
                }
            )
            $requestPath = Write-RequestArray -FileName "register-roles-with-user-$($account.Key)-batch.json" -Payload $directUserRoleRequests
            $results = Invoke-CorelyJsonLines -Label "register-roles-with-user-$($account.Key)-batch" -Arguments @('register', 'roles-with-user', $requestPath)
            Assert-True -Condition ($results.Count -eq $directUserNames.Count) -Message "Direct user role assignment result count mismatch for $($account.Name)"

            for ($i = 0; $i -lt $directUserNames.Count; $i++) {
                $userName = $directUserNames[$i]
                $expectedCount = @($account.DirectUserRoles[$userName]).Count
                Assert-BatchRegistrationResult -Result $results[$i] -CountProperty 'RegisteredRoleCount' -InvalidItemsProperty 'InvalidRoleIds' -ExpectedCount $expectedCount -Context "Direct role assignment for $userName in $($account.Name)"
            }
        }

        $groupUserNames = @($account.GroupUsers.Keys)
        if ($groupUserNames.Count -gt 0) {
            $groupUserRequests = @(
                $groupUserNames | ForEach-Object {
                    $groupName = $_
                    [ordered]@{
                        UserIds = @($account.GroupUsers[$groupName] | ForEach-Object { $state.Users[$_] })
                        GroupId = $accountState.Groups[$groupName]
                        AccountId = $accountId
                    }
                }
            )
            $requestPath = Write-RequestArray -FileName "register-users-with-group-$($account.Key)-batch.json" -Payload $groupUserRequests
            $results = Invoke-CorelyJsonLines -Label "register-users-with-group-$($account.Key)-batch" -Arguments @('register', 'users-with-group', $requestPath)
            Assert-True -Condition ($results.Count -eq $groupUserNames.Count) -Message "Group user assignment result count mismatch for $($account.Name)"

            for ($i = 0; $i -lt $groupUserNames.Count; $i++) {
                $groupName = $groupUserNames[$i]
                $expectedCount = @($account.GroupUsers[$groupName]).Count
                Assert-BatchRegistrationResult -Result $results[$i] -CountProperty 'RegisteredUserCount' -InvalidItemsProperty 'InvalidUserIds' -ExpectedCount $expectedCount -Context "Group user assignment for $groupName in $($account.Name)"
            }
        }

        $memberList = Invoke-CorelyJson -Label "validate-users-$($account.Key)" -Arguments @('retrieval', 'list-users', '-a', $accountId, '-s', '0', '-t', '50')
        $roleList = Invoke-CorelyJson -Label "validate-roles-$($account.Key)" -Arguments @('retrieval', 'list-roles', '-a', $accountId, '-s', '0', '-t', '50')
        $groupList = Invoke-CorelyJson -Label "validate-groups-$($account.Key)" -Arguments @('retrieval', 'list-groups', '-a', $accountId, '-s', '0', '-t', '50')
        $permissionList = Invoke-CorelyJson -Label "validate-permissions-$($account.Key)" -Arguments @('retrieval', 'list-permissions', '-a', $accountId, '-s', '0', '-t', '50')

        Assert-True -Condition ($memberList.Data.TotalCount -eq $account.ExpectedMembers) -Message "Unexpected member count for $($account.Name)"
        Assert-True -Condition ($roleList.Data.TotalCount -eq $account.ExpectedRoles) -Message "Unexpected role count for $($account.Name)"
        Assert-True -Condition ($groupList.Data.TotalCount -eq $account.ExpectedGroups) -Message "Unexpected group count for $($account.Name)"
        Assert-True -Condition ($permissionList.Data.TotalCount -eq $account.ExpectedPermissions) -Message "Unexpected permission count for $($account.Name)"

        $state.Validation[$account.Key] = [ordered]@{
            Members = $memberList.Data.TotalCount
            Roles = $roleList.Data.TotalCount
            Groups = $groupList.Data.TotalCount
            Permissions = $permissionList.Data.TotalCount
        }
        $accountState['AssignmentsComplete'] = $true
        Save-State
    }

    Sign-OutCurrentUser -Label "signout-build-account-$($account.Key)"
}

Write-Host '==> Final validation'
$naAccountId = $state.Accounts['na'].AccountId

Sign-InUser -Username 'alice.johnson' -AccountId $null
$aliceAccounts = Invoke-CorelyJson -Label 'validate-alice-accounts' -Arguments @('retrieval', 'list-accounts', '-s', '0', '-t', '25')
Assert-True -Condition ($aliceAccounts.ResultCode -eq 0) -Message 'Failed to list accounts for alice.johnson'
Assert-True -Condition ($aliceAccounts.Data.TotalCount -eq 5) -Message 'alice.johnson should see 5 accounts'
Assert-True -Condition ($aliceAccounts.Data.HasMore -eq $false) -Message 'alice.johnson accounts list should not page'
Sign-OutCurrentUser -Label 'signout-validate-alice-accounts'

Sign-InUser -Username 'alice.johnson' -AccountId $naAccountId
$naUsersPage1 = Invoke-CorelyJson -Label 'validate-na-users-page1' -Arguments @('retrieval', 'list-users', '-a', $naAccountId, '-s', '0', '-t', '25')
$naUsersPage2 = Invoke-CorelyJson -Label 'validate-na-users-page2' -Arguments @('retrieval', 'list-users', '-a', $naAccountId, '-s', '25', '-t', '25')
$naRoles = Invoke-CorelyJson -Label 'validate-na-roles-summary' -Arguments @('retrieval', 'list-roles', '-a', $naAccountId, '-s', '0', '-t', '25')
$naGroups = Invoke-CorelyJson -Label 'validate-na-groups-summary' -Arguments @('retrieval', 'list-groups', '-a', $naAccountId, '-s', '0', '-t', '25')
$naPermissionsPage1 = Invoke-CorelyJson -Label 'validate-na-permissions-page1' -Arguments @('retrieval', 'list-permissions', '-a', $naAccountId, '-s', '0', '-t', '25')
$naPermissionsPage2 = Invoke-CorelyJson -Label 'validate-na-permissions-page2' -Arguments @('retrieval', 'list-permissions', '-a', $naAccountId, '-s', '25', '-t', '25')

Assert-True -Condition ($naUsersPage1.Data.TotalCount -eq 30) -Message 'North America should have 30 users'
Assert-True -Condition ($naUsersPage1.Data.HasMore -eq $true) -Message 'North America users page 1 should indicate paging'
Assert-True -Condition ($naUsersPage2.Data.Items.Count -eq 5) -Message 'North America users page 2 should contain 5 users'
Assert-True -Condition ($naRoles.Data.TotalCount -eq 10) -Message 'North America should have 10 roles'
Assert-True -Condition ($naGroups.Data.TotalCount -eq 8) -Message 'North America should have 8 groups'
Assert-True -Condition ($naPermissionsPage1.Data.TotalCount -eq 28) -Message 'North America should have 28 permissions'
Assert-True -Condition ($naPermissionsPage1.Data.HasMore -eq $true) -Message 'North America permissions page 1 should indicate paging'
Assert-True -Condition ($naPermissionsPage2.Data.Items.Count -eq 3) -Message 'North America permissions page 2 should contain 3 permissions'

$state.Validation['alice-accounts'] = [ordered]@{
    Accounts = $aliceAccounts.Data.TotalCount
    HasMore = $aliceAccounts.Data.HasMore
}

$state.Validation['na-paging'] = [ordered]@{
    UsersTotal = $naUsersPage1.Data.TotalCount
    UsersPage2Count = $naUsersPage2.Data.Items.Count
    RolesTotal = $naRoles.Data.TotalCount
    GroupsTotal = $naGroups.Data.TotalCount
    PermissionsTotal = $naPermissionsPage1.Data.TotalCount
    PermissionsPage2Count = $naPermissionsPage2.Data.Items.Count
}

Save-State
Sign-OutCurrentUser -Label 'signout-final-validation'

Write-Host ''
Write-Host 'Demo environment seed complete.'
Write-Host "Artifacts saved under: $seedRoot"
Write-Host ''
Write-Host 'Created accounts:'
foreach ($accountKey in $state.Accounts.Keys) {
    $accountState = $state.Accounts[$accountKey]
    Write-Host "- $($accountState.Name) [$($accountState.AccountId)]"
}
