# Git CMD Command Reference

This document provides examples of common Git CMD commands that should be used instead of Linux/Mac commands when working with the Prosim2GSX project.

## File System Operations

### Directory Navigation and Listing

| Linux/Mac Command | Git CMD Equivalent | Description |
|-------------------|----------------------|-------------|
| `ls` | `dir` | List directory contents |
| `cd directory` | `cd directory` | Change directory |
| `pwd` | `cd` | Show current directory |
| `mkdir directory` | `mkdir directory` | Create directory |
| `rm file` | `del file` | Remove file |
| `rm -r directory` | `rmdir /s /q directory` | Remove directory recursively |
| `cp source destination` | `copy source destination` | Copy file |
| `cp -r source destination` | `xcopy source destination /s /e /i` | Copy directory recursively |
| `mv source destination` | `move source destination` | Move file or directory |
| `touch file` | `type nul > file` or `echo.> file` | Create empty file |
| `cat file` | `type file` | Display file contents |

### File Searching

| Linux/Mac Command | Git CMD Equivalent | Description |
|-------------------|----------------------|-------------|
| `find . -name "*.cs"` | `dir *.cs /s /b` | Find files by name |
| `grep "pattern" file` | `findstr "pattern" file` | Search for pattern in file |
| `grep -r "pattern" .` | `findstr /s "pattern" *.*` | Search recursively for pattern |

## Process Management

| Linux/Mac Command | Git CMD Equivalent | Description |
|-------------------|----------------------|-------------|
| `ps` | `tasklist` | List processes |
| `kill pid` | `taskkill /PID pid` | Kill process by ID |
| `killall name` | `taskkill /IM name` | Kill process by name |

## Network Operations

| Linux/Mac Command | Git CMD Equivalent | Description |
|-------------------|----------------------|-------------|
| `ping host` | `ping host` | Ping a host |
| `curl url` | `curl url` (if curl installed) or use PowerShell for this | Make HTTP request |
| `wget url` | Use PowerShell for this | Download file |
| `netstat` | `netstat` | Show network connections |

## File Path Conventions

### Path Separators
- Linux/Mac: Forward slash (`/`)
- Windows: Backslash (`\`)

### Root Directories
- Linux/Mac: `/` (root)
- Windows: Drive letters (`C:\`, `D:\`, etc.)

### Home Directory
- Linux/Mac: `~` or `$HOME`
- Windows: `%USERPROFILE%`

### Path Examples

| Linux/Mac Path | Windows/Git CMD Path |
|----------------|------------------------|
| `/usr/local/bin` | `C:\Program Files\` |
| `~/Documents` | `%USERPROFILE%\Documents` |
| `./relative/path` | `.\relative\path` |
| `../parent` | `..\parent` |

## Command Execution

### Running Commands

| Linux/Mac Style | Git CMD Style | Description |
|-----------------|-----------------|-------------|
| `command` | `command` | Run command |
| `command &` | `start command` | Run command in background |
| `command1 && command2` | `command1 && command2` | Run commands sequentially |
| `command1 \|\| command2` | `command1 || command2` | Run command2 if command1 fails |
| `command1 \| command2` | `command1 | command2` | Pipe output of command1 to command2 |

### Command Output Redirection

| Linux/Mac Style | Git CMD Style | Description |
|-----------------|-----------------|-------------|
| `command > file` | `command > file` | Redirect output to file |
| `command >> file` | `command >> file` | Append output to file |
| `command 2> file` | `command 2> file` | Redirect error output to file |
| `command 2>&1` | `command 2>&1` | Redirect all output to file |

## .NET and C# Development

### Building and Running

| Linux/Mac Command | Git CMD Equivalent | Description |
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

| Linux/Mac Style | Git CMD Style | Description |
|-----------------|-----------------|-------------|
| `export VAR=value` | `set VAR=value` | Set environment variable |
| `echo $VAR` | `echo %VAR%` | Display environment variable |
| `VAR=value command` | `set VAR=value && command` | Set variable for single command |

## Script Execution

| Linux/Mac Style | Git CMD Style | Description |
|-----------------|-----------------|-------------|
| `./script.sh` | `script.bat` or `script.cmd` | Run script in current directory |
| `bash script.sh` | `call script.bat` | Run script with explicit call |
| `sh -c "command"` | `cmd /c "command"` | Execute command string |

## Examples for Prosim2GSX Development

### Building the Project
```cmd
:: Build the solution
msbuild Prosim2GSX.sln

:: Build with specific configuration
msbuild Prosim2GSX.sln /p:Configuration=Release
```

### Running the Application
```cmd
:: Run the application from build output
start .\Prosim2GSX\bin\Debug\net8.0-windows10.0.17763.0\Prosim2GSX.exe
```

### Managing Files
```cmd
:: Copy DLLs to output directory
copy .\Prosim2GSX\lib\*.dll .\Prosim2GSX\bin\Debug\net8.0-windows10.0.17763.0\

:: Create backup of configuration file
copy .\Prosim2GSX\bin\Debug\net8.0-windows10.0.17763.0\config.xml .\Prosim2GSX\bin\Debug\net8.0-windows10.0.17763.0\config.xml.bak
```

### Searching Code
```cmd
:: Find all TODO comments in C# files
findstr /s "TODO" *.cs

:: Find all usages of a specific class
findstr /s "GsxController" *.cs
```

### Git Operations
```cmd
:: Check git status
git status

:: Create a new branch
git checkout -b feature/new-feature

:: Commit changes
git add .
git commit -m "Add new feature"
```

## Command Execution Notes

When executing commands in the Prosim2GSX project, always:

1. Use Git CMD syntax and conventions
2. Use Windows path conventions with backslashes
3. Handle spaces in paths properly with quotes: `"C:\Program Files\Example"`
4. Use comments with `::` instead of `#` for batch files
5. Remember that environment variables use `%VARIABLE%` syntax

## References

- [Windows Command Line Reference](https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/windows-commands)
- [Git for Windows Documentation](https://git-scm.com/book/en/v2/Appendix-A%3A-Git-in-Other-Environments-Git-in-Bash)
- [.NET CLI Documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/)
- [MSBuild Documentation](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-reference)
