<#
  SDSM(관리자) 게시 스크립트
    .\publish.ps1              배포본 만들기(0 커밋 확인 → 1 빌드 → 2 게시 → 3 내용 확인 → 4 압축)
    .\publish.ps1 -CheckOnly   1~3단계 검사만(압축·배포 폴더를 만들지 않음)

  조사자 앱 ..\SDSM_Surveyor\tools\publish.ps1 과 같은 구조다(2026-10-19 신설).
  게시 설정은 VS 게시 프로필 FolderProfile.pubxml 과 같다(Release · win-x64 · 단독 실행형 · 단일 파일 아님).
  프로필의 출력 경로(D:\1_Tasks\8_삼성DS\3_publish)만 이 PC 에 없어서 tools\_publish 로 바꿨다.

  ⚠ 게시본에는 SDSMDB.sqlite · mbtiles\(지도 7.6GB)가 들어가지 않는다(프로젝트 밖 파일).
     발주처 설치 폴더에 '덮어쓰기'로 설치해 기존 DB·지도를 그대로 쓴다(작업로그_ADMIN_261020 §7).
  ⚠ 게시본의 sites.json(조사지점 마스터)은 설치 폴더의 sites.json 을 덮는다.
     발주처가 [조사지점 관리]로 마스터를 고쳤다면 덮어쓰기 전에 백업한다.

  ⚠ 압축(4단계)까지 가는 실행은 커밋하지 않은 변경이 있으면 시작하자마자 멈춘다.
     게시한 exe 의 제품 버전에 커밋 해시가 박히므로 압축 내용과 같아야 한다. -CheckOnly 는 경고만 하고 계속한다.

  ⚠ 한글이 들어가는 일은 전부 여기(PowerShell)에서 한다.
     .cmd 는 UTF-8 로 저장하면 cmd.exe 가 멀티바이트 글자에서 읽는 위치를 잃고 명령이 잘려 나간다.
     그래서 publish.cmd 는 ASCII 만 남기고 이 파일을 부른다.
  ⚠ 이 파일은 UTF-8 with BOM 으로 저장한다. BOM 이 없으면 Windows PowerShell 5.1 이
     한글을 CP949 로 읽어 깨진다.
#>
param([switch]$CheckOnly)

$ErrorActionPreference = 'Stop'
# 콘솔을 UTF-8 로. 없거나 실패해도 진행한다(출력 글자만 영향).
try { chcp 65001 > $null } catch { }

# 창이 바로 닫히지 않게 기다린다. 입력을 받을 수 없는 환경(자동 실행)에서는 그냥 넘어간다.
function Wait-Enter($msg) {
    try { Read-Host $msg | Out-Null } catch { }
}

function Fail($msg, $code) {
    Write-Host ''
    Write-Host "[!] $msg" -ForegroundColor Red
    Wait-Enter '계속하려면 Enter'
    exit $code
}

trap {
    Write-Host ''
    Write-Host "[!] 예기치 못한 오류 : $_" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace
    Wait-Enter '계속하려면 Enter'
    exit 99
}

$tools = $PSScriptRoot
$root  = Split-Path -Parent $tools            # ...\SDSM
Set-Location $root

$stage   = Join-Path $tools '_publish'        # 게시 임시 폴더(.gitignore)
$logDir  = Join-Path $tools '_publish_log'    # 빌드 경고·오류 기록(.gitignore)
$exeName = '삼성DS모니터링DB통합관리프로그램.exe'   # 03-05 배포본과 같은 이름(바로가기 유지)

$kind = if ($CheckOnly) { '검사만(-CheckOnly)' } else { '배포본 만들기' }
Write-Host "=== SDSM 관리자 앱 $kind ===" -ForegroundColor Cyan

# ── 0 커밋 확인 ────────────────────────────────────────────────────────────
#   .gitignore 로 빠지는 파일은 git status 에 나오지 않는다(게시 임시 폴더 · 빌드 기록 · bin · obj).
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }   # 한글 파일 이름이 깨지지 않게
$eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'   # git 이 stderr 에 쓰는 안내로 멈추지 않게
$dirty  = @(& git -C $root -c core.quotepath=off status --porcelain 2>$null)
$gitOk  = ($LASTEXITCODE -eq 0)
$head   = (& git -C $root rev-parse --short HEAD 2>$null)
$branch = (& git -C $root rev-parse --abbrev-ref HEAD 2>$null)
$ErrorActionPreference = $eap
if (-not $gitOk) { Fail 'git 상태를 읽지 못했습니다(git 이 없거나 저장소가 아닙니다).' 5 }
Write-Host "    브랜치 $branch · 커밋 $head"
if ($dirty.Count -gt 0) {
    Write-Host ''
    Write-Host '[!] 커밋하지 않은 변경이 있습니다 — 먼저 커밋하세요' -ForegroundColor Yellow
    foreach ($line in $dirty) { Write-Host "      $line" }
    if ($CheckOnly) {
        Write-Host '    -CheckOnly : 1~3단계 검사만 하므로 계속합니다(압축은 만들지 않습니다).' -ForegroundColor Yellow
    } else {
        Fail "커밋하지 않은 변경 $($dirty.Count)건 — 배포본을 만들지 않았습니다. 커밋한 뒤 다시 실행하세요." 5
    }
} else {
    Write-Host '    작업 폴더 깨끗함(커밋하지 않은 변경 없음)'
}

# 이 저장소에서 띄운 앱이 떠 있으면 exe·dll 이 잠겨 빌드·게시가 실패한다.
#   다른 폴더(설치본 등)에서 띄운 앱은 상관없으므로 경로로 거른다.
$running = @(Get-Process -Name 'SDSM_App', ([System.IO.Path]::GetFileNameWithoutExtension($exeName)) -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith($root + '\', [System.StringComparison]::OrdinalIgnoreCase) })
if ($running.Count -gt 0) {
    Fail "이 저장소의 관리자 앱이 실행 중입니다($($running[0].Path)). 앱을 닫고 다시 실행하세요." 9
}

# ── 1/4 빌드 ───────────────────────────────────────────────────────────────
#   관리자에는 조사자의 xaml·대비 검사 도구가 없다 → Release 전체 빌드의 경고 0 · 오류 0 으로 대신한다.
#   --no-incremental : 증분 빌드는 이미 컴파일한 파일의 경고를 다시 내지 않는다.
Write-Host ''
Write-Host '=== 1/4  빌드 (SDSM.sln · Release · 경고·오류 0 확인) ==='
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir | Out-Null }
$warnLog = Join-Path $logDir 'build_warnings.log'
$errLog  = Join-Path $logDir 'build_errors.log'
foreach ($f in $warnLog, $errLog) { if (Test-Path $f) { Remove-Item $f -Force } }

& dotnet build SDSM.sln -c Release --no-incremental -v minimal -nologo `
    "-flp1:logfile=$warnLog;warningsonly;encoding=utf-8" `
    "-flp2:logfile=$errLog;errorsonly;encoding=utf-8"
$buildExit = $LASTEXITCODE

# 같은 경고가 여러 번 기록될 수 있어(솔루션·프로젝트 단계) 같은 줄은 한 번만 센다. 판정은 0 이냐 아니냐뿐이다.
$warns = @(if (Test-Path $warnLog) { Get-Content $warnLog -Encoding UTF8 | Where-Object { $_.Trim() } | Sort-Object -Unique })
$errs  = @(if (Test-Path $errLog)  { Get-Content $errLog  -Encoding UTF8 | Where-Object { $_.Trim() } | Sort-Object -Unique })
foreach ($l in $errs)  { Write-Host "      $l" -ForegroundColor Red }
foreach ($l in $warns) { Write-Host "      $l" -ForegroundColor Yellow }
if ($buildExit -ne 0 -or $errs.Count -gt 0) { Fail "빌드 실패 — 오류 $($errs.Count)건 (기록 : $errLog)" 1 }
if ($warns.Count -gt 0) { Fail "빌드 경고 $($warns.Count)건 — 경고 0 이어야 게시합니다 (기록 : $warnLog)" 1 }
Write-Host '    경고 0 · 오류 0'

# ── 2/4 게시 ───────────────────────────────────────────────────────────────
Write-Host ''
Write-Host '=== 2/4  게시 (Release · win-x64 · 단독 실행형) ==='
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }

& dotnet publish SDSM_App\SDSM_App.csproj `
    -c Release -r win-x64 --self-contained true `
    -p:DebugType=none -p:DebugSymbols=false `
    -o $stage -v minimal -nologo
if ($LASTEXITCODE -ne 0) { Fail 'dotnet publish 실패' 2 }

# ── 3/4 내용 확인 ──────────────────────────────────────────────────────────
Write-Host ''
Write-Host '=== 3/4  내용 확인 ==='

# 실행 파일 이름 바꾸기 — 03-05 배포본도 게시 뒤 이 이름으로 바꿔 보냈다.
#   exe 는 SDSM_App.dll · SDSM_App.runtimeconfig.json · SDSM_App.deps.json 을 이름으로 찾으므로
#   exe 이름만 바꿔도 실행된다(중복 실행 방지 Mutex 이름도 "SDSM_App" 고정 — App.xaml.cs).
$appExe = Join-Path $stage 'SDSM_App.exe'
if (-not (Test-Path $appExe)) { Fail '게시 결과에 SDSM_App.exe 가 없습니다' 3 }
Rename-Item $appExe $exeName
Write-Host "    실행 파일 이름 : SDSM_App.exe → $exeName"

$must = @(
    $exeName,
    'SDSM_App.dll',
    'SDSM_Core.dll',                 # 생태 계산(2026-08 신설) — 없으면 지수 계산이 멈춘다
    'SDSM_Models.dll',
    'SDSM_App.dll.config',
    'SDSM_App.runtimeconfig.json',   # 이름 바꾼 exe 가 실행에 쓴다
    'SDSM_App.deps.json',
    'sites.json'                     # 조사지점 마스터 초기본(CLAUDE.md §8-1)
)
foreach ($f in $must) {
    if (-not (Test-Path (Join-Path $stage $f))) { Fail "필수 파일 누락 : $f" 3 }
}
Write-Host "    필수 파일 $($must.Count)개 확인"

# 섞이면 안 되는 것 — 작업 DB 가 실려 나가면 발주처 DB 를 덮어쓴다.
#   확장자로만 본다(Microsoft.Data.Sqlite.dll · System.Data.SQLite.dll 은 정상 참조).
$dbFiles = @(Get-ChildItem $stage -Recurse -File | Where-Object { $_.Name -match '\.sqlite3?(-wal|-shm|-journal)?$' })
if ($dbFiles.Count -gt 0) {
    foreach ($d in $dbFiles) { Write-Host "      $($d.FullName.Substring($stage.Length + 1))" -ForegroundColor Red }
    Fail "게시본에 DB 파일이 섞였습니다($($dbFiles.Count)개) — SDSMDB.sqlite 는 배포본에 넣지 않습니다" 3
}
if (Test-Path (Join-Path $stage 'mbtiles')) { Fail '게시본에 mbtiles\ 폴더가 섞였습니다 — 지도는 설치 폴더의 것을 그대로 씁니다' 3 }
Write-Host '    SDSMDB.sqlite · mbtiles\ 없음 확인'

# sites.json — 프로젝트의 SDSM_App\sites.json 과 같은 파일인지, 읽히는지
$siteSrc = Join-Path $root 'SDSM_App\sites.json'
$siteOut = Join-Path $stage 'sites.json'
if ((Get-FileHash $siteSrc -Algorithm SHA256).Hash -ne (Get-FileHash $siteOut -Algorithm SHA256).Hash) {
    Fail 'sites.json 이 프로젝트의 SDSM_App\sites.json 과 다릅니다' 3
}
$catalog = Get-Content $siteOut -Raw -Encoding UTF8 | ConvertFrom-Json
$nSite = @($catalog.Sites).Count
if (-not $catalog.Version -or $nSite -eq 0) { Fail 'sites.json 에 버전 또는 지점이 없습니다' 3 }
Write-Host "    sites.json : $($catalog.Version) · 지점 $nSite 건"

# Telerik 판 — 2026.2.520 고정(CLAUDE.md §8)
$telerik = Join-Path $stage 'Telerik.Windows.Controls.dll'
if (-not (Test-Path $telerik)) { Fail '필수 파일 누락 : Telerik.Windows.Controls.dll' 3 }
$telerikVer = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($telerik).FileVersion
if (-not $telerikVer.StartsWith('2026.2.520')) { Fail "Telerik 판이 2026.2.520 이 아닙니다 — 현재 $telerikVer" 3 }

$rtCfg = Get-Content (Join-Path $stage 'SDSM_App.runtimeconfig.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$rtVer = (@($rtCfg.runtimeOptions.includedFrameworks) | Where-Object { $_.name -eq 'Microsoft.NETCore.App' }).version
$prodVer = [System.Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $stage $exeName)).ProductVersion
Write-Host "    Telerik $telerikVer · .NET 런타임 $rtVer · 제품 버전 $prodVer"

$all = @(Get-ChildItem $stage -Recurse -File)
$mbAll = '{0:N0} MB' -f (($all | Measure-Object Length -Sum).Sum / 1MB)
Write-Host "    파일 $($all.Count)개 · $mbAll"

if ($CheckOnly) {
    Write-Host ''
    Write-Host '=== 1~3단계 통과 (-CheckOnly : 4단계 압축은 하지 않음) ===' -ForegroundColor Green
    exit 0
}

# ── 4/4 압축 ───────────────────────────────────────────────────────────────
Write-Host ''
Write-Host '=== 4/4  압축 ==='
# 보낼 폴더 — 조사자 앱과 같은 ...\samsung\7_보낸파일\yyyy-MM-dd\
#   worktree 에서 실행해도 같은 곳으로 가도록 본 저장소 위치(git 공통 폴더의 부모)에서 찾는다.
$eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
$gitCommon = (& git -C $root rev-parse --path-format=absolute --git-common-dir 2>$null)
$ErrorActionPreference = $eap
if (-not $gitCommon) { Fail 'git 공통 폴더를 찾지 못했습니다' 4 }
$mainRoot = Split-Path -Parent $gitCommon     # ...\1_program\SDSM
$outRoot  = Join-Path $mainRoot '..\..\7_보낸파일'
if (-not (Test-Path $outRoot)) { Fail "보낼 폴더를 찾지 못했습니다 : $outRoot" 4 }
$outRoot = (Resolve-Path $outRoot).Path

$dest = Join-Path $outRoot (Get-Date -Format 'yyyy-MM-dd')
if (-not (Test-Path $dest)) { New-Item -ItemType Directory -Path $dest | Out-Null }

# 이름 — 03-05 배포본(DB통합SW_260305)과 같은 꼴. 압축 안 맨 위 폴더도 같은 이름이다.
#   압축을 풀면 이 폴더 하나가 생기고 그 안에 실행 파일이 있다(맨 위 폴더가 없으면 파일 400여 개가 흩어진다).
$top = 'DB통합SW_' + (Get-Date -Format 'yyMMdd')
$zip = Join-Path $dest ($top + '.zip')
if (Test-Path $zip) { Remove-Item $zip -Force }

# 게시 폴더를 '맨 위 폴더 이름'으로 잠시 옮겨 그 폴더째 압축한다(includeBaseDirectory = $true).
$wrap = Join-Path $tools '_publish_zip'
if (Test-Path $wrap) { Remove-Item $wrap -Recurse -Force }
New-Item -ItemType Directory -Path $wrap | Out-Null
$topDir = Join-Path $wrap $top
Move-Item $stage $topDir

# ⚠ Compress-Archive 는 수백 MB 급에서 느리고 한글 파일명 플래그를 안 세운다.
#    CreateFromDirectory 는 entryNameEncoding 을 UTF-8 로 못박을 수 있다(실행 파일 이름이 한글이다).
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    $topDir, $zip,
    [System.IO.Compression.CompressionLevel]::Optimal,
    $true,
    [System.Text.Encoding]::UTF8)
if (-not (Test-Path $zip)) { Fail '압축 실패' 4 }

Remove-Item $wrap -Recurse -Force
Write-Host "    압축 안 맨 위 폴더 : $top\"

$mb = '{0:N1} MB' -f ((Get-Item $zip).Length / 1MB)
Write-Host ''
Write-Host "완료 : $zip  ($mb)" -ForegroundColor Green
Write-Host '    설치는 기존 설치 폴더에 덮어쓰기 — SDSMDB.sqlite · mbtiles\ 는 압축에 없으므로 그대로 남는다.' -ForegroundColor Yellow
Write-Host '    덮어쓰기 전에 설치 폴더를 백업한다(특히 SDSMDB.sqlite · sites.json).' -ForegroundColor Yellow
Start-Process explorer.exe -ArgumentList "/select,`"$zip`""
Wait-Enter '끝났습니다. Enter 를 누르면 창이 닫힙니다'
