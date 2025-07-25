@{
    # Script module or binary module file associated with this manifest.
    RootModule = 'Svrooij.BetterGraph.dll'

    # Version number of this module.
    ModuleVersion = '0.1.0'

    # ID used to uniquely identify this module.
    GUID = 'a1da9b7e-5dda-2807-ae11-1986fcf03614'

    # Author of this module.
    Author = 'Stephan van Rooij (@svrooij)'

    # Company or vendor that produced this module.
    CompanyName = 'Stephan van Rooij'

    Copyright = 'Stephan van Rooij 2025, licensed under MIT License (MIT)'

    # Description of this module.
    Description = 'A faster Graph module'

    # Minimum version of the Windows PowerShell engine required by this module.
    # This module is build on net8.0 which requires PowerShell 7.4 (anything below is not supported).
    PowerShellVersion = '7.4'

    # Minimum version of the .NET Framework required by this module.
    # DotNetFrameworkVersion = '4.7.2'

    # Processor architecture (None, X86, Amd64) supported by this module.
    # ProcessorArchitecture = 'None'

    # Modules that must be imported into the global environment prior to importing this module.
    # RequiredModules = @()

    # Assemblies that must be loaded prior to importing this module.
    # RequiredAssemblies = @(
    #     "Microsoft.Extensions.Logging.Abstractions.dll",
    #     "System.Buffers.dll",
    #     "System.Memory.dll",
    #     "System.Numerics.Vectors.dll",
    #     "System.Runtime.CompilerServices.Unsafe.dll"
    # )

    # Script files (.ps1) that are run in the caller's environment prior to importing this module.
    # ScriptsToProcess = @()

    # Type files (.ps1xml) that are loaded into the session prior to importing this module.
    # TypesToProcess = @()

    # Format files (.ps1xml) that are loaded into the session prior to importing this module.
    # FormatsToProcess = @()

    # Modules to import as nested modules of the module specified in RootModule/ModuleToProcess.
    # NestedModules = @()

    # Functions to export from this module.
    # FunctionsToExport = @()

    # Cmdlets to export from this module.
    # CmdletsToExport = @(
    # )

    # Variables to export from this module.
    # VariablesToExport = @()

    # Aliases to export from this module.
    # AliasesToExport = @()

    # List of all files included in this module.
    FileList = @(
        "Svrooij.BetterGraph.dll",
        "Svrooij.BetterGraph.dll-Help.xml",
        "Svrooij.BetterGraph.psd1",
        "Svrooij.BetterGraph.psm1"
    )

    # Private data to pass to the module specified in RootModule/ModuleToProcess.
    PrivateData = @{
        PSData = @{
            Tags = @('Microsoft', 'Graph', 'Faster')

            LisenceUri = 'https://github.com/svrooij/BetterGraph-PowerShell?tab=MIT-1-ov-file'
            ProjectUri = 'https://github.com/svrooij/BetterGraph-PowerShell'
            ReleaseNotes = 'This module is still a work-in-progress. Changes might be made without notice.'
        }
    }

    # HelpInfo URI of this module.
    HelpInfoURI = 'https://github.com/svrooij/BetterGraph-PowerShell'
}
