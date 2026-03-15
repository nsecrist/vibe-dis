# Developer Environment Setup

This directory contains scripts and documentation for setting up development environments to work on SisoDis.NET.

## Overview

SisoDis.NET is a C# project targeting .NET 10+ that implements the IEEE 1278.1-2012 DIS protocol. This directory provides automated setup scripts for common development environments.

## Directory Structure

```
dev-env/
├── README.md              # This file
└── wsl/
    ├── README.md          # WSL-specific documentation
    └── setup-ubuntu.sh    # Ubuntu/WSL setup script
```

## Quick Start

### For WSL / Ubuntu Users

1. Run the setup script:
   ```bash
   cd dev-env/wsl
   ./setup-ubuntu.sh
   ```

2. Clone the repository:
   ```bash
   git clone https://github.com/nsecrist/vibe-dis
   cd SisoDis.NET
   ```

3. Build and test:
   ```bash
   dotnet restore
   dotnet build
   dotnet test
   ```

## What Gets Installed

The setup script installs the following tools:

### Required Tools

| Tool | Version | Purpose |
|------|---------|---------|
| **.NET SDK** | 10 | Build and run C# applications |
| **Git** | Latest | Version control |
| **Build Essentials** | System | Compiler toolchain |

### Optional Tools

| Tool | Purpose |
|------|---------|
| **OpenCode** | AI assistant for development (manual installation required) |

## Script Options

The `setup-ubuntu.sh` script supports the following options:

```bash
./setup-ubuntu.sh [OPTIONS]

Options:
  --dry-run       Preview what would be installed without making changes
  --skip-git      Skip git configuration prompts
  --dotnet-ver N  Specify .NET version (default: 10)
  -h, --help      Show help message
```

### Examples

```bash
# Preview installation
./setup-ubuntu.sh --dry-run

# Install specific .NET version
./setup-ubuntu.sh --dotnet-ver 9

# Skip git configuration
./setup-ubuntu.sh --skip-git
```

## Environment Variables

You can customize the installation using environment variables:

| Variable | Default | Description |
|----------|---------|-------------|
| `DOTNET_VERSION` | 10 | .NET SDK version to install |
| `OPENCODE_VERSION` | latest | OpenCode version (if auto-install available) |

Example:
```bash
DOTNET_VERSION=10 ./setup-ubuntu.sh
```

## Manual Installation

If you prefer to install tools manually, here's what you need:

### .NET SDK 10 (Ubuntu 22.04+)

```bash
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install dotnet-sdk-10.0
```

### Build Essentials

```bash
sudo apt install build-essential curl wget git vim unzip x-utils zip libssl-dev libffi-dev
```

## Troubleshooting

### dotnet command not found

If .NET was installed but `dotnet` isn't in your PATH, add it:

```bash
echo 'export PATH="$PATH:$HOME/.dotnet"' >> ~/.bashrc
source ~/.bashrc
```

### WSL Network Issues

If running in WSL and experiencing network issues with multicast (used by DIS):

```bash
# Check WSL version
wsl --version

# Update WSL kernel
wsl --update
```

### Permission Errors

If you encounter permission errors, ensure you're not running as root unnecessarily:

```bash
# Check current user
whoami

# If root, create a regular user
sudo adduser developer
sudo usermod -aG sudo developer
su - developer
```

## Development Workflow

After setup, typical development workflow:

```bash
# Pull latest changes
git pull origin master

# Build the solution
dotnet build

# Run tests
dotnet test

# Run a specific project
dotnet run --project SisoDis.ConsoleProducer
```

## Additional Tools

Consider installing these tools for an enhanced development experience:

- **VS Code** or **Rider** - IDE with C# support
- **tmux** - Terminal multiplexer (used by test-integration.sh)
- **GitHub CLI** - `gh` for PR management

See [WSL README](./wsl/README.md) for WSL-specific recommendations.

## Support

- For .NET issues: https://github.com/dotnet/core
- For SisoDis.NET issues: https://github.com/nsecrist/vibe-dis
- For WSL issues: https://docs.microsoft.com/en-us/windows/wsl
