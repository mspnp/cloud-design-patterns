<#
.SYNOPSIS
    This is a Powershell script to create a build
.DESCRIPTION
    Usage: Run .openpublishing.build.ps1 -parameters:"_op_accessToken=<your Git repository access token>"
    Refer to https://opsdocs.azurewebsites.net/en-us/opsdocs/partnerdocs/local-build-and-preview?branch=master for more information.
.PARAMETER parameters
    Specifies optional paramerters.
    _op_accessToken: access token for your Git repository, optional if the repository is public.
#>

param(
    [string]$parameters
)

trap
{
    # Handle the error
    $err = $_.Exception
    Write-Error "Exception occurred. $err.Message" -ErrorAction Continue

    while($err.InnerException) {
        $err = $err.InnerException
        Write-Error "Inner exception: $err.Message" -ErrorAction Continue
    };

    # End the script.
    exit 1
}

# Main
$errorActionPreference = 'Stop'

$contextDictionary = @{}
$currentDictionary = @{
    environment = @{};
    docset = @{};
    context = @{}
}

# Entry-point package
$entryPointPackage = @{
    id = "opbuild.scripts";
    version = "latest";
    targetFramework = "net45";
}

# Pre-step: Set the repository root folder, working folder, packages folder and last op script version file path
$repositoryRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$workingDirectory = "$repositoryRoot\.optemp"
$packagesDirectory = "$workingDirectory\packages"
$lastOpScriptVersionRecordFile = "$workingDirectory\lastOpScriptVersion.txt";

Filter timestamp
{
    if (![string]::IsNullOrEmpty($_) -and ![string]::IsNullOrWhiteSpace($_))
    {
        Write-Host -NoNewline -ForegroundColor Magenta "[$(((get-date).ToUniversalTime()).ToString("HH:mm:ss.ffffffZ"))]: "
    }

    $_
}

echo "Running build core ps1 with parameters: $parameters" | timestamp

# Print current PowerShell environment version
echo "Current PowerShell environment version: $($PSVersionTable.PSVersion.ToString())" | timestamp

# Check PowerShell version, exit when below 4.0
if ($PSVersionTable.PSVersion.Major -lt 4)
{
    Write-Error "PowerShell version should be equal with or higher than 4.0, current PowerShell version is $PSVersionTable.PSVersion.Major"
}

# Define system value
$systemDefaultVariables = @{
    ResourceContainerUrl = "https://opbuildstorageprod.blob.core.windows.net/opps1container";
    DefaultEntryPoint = "op";
    UpdateNugetExe = $false;
    UpdateNugetConfig = $true;
    UpdateMdproj = $true;
    NeedBuildMdproj = $true;
    MdprojTargets = "build";
    OutputFolder = "$repositoryRoot\_site";
    LogOutputFolder = "$repositoryRoot\log";
    CacheFolder = "$workingDirectory\cache";
    LogLevel = "Info";
    NeedGeneratePdf = $false;
    NeedGenerateIntellisense = $false;
    UpdatePackagesConfig = $true;
    GlobalMetadataFile = "";
    EnvironmentResourcesFile = "";
    DefaultMaxRetryCount = 3;
    NeedFetchSubmodule = $true;
    DefaultSubmoduleBranch = "master";
    DownloadNugetExeTimeOutInSeconds= 300;
    DownloadNugetConfigTimeOutInSeconds= 30;
    GitCloneRepositoryTimeOutInSeconds = 600;
    GitPullRepositoryTimeOutInSeconds = 300;
    GitOtherOperationsTimeOutInSeconds = 60;
    BuildToolParallelism = 0;
    AllowUseDynamicRendering = $false;
    PreservedFallbackFolderRegex = "(?i)_repo\.(.+-.+)";
    PreservedTemplateFolders = @("_themes", "_themes.MSDN.Modern", "_themes.MSDN.PartnerCenter", "_themes.VS.Modern", "_themes.VS.UHF", "_themes.TechNet.Modern", "_themes.OPS");
    EnableIncrementalBuild = $false;
    ResolveUserProfile = $false;
    ResolveUserProfileByAccessToken = $true;
    PdfTemplateFolder = "";
    NeedGeneratePdfExternalLink = $false;
    PdfConvertParallelism = 4;
}

$systemDefaultTargets = @{
    build = @{
        NeedInit = $true;
        NeedBuild = $true;
        NeedServe = $false;
        NeedGenerateIntellisense = $false;
        NeedGeneratePdf = $false;
    };
    init = @{
        NeedInit = $true;
        NeedBuild = $false;
        NeedServe = $false;
        NeedGenerateIntellisense = $false;
        NeedGeneratePdf = $false;
    };
    localbuild = @{
        NeedInit = $false;
        NeedBuild = $true;
        NeedServe = $false;
        NeedGenerateIntellisense = $false;
        NeedGeneratePdf = $false;
    };
    serve = @{
        NeedInit = $true;
        NeedBuild = $true;
        NeedServe = $true;
        NeedGenerateIntellisense = $false;
        NeedGeneratePdf = $false;
    };
    generateIntellisense = @{
        NeedInit = $true;
        NeedBuild = $true;
        NeedServe = $false;
        NeedGenerateIntellisense = $true;
        NeedGeneratePdf = $false;
    };
    generatePdf = @{
        NeedInit = $true;
        NeedBuild = $true;
        NeedServe = $false;
        NeedGenerateIntellisense = $false;
        NeedGeneratePdf = $true;
    };
    sync = @{
        NeedInit = $true;
        NeedBuild = $false;
        NeedServe = $false;
        NeedGenerateIntellisense = $false;
        NeedGeneratePdf = $false;
    };
}

function Write-HostWithTimestamp([string]$output)
{
    Write-Host -NoNewline -ForegroundColor Magenta "[$(((get-date).ToUniversalTime()).ToString("HH:mm:ss.ffffffZ"))]: "
    Write-Host $output
}

Function ParseBoolValue([string]$variableName, [string]$stringValue, [bool]$defaultBoolValue)
{
    if([string]::IsNullOrEmpty($stringValue))
    {
        return $defaultBoolValue
    }

    try
    {
        $parsedBoolValue = [System.Convert]::ToBoolean($stringValue)
    }
    catch
    {
        Write-Error "variable $variableName does not have a valid bool value: $stringValue. Exception: $($_.Exception.Message)"
    }

    return $parsedBoolValue
}

Function GetValueFromVariableName([string]$variableValue, [string]$defaultStringValue)
{
    if([string]::IsNullOrEmpty($variableValue))
    {
        $variableValue = $defaultStringValue
    }
    return $variableValue
}

Function ParseParameters([string]$parameters)
{
    if([string]::IsNullOrEmpty($parameters))
    {
        return
    }

    $parameterPortions = $parameters.Split(';')
    foreach ($parameterPortion in $parameterPortions)
    {
        $keyValuePair = $parameterPortion.Split('=')
        if ($keyValuePair.Length -eq 2)
        {
            Set-Variable -Name $keyValuePair[0] -Value $keyValuePair[1] -Scope "Script" -Force
            Write-HostWithTimestamp "Create script scope variable with input $keyValuePair"
        }
        else
        {
            Write-HostWithTimestamp "Invalid variable with input $keyValuePair. Ignore it."
        }
    }
}

Function IsPathExists([string]$path)
{
    return Test-Path $path
}

Function CheckPath([string]$path)
{
    if(!(IsPathExists($path)))
    {
        Write-Error "$path doesn't exist"
    }
}

Function CreateFolderIfNotExists([string]$folder)
{
    if(!(Test-Path "$folder"))
    {
        New-Item "$folder" -ItemType Directory
    }
}

Function RetryCommand
{
    param (
        [Parameter(Mandatory=$true)][string]$command,
        [Parameter(Mandatory=$true)][hashtable]$args,
        [Parameter(Mandatory=$false)][int]$maxRetryCount = $systemDefaultVariables.DefaultMaxRetryCount,
        [Parameter(Mandatory=$false)][ValidateScript({$_ -ge 0})][int]$retryIncrementalIntervalInSeconds = 10
    )

    # Setting ErrorAction to Stop is important. This ensures any errors that occur in the command are 
    # treated as terminating errors, and will be caught by the catch block.
    $args.ErrorAction = "Stop"

    $currentRetryIteration = 1
    $retryIntervalInSeconds = 0

    Write-HostWithTimestamp ("Start to run command [{0}] with args [{1}]." -f $command, $($args | Out-String))
    do{
        try
        {
            Write-HostWithTimestamp "Calling iteration $currentRetryIteration"
            & $command @args

            Write-HostWithTimestamp "Command ['$command'] succeeded at iteration $currentRetryIteration."
            return
        }
        Catch
        {
            Write-HostWithTimestamp "Calling iteration $currentRetryIteration failed, exception: '$($_.Exception.Message)'"
        }

        if ($currentRetryIteration -ne $maxRetryCount)
        {
            $retryIntervalInSeconds += $retryIncrementalIntervalInSeconds
            Write-HostWithTimestamp "Command ['$command'] failed. Retrying in $retryIntervalInSeconds seconds."
            Start-Sleep -Seconds $retryIntervalInSeconds
        }
    } while (++$currentRetryIteration -le $maxRetryCount)

    Write-HostWithTimestamp "Command ['$command'] failed. Maybe the network issues, please retry the build later."
    exit 1
}

Function DownloadFile([string]$source, [string]$destination, [bool]$forceDownload, [int]$timeoutSec = -1)
{
    if($forceDownload -or !(IsPathExists($destination)))
    {
        Write-HostWithTimestamp "Download file to $destination from $source with force: $forceDownload"
        $destinationFolder = Split-Path -Parent $destination
        CreateFolderIfNotExists($destinationFolder)
        if ($timeoutSec -lt 0)
        {
            RetryCommand -Command 'Invoke-WebRequest' -Args @{ Uri = $source; OutFile = $destination; }
        }
        else
        {
            RetryCommand -Command 'Invoke-WebRequest' -Args @{ Uri = $source; OutFile = $destination; TimeoutSec = $timeoutSec }
        }
    }
}

Function FindResource(
    [object[]]$resources,
    [parameter(mandatory=$true)]
    [string]$resourceName,
    [string]$resourceVersion = $null
)
{
    foreach ($resource in $resources)
    {
        if ($resource.Name -eq $resourceName)
        {
            if ([string]::IsNullOrEmpty($resourceVersion) -or ($resource.Version -eq $resourceVersion))
            {
                return $resource
            }
        }
    }

    return $null
}

Function GetPackageLatestVersion([string]$nugetExeDestination, [string]$packageName, [string]$nugetConfigDestination, [int]$maxRetryCount, [bool]$usePrereleasePackage = $false, [object[]]$environmentResources = $null)
{
    $currentRetryIteration = 0;
    $retryIntervalInSeconds = 0;
    $retryIncrementalIntervalInSeconds = 10;

    do
    {
        Try
        {
            Write-HostWithTimestamp "Use prerelease package for $packageName : $usePrereleasePackage"

            $cachedPackageVersionString = "latest";
            if ($usePrereleasePackage)
            {
                $cachedPackageVersionString = "latest-prerelease"
            }

            $cachedPackageVersion = FindResource($cachedPackageVersions) ($packageName) ($cachedPackageVersionString)
            if ($cachedPackageVersion)
            {
                Write-HostWithTimestamp "Package version for $packageName loaded from cache: $cachedPackageVersion"
                return $cachedPackageVersion.Location
            }

            if ($usePrereleasePackage)
            {
                $filteredPackages = (& "$nugetExeDestination" list $packageName -Prerelease -ConfigFile "$nugetConfigDestination") -split "\n"
            }
            else
            {
                $filteredPackages = (& "$nugetExeDestination" list $packageName -ConfigFile "$nugetConfigDestination") -split "\n"
            }

            if ($LASTEXITCODE -eq 0)
            {
                foreach ($filteredPackage in $filteredPackages)
                {
                    $segments = $filteredPackage -split " "
                    if ($segments.Length -eq 2 -and $segments[0] -eq $packageName)
                    {
                        return $segments[1]
                    }
                }
            }

            Write-HostWithTimestamp "Call iteration '$currentRetryIteration', cannot find latest version for $packageName, filtered packages: $filteredPackages"
        }
        Catch
        {
            Write-HostWithTimestamp "Call iteration '$currentRetryIteration', cannot find latest version for $packageName, exception: $($_.Exception.Message)"
        }

        if ($currentRetryIteration -ne $maxRetryCount)
        {
            $retryIntervalInSeconds += $retryIncrementalIntervalInSeconds
            Write-HostWithTimestamp "List package version failed, sleep $retryIntervalInSeconds seconds..."
            Start-Sleep -Seconds $retryIntervalInSeconds
        }
    } while (++$currentRetryIteration -le $maxRetryCount)

    Write-HostWithTimestamp "Current nuget package list service is busy, please retry the build in 10 minutes"
    exit 1
}

Function RestorePackage([string] $nugetExeDestination, [string]$packagesDestination, [string]$packagesDirectory, [string]$nugetConfigDestination)
{
    Try
    {
        & "$nugetExeDestination" restore "$packagesDestination" -PackagesDirectory "$packagesDirectory" -ConfigFile "$nugetConfigDestination"
        return $LASTEXITCODE -eq 0
    }
    Catch
    {
        return $false;
    }
}

Function GeneratePackagesConfig([string]$outputFilePath, [object[]]$dependencies, [string]$nugetExeDestination, [object[]]$cachedPackageVersions = $null)
{
    $packageConfigXmlTemplate = @'
<?xml version="1.0" encoding="utf-8"?>
<packages></packages>
'@

    $packageConfigXml = [xml]$packageConfigXmlTemplate
    foreach ($dependency in $dependencies)
    {
        $packageNode = $packageConfigXml.CreateElement("package")
        $packageNode.SetAttribute("id", $dependency.id)
        
        if ($dependency.version -eq "latest" -or $dependency.version -eq "latest-prerelease")
        {
            $usePrereleasePackage = $dependency.version -eq "latest-prerelease"

            # Get latest package version
            $dependency.actualVersion = GetPackageLatestVersion($nugetExeDestination) ($dependency.id) ($nugetConfigDestination) ($systemDefaultVariables.DefaultMaxRetryCount) ($usePrereleasePackage) ($environmentResources)

            Write-HostWithTimestamp "Using version $($dependency.actualVersion) for package $($dependency.id) (requested: $($dependency.version))"
        }
        else
        {
            $dependency.actualVersion = $dependency.version
        }
        $packageNode.SetAttribute("version", $dependency.actualVersion)

        $packageNode.SetAttribute("targetFramework", $dependency.targetFramework)
        $packageConfigXml.SelectSingleNode("packages").AppendChild($packageNode)
    }
    
    if (IsPathExists($outputFilePath))
    {
        Remove-Item $outputFilePath -Force
    }
    $packageConfigXml.Save($outputFilePath)
}

Function ConvertToJsonSafely {
    param([string]$content)
    process { $_ | ConvertTo-Json -Depth 99 }
}

Function CloneParameterDictionaryWithContext
{
    param (
        [parameter(mandatory=$true)]
        [hashtable]$currentDictionary, 
        [parameter(mandatory=$true)]
        [hashtable]$context
    )

    $ParameterDictionary = $currentDictionary.Clone()
    $ParameterDictionary.context = $context

    return $ParameterDictionary
}

# Step-1: Parse parameters
echo "Default system value:" $systemDefaultVariables | timestamp
echo "Parse parameters $parameters" | timestamp
ParseParameters($parameters)

# Step-2: Parse publish configuration
$publishConfigFile = "$repositoryRoot\.openpublishing.publish.config.json"
CheckPath($publishConfigFile)
$publishConfigContent = (Get-Content $publishConfigFile -Raw) | ConvertFrom-Json

# Get config package information
echo "Create packages.config for entry-point package" | timestamp
$configPackageVersion = $publishConfigContent.package_version
if (![string]::IsNullOrEmpty($configPackageVersion))
{
    $entryPointPackage.version = $configPackageVersion
}

# Retrieve targets from system predefined targets
$Targets = GetValueFromVariableName($Targets) ($systemDefaultVariables.MdprojTargets)

if (-Not $systemDefaultTargets.ContainsKey("$Targets"))
{
    Throw [System.NotSupportedException] "Targets $Targets is not supported"
}

$currentTargets = $systemDefaultTargets.$Targets

# Pass user input for generate pdf target if need to resolve external link.
if ($Targets -eq 'generatePdf')
{
    $NeedResolveLink = GetValueFromVariableName($NeedResolveLink) ($false)
    if ($NeedResolveLink -eq $true)
    {
        $systemDefaultVariables.NeedGeneratePdfExternalLink = $true
        foreach ($docset in $publishConfigContent.docsets_to_publish)
        {
            $hostUrl = Read-Host "Please enter $($docset.docset_name)'s host url:"
            $basePath = Read-Host "Please enter $($docset.docset_name)'s base path:"

            $currentDictionary.environment.pdfParameters = @{}
            $currentDictionary.environment.pdfParameters.$($docset.docset_name) = @{}
            $currentDictionary.environment.pdfParameters.$($docset.docset_name).hostUrl = $hostUrl
            $currentDictionary.environment.pdfParameters.$($docset.docset_name).basePath = $basePath
        }
    }
}

# Step-3: Parse environment resources
$EnvironmentResourcesFile = GetValueFromVariableName($EnvironmentResourcesFile) ($systemDefaultVariables.EnvironmentResourcesFile)

$environmentResources = @{}
if (![string]::IsNullOrEmpty($EnvironmentResourcesFile) -and (IsPathExists($EnvironmentResourcesFile)))
{
    $environmentResources = (Get-Content $EnvironmentResourcesFile -Raw) | ConvertFrom-Json
}

$nugetConfigDestination = "$workingDirectory\Tools\Nuget\Nuget.Config"
$nugetExeDestination = "$workingDirectory\Tools\Nuget\nuget.exe"

if ($currentTargets.NeedInit -eq $true)
{
    # Step-3: Download Nuget tools and nuget config
    echo "Download Nuget tool and config" | timestamp
    $resourceContainerUrl = GetValueFromVariableName($resourceContainerUrl) ($systemDefaultVariables.ResourceContainerUrl)
    # Trace one log to tell user currently it's internal environment.
    if ($resourceContainerUrl.StartsWith("https://opbuildstorageprod.blob.core.windows.net"))
    {
        echo "Build within internal environment."
    }
    $nugetConfigSource = "$resourceContainerUrl/Tools/Nuget/Nuget.Config"
    $nugetExeSource = "$resourceContainerUrl/Tools/Nuget/nuget.exe"

    $DownloadNugetExeTimeOutInSeconds = GetValueFromVariableName($DownloadNugetExeTimeOutInSeconds) ($systemDefaultVariables.DownloadNugetExeTimeOutInSeconds)
    $DownloadNugetConfigTimeOutInSeconds = GetValueFromVariableName($DownloadNugetConfigTimeOutInSeconds) ($systemDefaultVariables.DownloadNugetConfigTimeOutInSeconds)
    $UpdateNugetExe = ParseBoolValue("UpdateNugetExe") ($UpdateNugetExe) ($systemDefaultVariables.UpdateNugetExe)
    DownloadFile($nugetExeSource) ($nugetExeDestination) ($UpdateNugetExe) ($DownloadNugetExeTimeOutInSeconds)
    $UpdateNugetConfig = ParseBoolValue("UpdateNugetConfig") ($UpdateNugetConfig) ($systemDefaultVariables.UpdateNugetConfig)
    DownloadFile($nugetConfigSource) ($nugetConfigDestination) ($UpdateNugetConfig) ($DownloadNugetConfigTimeOutInSeconds)

    # Step-5: Create packages.config for entry-point package. For non-PROD env, treat latest version as latest-prerelease version by default
    $treatLatestVersionAsLatestPrereleaseVersion = !$resourceContainerUrl.StartsWith("https://opbuildstorageprod.blob.core.windows.net")
    if ($_op_treatLatestVersionAsLatestPrereleaseVersion)
    {
        $treatLatestVersionAsLatestPrereleaseVersion = $_op_treatLatestVersionAsLatestPrereleaseVersion -eq "true"
    }

    if ($treatLatestVersionAsLatestPrereleaseVersion -and $entryPointPackage.version -eq "latest")
    {
        $entryPointPackage.version = "latest-prerelease"
        echo "Use latest-prerelease version instead of latest version." | timestamp
    }

    $packagesDestination = "$workingDirectory\packages.config"
    GeneratePackagesConfig($packagesDestination) (@($entryPointPackage)) ($nugetExeDestination) ($environmentResources.PackageVersion)

    # Step-6: Restore entry-point package
    echo "Restore entry-point package: $($entryPointPackage.id)" | timestamp
    $restoreSucceeded = RestorePackage($nugetExeDestination) ($packagesDestination) ($packagesDirectory) ($nugetConfigDestination)
    if (!$restoreSucceeded)
    {
        echo "Restore entry-point package failed" | timestamp
        exit 1
    }
}
else
{
    if(!(Test-Path "$lastOpScriptVersionRecordFile"))
    {
        echo "Please run a non local build at first to restore the necessary packages and files."
        exit 1;
    }

    # TODO: check whether the actual version has a legal version stype
    $entryPointPackage.actualVersion = (Get-Content $lastOpScriptVersionRecordFile -Raw).Trim()
}

# Step-6: Call build entry point
$packageToolsDirectory = "$packagesDirectory\$($entryPointPackage.id).$($entryPointPackage.actualVersion)\tools"
$buildEntryPointDestination = "$packageToolsDirectory\build.entrypoint.ps1"

$currentDictionary.environment.repositoryRoot = $repositoryRoot
$currentDictionary.environment.workingDirectory = $workingDirectory
$currentDictionary.environment.systemDefaultVariables = $systemDefaultVariables
$currentDictionary.environment.packagesDirectory = $packagesDirectory
$currentDictionary.environment.nugetConfigDestination = $nugetConfigDestination
$currentDictionary.environment.nugetExeDestination = $nugetExeDestination
$currentDictionary.environment.currentTargets = $currentTargets
$currentDictionary.environment.LastOpScriptVersion = $entryPointPackage.actualVersion
$currentDictionary.environment.LastOpScriptVersionRecordFile = $lastOpScriptVersionRecordFile
$currentDictionary.environment.EnvironmentResources = $environmentResources
$currentDictionary.environment.enableIncrementalBuild = ParseBoolValue("EnableIncrementalBuild") ($EnableIncrementalBuild) ($systemDefaultVariables.EnableIncrementalBuild)
$currentDictionary.environment.pdfTemplateFolder = GetValueFromVariableName($PdfTemplateFolder) ($systemDefaultVariables.PdfTemplateFolder)
$currentDictionary.environment.needGeneratePdfExternalLink = ParseBoolValue("NeedGeneratePdfExternalLink") ($NeedGeneratePdfExternalLink) ($systemDefaultVariables.NeedGeneratePdfExternalLink)
$currentDictionary.environment.pdfConvertParallelism = GetValueFromVariableName($PdfConvertParallelism) ($systemDefaultVariables.PdfConvertParallelism)

$AllowUseDynamicRendering = ParseBoolValue("AllowUseDynamicRendering") ($AllowUseDynamicRendering) ($systemDefaultVariables.AllowUseDynamicRendering)
echo "Allow use of dynamic rendering: $AllowUseDynamicRendering" | timestamp
$currentDictionary.environment.docfxAllowUseDynamicRendering = $AllowUseDynamicRendering

$ParameterDictionary = CloneParameterDictionaryWithContext($currentDictionary) ($contextDictionary)

echo "Call build entry point at $buildEntryPointDestination" | timestamp
& "$buildEntryPointDestination" $ParameterDictionary

exit $LASTEXITCODE
