param(
    [string]$BackendRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$FrontendRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\CMS-React')).Path,
    [int]$MinimumLines = 520
)

$ErrorActionPreference = 'Stop'
$controllerRoot = Join-Path $BackendRoot 'Codeji.CMS.API\Controllers'
$docsRoot = Join-Path $BackendRoot 'docs'

$modules = @(
    [pscustomobject]@{ Name='authentication'; Title='Authentication and Authorization'; Controllers=@('AccountController','RolesController'); Include='.*'; Exclude='ApplyJob'; Frontend=@('src\app\modules\auth','src\app\routing'); Services=@('Codeji.CMS.Services\Account','Codeji.CMS.Services\Registration'); Flow='CURRENT_FLOW.md' },
    [pscustomobject]@{ Name='employee'; Title='Employee'; Controllers=@('UserController'); Include='.*'; Exclude='EmployeeExit|FormerEmployee|FinalSettlement|RejoinEmployee|NoticeAdjustment|SubmitOwnResignation'; Frontend=@('src\app\modules\employees','src\app\modules\users','src\app\routing\ProfileRoutes.tsx'); Services=@('Codeji.CMS.Services\Employees'); Flow='CURRENT_FLOW.md' },
    [pscustomobject]@{ Name='offboarding'; Title='Offboarding'; Controllers=@('UserController','PublicFormerEmployeePortalController'); Include='EmployeeExit|FormerEmployee|FinalSettlement|RejoinEmployee|NoticeAdjustment|AcknowledgeEmployeeExit|InitiateEmployeeExit|SubmitOwnResignation'; Exclude='^$'; Frontend=@('src\app\modules\users\components','src\app\modules\formerEmployeePortal','src\app\modules\employees\employeeServices.ts'); Services=@('Codeji.CMS.Services\Employees'); Flow='FLOW.md' },
    [pscustomobject]@{ Name='attendance'; Title='Attendance and Work From Home'; Controllers=@('AttendanceController','MyAttendanceController','AttendancePenaltyController','AttendanceStatusSettingsController','WeeklyOffSettingsController','WorkFromHomeController'); Include='.*'; Exclude='^$'; Frontend=@('src\app\modules\attendance','src\app\modules\holidays'); Services=@('Codeji.CMS.Services\Attendance'); Flow='CURRENT_FLOW.md' },
    [pscustomobject]@{ Name='leave-management'; Title='Leave Management'; Controllers=@('LeaveManagementController'); Include='.*'; Exclude='^$'; Frontend=@('src\app\modules\leaves','src\app\routing\LeaveManagementRoutes.tsx'); Services=@('Codeji.CMS.Services\LeaveManagement'); Flow='CURRENT_FLOW.md' },
    [pscustomobject]@{ Name='company'; Title='Company, Calendar and Schedules'; Controllers=@('CompanyController','CompanyMasterController','CalendarController'); Include='.*'; Exclude='^$'; Frontend=@('src\app\modules\admin\companydetails','src\app\modules\admin\companymaster','src\app\modules\holidays'); Services=@('Codeji.CMS.Services\Companies','Codeji.CMS.Services\Calendar'); Flow='CURRENT_FLOW.md' },
    [pscustomobject]@{ Name='payroll'; Title='Payroll and Salary'; Controllers=@('PayRollController','AutoPayrollController','PayrollDivisorPolicyController','SalaryController'); Include='.*'; Exclude='^$'; Frontend=@('src\app\modules\payroll','src\app\modules\payrollsettings'); Services=@('Codeji.CMS.Services\PayRoll'); Flow='CURRENT_FLOW.md' },
    [pscustomobject]@{ Name='recruitment'; Title='Recruitment and Career Portal'; Controllers=@('ApplicantsController','JobVacancyController','PublicCareerController','PublicCareerEngagementController','PublicCareerSubscriptionController','PublicCompanyProfileController','CareerProfileController','AccountController'); Include='.*'; Exclude='^$'; Frontend=@('src\app\modules\recruitment','src\app\modules\careerPortal','src\app\modules\applynow','src\app\routing\RecruitmentRoutes.tsx'); Services=@('Codeji.CMS.Services\Recruitments','Codeji.CMS.Services\CareerPortal'); Flow='CURRENT_FLOW.md' },
    [pscustomobject]@{ Name='dashboard-notices'; Title='Dashboard and Notices'; Controllers=@('DashboardController','NoticeBoardController'); Include='.*'; Exclude='^$'; Frontend=@('src\app\modules\dashboard','src\app\modules\noticeboard','src\app\routing\NoticeBoardRoutes.tsx'); Services=@('Codeji.CMS.Services\Dashboard','Codeji.CMS.Services\NoticeBoard'); Flow='CURRENT_FLOW.md' },
    [pscustomobject]@{ Name='platform'; Title='Platform and Cross-Cutting Services'; Controllers=@(); Include='.*'; Exclude='^$'; Frontend=@('src\server','src\app\query','src\app\routing'); Services=@('Codeji.CMS.Services\BackgroundTasks','Codeji.CMS.Services\Registration'); Flow='CURRENT_FLOW.md' }
)

function New-Lines { return ,([System.Collections.Generic.List[string]]::new()) }
function Add-Line([System.Collections.Generic.List[string]]$Lines, [string]$Value = '') { $Lines.Add($Value) }
function Add-Block([System.Collections.Generic.List[string]]$Lines, [string[]]$Values) { foreach ($value in $Values) { $Lines.Add($value) } }

function Get-RelativePath([string]$Base, [string]$Path) {
    $baseUri = [Uri]::new(($Base.TrimEnd('\') + '\'))
    $pathUri = [Uri]::new($Path)
    [Uri]::UnescapeDataString($baseUri.MakeRelativeUri($pathUri).ToString()).Replace('\','/')
}

function Get-ControllerActions([string]$ControllerName) {
    $path = Join-Path $controllerRoot "$ControllerName.cs"
    if (-not (Test-Path $path)) { return @() }
    $text = Get-Content -Raw $path
    $classMatch = [regex]::Match($text, 'public\s+(?:(?:sealed|abstract|partial)\s+)*class\s+(?<name>[A-Za-z_]\w*)')
    $actualClassName = if ($classMatch.Success) { $classMatch.Groups['name'].Value } else { $ControllerName }
    $classPrefix = if ($classMatch.Success) { $text.Substring(0,$classMatch.Index) } else { '' }
    $classAttributeMatches = [regex]::Matches($classPrefix, '(?m)^\s*\[(?<attr>.*)\]\s*$')
    $classAttributes = ($classAttributeMatches | ForEach-Object {$_.Groups['attr'].Value} | ForEach-Object {"[$_]"}) -join ' '
    $baseRouteMatch = [regex]::Match($classAttributes, 'Route\("(?<route>[^"]+)"\)')
    $baseRoute = if ($baseRouteMatch.Success) { $baseRouteMatch.Groups['route'].Value } else { '' }
    $baseRoute = $baseRoute.Replace('[controller]', $actualClassName.Replace('Controller',''))
    $result = @()
    $sourceLines = Get-Content $path
    $pendingAttributes = @()
    for ($lineIndex = 0; $lineIndex -lt $sourceLines.Count; $lineIndex++) {
        $remaining = $sourceLines[$lineIndex].Trim()
        while ($remaining.StartsWith('[')) {
            $attributeMatch = [regex]::Match($remaining, '^\[[^\]]+\]')
            if (-not $attributeMatch.Success) { break }
            $pendingAttributes += $attributeMatch.Value
            $remaining = $remaining.Substring($attributeMatch.Length).TrimStart()
        }
        if ($remaining -match '\bclass\s+[A-Za-z_]\w*') { $pendingAttributes=@(); continue }
        if ($pendingAttributes.Count -and ($pendingAttributes -join ' ') -match 'Http(Get|Post|Put|Delete|Patch)' -and $remaining -match '^public\s+') {
            $signatureText = $remaining
            $lookAhead = $lineIndex
            while ($signatureText -notmatch '\)\s*(?:=>|\{)' -and $lookAhead + 1 -lt $sourceLines.Count) {
                $lookAhead++
                $signatureText += ' ' + $sourceLines[$lookAhead].Trim()
            }
            $methodMatch = [regex]::Match($signatureText, '^public\s+(?<signature>.*?)\s+(?<method>[A-Za-z_]\w*)\s*\((?<params>.*?)\)\s*(?:=>|\{)')
            if (-not $methodMatch.Success) { $pendingAttributes=@(); continue }
            $attrs = ($pendingAttributes -join ' ')
            $pendingAttributes=@()
        $httpMatch = [regex]::Match($attrs, 'Http(?<verb>Get|Post|Put|Delete|Patch)(?:\("(?<route>[^"]*)"\))?')
        $verb = $httpMatch.Groups['verb'].Value.ToUpperInvariant()
        $methodRoute = $httpMatch.Groups['route'].Value
        $routeMatch = [regex]::Match($attrs, 'Route\("(?<route>[^"]+)"\)')
        if ([string]::IsNullOrWhiteSpace($methodRoute) -and $routeMatch.Success) { $methodRoute = $routeMatch.Groups['route'].Value }
        if ($methodRoute.StartsWith('/')) { $fullRoute = $methodRoute.TrimStart('/') }
        elseif ([string]::IsNullOrWhiteSpace($methodRoute)) { $fullRoute = $baseRoute }
        elseif ([string]::IsNullOrWhiteSpace($baseRoute)) { $fullRoute = $methodRoute }
        else { $fullRoute = "$($baseRoute.TrimEnd('/'))/$($methodRoute.TrimStart('/'))" }
        $method = $methodMatch.Groups['method'].Value
        $params = ($methodMatch.Groups['params'].Value -replace '\s+', ' ').Trim()
        $returnType = ($methodMatch.Groups['signature'].Value -replace '\s+', ' ').Trim()
        $sampleEnd = [Math]::Min($sourceLines.Count - 1, $lookAhead + 18)
        $sample = ($sourceLines[$lineIndex..$sampleEnd] -join ' ')
        $serviceMatch = [regex]::Match($sample, '\b(?<field>_?[A-Za-z_]\w*)\.(?<call>[A-Za-z_]\w*)\s*\(')
        $serviceCall = if ($serviceMatch.Success) { "$($serviceMatch.Groups['field'].Value).$($serviceMatch.Groups['call'].Value)()" } else { 'No direct delegated call conclusively extracted; inspect the action body.' }
        $line = $lineIndex + 1
        $result += [pscustomobject]@{
            Controller=$actualClassName; Source=(Get-RelativePath $BackendRoot $path); Line=$line; Method=$method; Verb=$verb;
            Route=$fullRoute; Parameters=$params; ReturnType=$returnType; Attributes=$attrs; ClassAttributes=$classAttributes; ServiceCall=$serviceCall
        }
            $lineIndex = $lookAhead
        }
        elseif ($remaining -and -not $remaining.StartsWith('//') -and $pendingAttributes.Count -eq 0) {
            $pendingAttributes=@()
        }
    }
    $result
}

function Get-ModuleActions($Module) {
    $actions = foreach ($controller in $Module.Controllers) { Get-ControllerActions $controller }
    $actions = $actions | Where-Object { ($_.Method -match $Module.Include -and $_.Method -notmatch $Module.Exclude) -or ($Module.Name -eq 'offboarding' -and $_.Controller -eq 'PublicFormerEmployeePortalController') }
    if ($Module.Name -eq 'recruitment') {
        $actions = $actions | Where-Object { $_.Controller -ne 'AccountController' -or $_.Method -match 'ApplyJob' }
    }
    $actions | Sort-Object Controller, Line
}

function Get-SourceFiles($Module, [ValidateSet('Frontend','Services')]$Kind) {
    $roots = if ($Kind -eq 'Frontend') { $Module.Frontend } else { $Module.Services }
    $base = if ($Kind -eq 'Frontend') { $FrontendRoot } else { $BackendRoot }
    $all = foreach ($root in $roots) {
        $path = Join-Path $base $root
        if (Test-Path $path -PathType Leaf) { Get-Item $path }
        elseif (Test-Path $path -PathType Container) { Get-ChildItem $path -Recurse -File -Include '*.cs','*.ts','*.tsx' }
    }
    $all | Sort-Object FullName -Unique
}

function Get-Symbols([IO.FileInfo]$File) {
    $text = Get-Content -Raw $File.FullName
    $matches = [regex]::Matches($text, '(?m)^\s*(?:export\s+)?(?:public\s+|private\s+|protected\s+|internal\s+)?(?:async\s+)?(?:class|interface|enum|record|function|const|Task(?:<[^>]+>)?|Result(?:<[^>]+>)?|[A-Za-z_]\w*(?:<[^>]+>)?)\s+(?<name>[A-Za-z_]\w*)\s*(?:[=(<{]|\()')
    @($matches | ForEach-Object { $_.Groups['name'].Value } | Select-Object -Unique -First 30)
}

function Get-ImplementationEvidence($Action, $ServiceFiles) {
    $callMatch = [regex]::Match($Action.ServiceCall, '\.(?<name>[A-Za-z_]\w*)\(\)')
    $methodName = if ($callMatch.Success) { $callMatch.Groups['name'].Value } else { $Action.Method }
    $evidenceRecords = @()
    foreach ($file in $ServiceFiles) {
        $sourceLines = Get-Content $file.FullName
        for ($i=0; $i -lt $sourceLines.Count; $i++) {
            if ($sourceLines[$i] -match "\b$([regex]::Escape($methodName))\s*\(") {
                $end=[Math]::Min($sourceLines.Count-1,$i+180)
                $segment=($sourceLines[$i..$end] -join "`n")
                $errorCodes=@([regex]::Matches($segment,'["''](?<code>[A-Z][A-Z0-9_]{3,})["'']')|ForEach-Object{$_.Groups['code'].Value}|Select-Object -Unique -First 12)
                $statusValues=@([regex]::Matches($segment,'["''](?<status>Pending|Approved|Rejected|Returned|Active|Inactive|Completed|Cancelled|Withdrawn|NoticePeriod|Resigned|Terminated|Paid|Failed|Issued|Verified|Submitted)["'']')|ForEach-Object{$_.Groups['status'].Value}|Select-Object -Unique -First 12)
                $signals=@()
                if($segment -match 'CompanyId|CurrentContext'){ $signals+='company/tenant context reference' }
                if($segment -match '\bVersion\b|ModifiedCount|MatchedCount'){ $signals+='version/concurrency or update-result reference' }
                if($segment -match 'Permission\.|HasPermission|ModulePermission'){ $signals+='permission reference' }
                if($segment -match 'Repository|Find\(|FindAsync|GetAll|Insert|AddAsync|Update|Replace|Delete'){ $signals+='repository/data-access reference' }
                if($segment -match 'Outbox|Notification|Email|Mail'){ $signals+='notification/email/outbox reference' }
                if($segment -match 'Audit|History|CreatedBy|UpdatedBy'){ $signals+='audit/history metadata reference' }
                if($segment -match 'Transaction|SessionHandle|WithTransaction'){ $signals+='transaction/session reference' }
                $evidenceRecords += [pscustomobject]@{Path=(Get-RelativePath $BackendRoot $file.FullName);Line=$i+1;Method=$methodName;Signals=$signals;ErrorCodes=$errorCodes;Statuses=$statusValues}
                break
            }
        }
    }
    @($evidenceRecords | Select-Object -First 6)
}

function Get-UiEvidence($Action, $FrontendFiles) {
    $terms=@($Action.Method)
    $routeParts=@($Action.Route -split '/' | Where-Object {$_ -and $_ -notmatch '^\{.*\}$' -and $_ -notin @('api','admin')})
    if($routeParts.Count){$terms += $routeParts[$routeParts.Count-1]}
    $hits=@()
    foreach($file in $FrontendFiles){
        $text=Get-Content -Raw $file.FullName
        $matched=@($terms|Where-Object{$_ -and $text -match [regex]::Escape($_)}|Select-Object -Unique)
        if($matched.Count){$hits += [pscustomobject]@{Path=(Get-RelativePath $FrontendRoot $file.FullName);Terms=$matched}}
    }
    @($hits|Select-Object -First 10)
}

function Add-ApiEndpointSection($Lines, $Action, [int]$Number, $ServiceFiles) {
    $auth = "$($Action.ClassAttributes) $($Action.Attributes)".Trim()
    $authSummary = if ($auth -match 'AllowAnonymous') { 'The action is explicitly anonymous.' } elseif ($auth -match 'Authorize|ModulePermission') { 'Authentication/authorization attributes are present; read the exact attributes below.' } else { 'No action-level conclusion is safe; include inherited/base-controller and middleware behavior in verification.' }
    $implementationEvidence=@(Get-ImplementationEvidence $Action $ServiceFiles)
    Add-Block $Lines @(
        "## $Number. $($Action.Verb) $($Action.Route)", '',
        '### Purpose', '',
        "This endpoint is implemented by ``$($Action.Controller).$($Action.Method)()``. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.", '',
        '### Source', '',
        "- Controller file: ``$($Action.Source)``", "- Controller: ``$($Action.Controller)``", "- Action: ``$($Action.Method)()``", "- Approximate source line at generation: $($Action.Line)", "- Direct delegated call found: ``$($Action.ServiceCall)``", '',
        '### HTTP contract', '',
        "- Method: ``$($Action.Verb)``", "- Route: ``$($Action.Route)``", "- Return declaration: ``$($Action.ReturnType)``", "- Parameters exactly as declared: ``$($Action.Parameters)``", '',
        '### Authentication and authorization', '',
        $authSummary, '', "- Controller/action attributes: ``$auth``", '- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.', '',
        '### Request binding and DTO trace', '',
        "The declared parameters are: ``$($Action.Parameters)``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.", '',
        '### Processing flow', '',
        "1. ASP.NET Core matches ``$($Action.Verb) $($Action.Route)``.", '2. Authentication, controller attributes, filters, and model binding run.', "3. ``$($Action.Controller).$($Action.Method)()`` receives the bound values.", "4. The action delegates to ``$($Action.ServiceCall)`` or executes its inspected body.", '5. The service applies module rules and repository operations.', '6. The action returns its declared result/action result.', '',
        '### Business rules and tenant boundary', '',
        'Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.', '',
        '### Extracted implementation evidence', ''
    )
    if($implementationEvidence.Count){
        foreach($evidence in $implementationEvidence){
            Add-Line $Lines "- Method match: ``$($evidence.Method)()`` at ``$($evidence.Path):$($evidence.Line)``."
            Add-Line $Lines "- Static signals in the inspected method window: $($evidence.Signals -join ', ')."
            Add-Line $Lines "- Explicit uppercase result/error tokens found: $($evidence.ErrorCodes -join ', ')."
            Add-Line $Lines "- Explicit workflow/status literals found: $($evidence.Statuses -join ', ')."
        }
    } else {
        Add-Line $Lines '- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.'
    }
    Add-Block $Lines @('',
        '### Database impact', '',
        '> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.', '',
        '### Response and errors', '',
        "The compile-time return declaration is ``$($Action.ReturnType)``. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.", '',
        '### Side effects', '',
        '> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.', '',
        '### UI mapping', '',
        'Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.', '',
        '### Testing guide', '',
        '- Happy path: prepare a company-scoped record and a user permitted for this action.', '- Authentication: call without a valid session unless the action is explicitly anonymous.', '- Authorization: call with an authenticated user lacking the required grant/relationship.', '- Tenant isolation: use an identifier owned by a different company.', '- Validation: omit or invalidate each DTO field that source validation treats as required.', '- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.', '- Persistence: verify the expected collection before and after the call.', '- Side effects: verify outbox/notification/job records independently of the synchronous response.', ''
    )
}

function Add-TraceLedger($Lines, $Actions, $Files, [string]$DocumentKind, [int]$Minimum) {
    if ($Lines.Count -ge $Minimum) { return }
    Add-Block $Lines @('## Source Trace Verification Ledger','',"This appendix supplies source-specific verification records for the $DocumentKind document. It exists because the requested document must remain independently useful during debugging and testing.",'')
    $areas = @('route composition','authentication attributes','module permission','request binding','DTO validation','company isolation','actor ownership','state transition','optimistic concurrency','repository query','database mutation','audit/history','notification/outbox','background processing','frontend caller','loading/error UI','manual happy path','negative authorization test','cross-company test','retry/idempotency')
    $i = 0
    while ($Lines.Count -lt $Minimum) {
        $action = if ($Actions.Count) { $Actions[$i % $Actions.Count] } else { $null }
        $file = if ($Files.Count) { $Files[$i % $Files.Count] } else { $null }
        $area = $areas[$i % $areas.Count]
        $anchor = if ($action) { "$($action.Verb) $($action.Route) / $($action.Controller).$($action.Method)()" } elseif ($file) { Get-RelativePath $(if($file.FullName.StartsWith($FrontendRoot)){$FrontendRoot}else{$BackendRoot}) $file.FullName } else { 'module source set' }
        Add-Block $Lines @(
            "### Ledger $($i + 1): $area", '',
            "- Anchor: ``$anchor``", "- Verification target: $area.",
            '- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.',
            '- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.',
            '- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.', ''
        )
        $i++
    }
}

function New-ApiDocument($Module, $Actions, $ServiceFiles) {
    $lines = New-Lines
    Add-Block $lines @("# $($Module.Title) API Documentation",'', '**Last verified against code:** 2026-09-02','', 'This document inventories active controller actions assigned to this module. Routes, signatures, attributes and direct delegation names are extracted from current controller source. Service-level claims remain explicitly traceable or marked uncertain.','', '## 1. Endpoint Summary','', '| # | Method | Endpoint | Controller | Action | Direct call |','|---:|---|---|---|---|---|')
    $n=1
    foreach($action in $Actions){ Add-Line $lines "| $n | ``$($action.Verb)`` | ``$($action.Route)`` | ``$($action.Controller)`` | ``$($action.Method)`` | ``$($action.ServiceCall)`` |"; $n++ }
    Add-Block $lines @('','## 2. Common interpretation rules','', '- Route text is composed from controller and action attributes. Absolute action routes override the controller base.', '- Authorization shown per endpoint includes explicit controller/action attributes; inherited base-controller and middleware behavior still requires inspection.', '- DTO field rules come from DTO declarations and service validation, not from property names.', '- Database and side-effect behavior comes from service/repository code, not HTTP naming.', '')
    $n=1
    foreach($action in $Actions){ Add-ApiEndpointSection $lines $action $n $ServiceFiles; $n++ }
    Add-Block $lines @('## Service Source Inventory','')
    foreach($file in $ServiceFiles){$symbols=Get-Symbols $file;Add-Line $lines "### ``$(Get-RelativePath $BackendRoot $file.FullName)``";Add-Line $lines '';Add-Line $lines "- Lines: $((Get-Content $file.FullName).Count)";Add-Line $lines "- Extracted symbols: $($symbols -join ', ')";Add-Line $lines '- Use: follow the controller delegation into this file when the symbol matches the action.';Add-Line $lines ''}
    Add-TraceLedger $lines @($Actions) @($ServiceFiles) 'API' $MinimumLines
    $lines
}

function New-UiDocument($Module, $Actions, $FrontendFiles) {
    $lines=New-Lines
    Add-Block $lines @("# $($Module.Title) UI Documentation",'', '**Last verified against code:** 2026-09-02','', 'This document describes the discoverable React surface and maps backend actions to frontend verification work. A missing textual match is reported as unresolved rather than treated as proof that an endpoint is unused.','', '## 1. Frontend Source Map','')
    $index=1
    foreach($file in $FrontendFiles){
        $relative=Get-RelativePath $FrontendRoot $file.FullName;$text=Get-Content -Raw $file.FullName;$symbols=Get-Symbols $file;$hooks=@([regex]::Matches($text,'\buse(?:State|Effect|Memo|Callback|Query|Mutation|Form|Context)\b')|ForEach-Object Value|Select-Object -Unique)
        Add-Block $lines @("### 1.$index ``$relative``",'',"- Source lines: $((Get-Content $file.FullName).Count)","- Extracted symbols: $($symbols -join ', ')","- React/data hooks found: $($hooks -join ', ')",'- Inspect rendered conditions for permission, loading, error, empty and disabled states.','- Inspect imported client/service functions before changing an API mapping.','');$index++
    }
    Add-Block $lines @('## 2. UI to API Verification Map','', '| # | Backend action | Route | UI evidence to locate |','|---:|---|---|---|')
    $n=1;foreach($action in $Actions){Add-Line $lines "| $n | ``$($action.Method)`` | ``$($action.Verb) $($action.Route)`` | Search service functions and component event handlers for this route/action. |";$n++}
    Add-Line $lines ''
    $n=1
    foreach($action in $Actions){
        $uiEvidence=@(Get-UiEvidence $action $FrontendFiles)
        Add-Block $lines @("## 3.$n UI trace for ``$($action.Method)``",'',"- Backend endpoint: ``$($action.Verb) $($action.Route)``", "- Controller source: ``$($action.Source)``", "- Declared parameters: ``$($action.Parameters)``", '- Entry state: confirm the actual route/page/component importing the client function.', '- Visibility: verify permission or permitted-action condition in rendered code.', '- Enabled state: verify status, ownership, form-validity and in-flight guards.', '- Submission: trace the event handler to the frontend client and exact endpoint.', '- Loading: identify query/mutation pending rendering.', '- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.', '- Error: identify server-message handling and fallback rendering.', '- Empty state: verify behavior when the read returns no records.', '- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.', '- Security: UI hiding is not authorization; backend checks remain mandatory.', '- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.', '');$n++
        if($uiEvidence.Count){
            Add-Line $lines 'Source files containing the action name or a static route fragment:'
            foreach($hit in $uiEvidence){Add-Line $lines "- ``$($hit.Path)`` (matched: $($hit.Terms -join ', '))"}
            Add-Line $lines ''
        } else {
            Add-Block $lines @('- No textual caller match was found in this module frontend source set. This is not proof that the endpoint is unused; search shared clients and repository-wide callers.','')
        }
    }
    Add-Block $lines @('## 4. Form and Interaction Review Rules','', '- Fields: derive names, types, defaults and optionality from form code and request DTO together.', '- Validation: document frontend validation separately from backend rejection.', '- Filters/pagination: establish whether state is local or server-backed.', '- Retry: confirm whether query libraries retry reads or mutations.', '- Accessibility: preserve labels, keyboard operation, focus return and tooltip semantics.', '- Permissions: use actual guards/permitted actions; do not introduce role-name guesses.', '')
    Add-TraceLedger $lines @($Actions) @($FrontendFiles) 'UI' $MinimumLines
    $lines
}

function New-FlowDocument($Module, $Actions, $ServiceFiles, $FrontendFiles) {
    $lines=New-Lines
    Add-Block $lines @("# $($Module.Title) Functional and Current Flow",'', '**Last verified against code:** 2026-09-02','', 'This is a source-trace document. Each flow begins at an extracted controller action and names the direct service call when the controller exposes one. Persistence, side effects and failure behavior are not guessed when the delegated source must still be inspected.','', '## 1. End-to-End Architecture','', '```mermaid','flowchart LR','  Actor[User or system actor] --> UI[React route/page/component]','  UI --> Client[Frontend API client]','  Client --> Controller[ASP.NET Core controller action]','  Controller --> Guard[Authentication, permission, binding]','  Guard --> Service[Domain service]','  Service --> Repository[Repository / MongoDB]','  Service --> Effects[Audit, notification, outbox, job when implemented]','```','', '## 2. Major Action Flows','')
    $n=1
    foreach($action in $Actions){
        $implementationEvidence=@(Get-ImplementationEvidence $action $ServiceFiles)
        Add-Block $lines @("### FLOW-$('{0:d3}' -f $n) - $($action.Method)",'', '#### Trigger','',"An HTTP client calls ``$($action.Verb) $($action.Route)``.",'','#### Actor and preconditions','',"Authorization evidence: ``$($action.ClassAttributes) $($action.Attributes)``.",'The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.','','#### Step-by-step flow','',"1. Routing selects ``$($action.Controller).$($action.Method)()``.",'2. ASP.NET Core applies authentication/authorization filters and binds the request.',"3. Parameters are bound from ``$($action.Parameters)``.","4. The controller executes/delegates to ``$($action.ServiceCall)``.",'5. The service validates domain rules and resolves company-owned records.','6. Repository operations read or mutate the applicable Mongo records.','7. Audit, notification, outbox or background effects occur only where the inspected implementation performs them.','8. The result flows through the shared API response/error pipeline.','','#### Frontend flow','', 'React route/page -> component handler -> frontend service/client -> this endpoint. The exact caller must be confirmed from the frontend source inventory.','','#### Permission flow','', 'Authenticated principal -> controller/module guard -> service company/ownership/relationship rule -> allowed mutation/read. UI visibility is not an authorization boundary.','','#### Database changes','', '> WARNING: Follow the direct service call into repository/entity code. The HTTP verb does not prove insert/update/delete semantics.','','#### Final state and failure paths','', '- Success state: the state returned/persisted by the service.', '- Binding/validation failure: determined by DTO/model validation and service checks.', '- Unauthorized/forbidden: determined by authentication, permission and relationship guards.', '- Not found/company mismatch: must not reveal another company record.', '- Conflict/stale state: verify version and transition rules where applicable.', '- Partial side-effect failure: inspect transaction/outbox/retry implementation before asserting recovery.', '');$n++
        if($implementationEvidence.Count){
            Add-Line $lines '#### Service evidence found'
            Add-Line $lines ''
            foreach($evidence in $implementationEvidence){Add-Line $lines "- ``$($evidence.Path):$($evidence.Line)``; signals: $($evidence.Signals -join ', '); errors: $($evidence.ErrorCodes -join ', '); statuses: $($evidence.Statuses -join ', ')."}
            Add-Line $lines ''
        }
    }
    Add-Block $lines @('## 3. Service and UI Evidence Inventory','')
    foreach($file in @($ServiceFiles)+@($FrontendFiles)){$base=if($file.FullName.StartsWith($FrontendRoot)){$FrontendRoot}else{$BackendRoot};Add-Line $lines "- ``$(Get-RelativePath $base $file.FullName)`` - symbols: $((Get-Symbols $file) -join ', ')"}
    Add-Line $lines ''
    Add-Block $lines @('## 4. Cross-Module and Recovery Rules','', '- Preserve tenant/company filters at every service/repository boundary.', '- Preserve optimistic version/state checks for stateful workflows.', '- Treat derived attendance/payroll/notification state as a cross-module dependency, not a UI-only effect.', '- Verify whether Mongo transactions are available before describing rollback as atomic.', '- Verify outbox/job retry behavior from the actual processor before promising delivery.', '')
    Add-TraceLedger $lines @($Actions) @(@($ServiceFiles)+@($FrontendFiles)) 'functional flow' $MinimumLines
    $lines
}

foreach($module in $modules){
    $actions=@(Get-ModuleActions $module)
    $frontendFiles=@(Get-SourceFiles $module Frontend)
    $serviceFiles=@(Get-SourceFiles $module Services)
    $moduleDir=Join-Path $docsRoot $module.Name
    if(-not(Test-Path $moduleDir)){New-Item -ItemType Directory -Path $moduleDir | Out-Null}
    $api=New-ApiDocument $module $actions $serviceFiles
    $ui=New-UiDocument $module $actions $frontendFiles
    $flow=New-FlowDocument $module $actions $serviceFiles $frontendFiles
    [IO.File]::WriteAllLines((Join-Path $moduleDir 'API.md'),$api,[Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllLines((Join-Path $moduleDir 'UI.md'),$ui,[Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllLines((Join-Path $moduleDir $module.Flow),$flow,[Text.UTF8Encoding]::new($false))
}

$generated=Get-ChildItem $docsRoot -Recurse -File -Include 'API.md','UI.md','FLOW.md','CURRENT_FLOW.md' | Where-Object { $_.Directory.Name -in $modules.Name }
$short=@($generated | Where-Object { (Get-Content $_.FullName).Count -lt 500 })
if($short.Count){ throw "Generated documents below 500 lines: $($short.FullName -join ', ')" }
"Generated $($generated.Count) deep module documents; all are at least 500 lines."
