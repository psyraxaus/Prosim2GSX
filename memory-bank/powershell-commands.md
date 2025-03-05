# PowerShell Command Reference

This document provides examples of common PowerShell commands that should be used instead of Linux/Mac commands when working with the Prosim2GSX project.

## File System Operations

### Directory Navigation and Listing

| Linux/Mac Command | PowerShell Equivalent | Description |
|-------------------|----------------------|-------------|
| `ls` | `Get-ChildItem` or `dir` | List directory contents |
| `cd directory` | `Set-Location directory` or `cd directory` | Change directory |
| `pwd` | `Get-Location` or `pwd` | Show current directory |
| `mkdir directory` | `New-Item -ItemType Directory -Path directory` or `mkdir directory` | Create directory |
| `rm file` | `Remove-Item file` | Remove file |
| `rm -r directory` | `Remove-Item -Recurse directory` | Remove directory recursively |
| `cp source destination` | `Copy-Item source destination` | Copy file or directory |
| `mv source destination` | `Move-Item source destination` | Move file or directory |
| `touch file` | `New-Item -ItemType File -Path file` | Create empty file |
| `cat file` | `Get-Content file` | Display file contents |

### File Searching

| Linux/Mac Command | PowerShell Equivalent | Description |
|-------------------|----------------------|-------------|
| `find . -name "*.cs"` | `Get-ChildItem -Path . -Filter *.cs -Recurse` | Find files by name |
| `grep "pattern" file` | `Select-String -Pattern "pattern" -Path file` | Search for pattern in file |
| `grep -r "pattern" .` | `Get-ChildItem -Recurse | Select-String -Pattern "pattern"` | Search recursively for pattern |

## Process Management

| Linux/Mac Command | PowerShell Equivalent | Description |
|-------------------|----------------------|-------------|
| `ps` | `Get-Process` | List processes |
| `kill pid` | `Stop-Process -Id pid` | Kill process by ID |
| `killall name` | `Stop-Process -Name name` | Kill process by name |

## Network Operations

| Linux/Mac Command | PowerShell Equivalent | Description |
|-------------------|----------------------|-------------|
| `ping host` | `Test-Connection host` | Ping a host |
| `curl url` | `Invoke-WebRequest -Uri url` | Make HTTP request |
| `wget url` | `Invoke-WebRequest -Uri url -OutFile filename` | Download file |
| `netstat` | `Get-NetTCPConnection` | Show network connections |

## File Path Conventions

### Path Separators
- Linux/Mac: Forward slash (`/`)
- Windows: Backslash (`\`) or escaped backslash in strings (`\\`)

### Root Directories
- Linux/Mac: `/` (root)
- Windows: Drive letters (`C:\`, `D:\`, etc.)

### Home Directory
- Linux/Mac: `~` or `$HOME`
- Windows: `$env:USERPROFILE` or `$HOME`

### Path Examples

| Linux/Mac Path | Windows/PowerShell Path |
|----------------|------------------------|
| `/usr/local/bin` | `C:\Program Files\` |
| `~/Documents` | `$env:USERPROFILE\Documents` |
| `./relative/path` | `.\relative\path` |
| `../parent` | `..\parent` |

## Command Execution

### Running Commands

| Linux/Mac Style | PowerShell Style | Description |
|-----------------|-----------------|-------------|
| `command` | `command` | Run command |
| `command &` | `Start-Process command` | Run command in background |
| `command1 && command2` | `command1; command2` | Run commands sequentially |
| `command1 \|\| command2` | `command1 -or command2` | Run command2 if command1 fails |
| `command1 \| command2` | `command1 \| command2` | Pipe output of command1 to command2 |

### Command Output Redirection

| Linux/Mac Style | PowerShell Style | Description |
|-----------------|-----------------|-------------|
| `command > file` | `command > file` or `command | Out-File file` | Redirect output to file |
| `command >> file` | `command >> file` or `command | Out-File file -Append` | Append output to file |
| `command 2> file` | `command 2> file` | Redirect error output to file |
| `command 2>&1` | `command 2>&1` or `command *> file` | Redirect all output to file |

## .NET and C# Development

### Building and Running

| Linux/Mac Command | PowerShell Equivalent | Description |
|-------------------|----------------------|-------------|
| `dotnet build` | `dotnet build` | Build .NET project |
| `dotnet run` | `dotnet run` | Run .NET project |
| `dotnet test` | `dotnet test` | Run tests |
| `dotnet publish` | `dotnet publish` | Publish application |
| `dotnet add package` | `dotnet add package` | Add NuGet package |

### MSBuild Commands

| Command | Description |
|---------|-------------|
| `msbuild Prosim2GSX.sln` | Build solution |
| `msbuild Prosim2GSX.sln /p:Configuration=Release` | Build solution in Release configuration |
| `msbuild Prosim2GSX.sln /t:Clean` | Clean solution |

## Environment Variables

| Linux/Mac Style | PowerShell Style | Description |
|-----------------|-----------------|-------------|
| `export VAR=value` | `$env:VAR = "value"` | Set environment variable |
| `echo $VAR` | `$env:VAR` or `Write-Output $env:VAR` | Display environment variable |
| `VAR=value command` | `$env:VAR = "value"; command` | Set variable for single command |

## Script Execution

| Linux/Mac Style | PowerShell Style | Description |
|-----------------|-----------------|-------------|
| `./script.sh` | `.\script.ps1` | Run script in current directory |
| `bash script.sh` | `powershell -File script.ps1` | Run script with explicit interpreter |
| `sh -c "command"` | `powershell -Command "command"` | Execute command string |

## Examples for Prosim2GSX Development

### Building the Project
```powershell
# Build the solution
msbuild Prosim2GSX.sln

# Build with specific configuration
msbuild Prosim2GSX.sln /p:Configuration=Release
```

### Running the Application
```powershell
# Run the application from build output
Start-Process .\Prosim2GSX\bin\Debug\net8.0-windows10.0.17763.0\Prosim2GSX.exe
```

### Managing Files
```powershell
# Copy DLLs to output directory
Copy-Item .\Prosim2GSX\lib\*.dll .\Prosim2GSX\bin\Debug\net8.0-windows10.0.17763.0\

# Create backup of configuration file
Copy-Item .\Prosim2GSX\bin\Debug\net8.0-windows10.0.17763.0\config.xml .\Prosim2GSX\bin\Debug\net8.0-windows10.0.17763.0\config.xml.bak
```

### Searching Code
```powershell
# Find all TODO comments in C# files
Get-ChildItem -Path . -Filter *.cs -Recurse | Select-String -Pattern "TODO"

# Find all usages of a specific class
Get-ChildItem -Path . -Filter *.cs -Recurse | Select-String -Pattern "GsxController"
```

### Git Operations
```powershell
# Check git status
git status

# Create a new branch
git checkout -b feature/new-feature

# Commit changes
git add .
git commit -m "Add new feature"
```

## Command Execution Notes

When executing commands in the Prosim2GSX project, always:

1. Use PowerShell syntax and conventions
2. Prefix commands with `powershell -Command` when executing from external tools
3. Use Windows path conventions with backslashes
4. Use PowerShell cmdlets when available instead of aliases or cmd.exe commands
5. Handle spaces in paths properly with quotes: `"C:\Program Files\Example"`

## References

- [PowerShell Documentation](https://docs.microsoft.com/en-us/powershell/)
- [PowerShell Command Reference](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/?view=powershell-7.2)
- [.NET CLI Documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/)
- [MSBuild Documentation](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-reference)
