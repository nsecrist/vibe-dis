# WSL Development Environment

This document covers setting up SisoDis.NET development in Windows Subsystem for Linux (WSL).

## Why WSL?

WSL provides a Linux development environment on Windows with:
- Native Linux toolchain for .NET development
- Full compatibility with .NET SDK and CLI tools
- Better performance for file I/O compared to traditional VM
- Seamless integration with Windows files and VS Code

## Prerequisites

- Windows 10 version 2004+ or Windows 11
- WSL 2 enabled
- Ubuntu 20.04+ installed via WSL

## Installation

### 1. Enable WSL

Run in PowerShell (as Administrator):

```powershell
wsl --install
```

Restart your computer when prompted.

### 2. Set Up Ubuntu

After restart, Ubuntu will open automatically. Create your user account:

```bash
# Enter your desired password when prompted
```

### 3. Run Setup Script

```bash
cd /mnt/c/repos/SisoDis.NET/dev-env/wsl
./setup-ubuntu.sh
```

### 4. Configure VS Code (Recommended)

Install VS Code extensions for WSL:

```bash
# In VS Code, install these extensions:
# - C# (Microsoft)
# - WSL (Microsoft)
# - GitHub Copilot (optional)
```

From WSL terminal:
```bash
code .
```

## Network Configuration for DIS

DIS applications use UDP multicast for network communication. WSL 2 has a different network topology than WSL 1.

### Recommended: Use WSL 1 for Network Apps

```bash
# Check current WSL version
wsl -l -v

# Convert to WSL 1 for better network compatibility
wsl --set-version Ubuntu 1
```

WSL 1 shares the Windows network stack, making multicast work seamlessly with Windows DIS applications.

### WSL 2 Network Bridge

If using WSL 2, you need to:

1. Allow multicast in Windows Firewall or disable it for testing
2. Use host network mode in Docker if applicable

## WSL Specific Tips

### File Access

```bash
# Access Windows files from WSL
cd /mnt/c/Users/YourName

# Access W files from Windows
# In File Explorer, type: \\wsl$\Ubuntu\home\username
```

### Clipboard Integration

```bash
# Copy to Windows clipboard
echo "text" | clip.exe

# Paste from Windows clipboard
powershell.exe -c "Get-Clipboard"
```

### Running Windows Executables

```bash
# Run Windows executables from WSL
/mnt/c/Path/To/Program.exe

# Or use wine for Windows executables
```

### Git Credential Caching

To avoid entering credentials constantly:

```bash
# Option 1: Use Windows Credential Manager
git config --global credential.helper "/mnt/c/Program\ Files/Git/mingw64/bin/git-credential-manager.exe"

# Option 2: Cache credentials (less secure)
git config --global credential.helper cache
```

## Common Issues

### Issue: dotnet command not found after installation

Add to `~/.bashrc`:

```bash
export PATH="$PATH:$HOME/.dotnet"
export DOTNET_ROOT="$HOME/.dotnet"
source ~/.bashrc
```

### Issue: Multicast not working in WSL 2

This is a known limitation. Solutions:

1. Use WSL 1 (recommended for DIS development)
2. Use a VPN that supports multicast
3. Run DIS receiver on Windows side

### Issue: File permissions

Windows files mounted to WSL may have permission issues:

```bash
# Fix git executable permissions
git config --global core.fileMode false

# Or remount with proper permissions
wsl --shutdown
wsl -e bash -c "umount /mnt/c && mount -t drvfs C: /mnt/c -o metadata"
```

## Development Workflow

### Recommended Setup

1. **Store code in Windows** - Edit in VS Code with WSL extension
2. **Build in WSL** - Use Linux toolchain for faster builds
3. **Test network apps** - Run receiver in WSL, producer in Windows

### Example Workflow

```bash
# In WSL terminal
cd /mnt/c/repos/SisoDis.NET
dotnet build

# In PowerShell (Windows)
cd C:\repos\SisoDis.NET
dotnet run --project SisoDis.Receiver

# Both will communicate via multicast on same network
```

## Additional WSL Resources

- [WSL Documentation](https://docs.microsoft.com/en-us/windows/wsl)
- [VS Code WSL Tutorial](https://code.visualstudio.com/docs/remote/wsl)
- [WSL GitHub Repository](https://github.com/microsoft/WSL)
