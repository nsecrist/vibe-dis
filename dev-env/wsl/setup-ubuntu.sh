#!/bin/bash
#
# SisoDis.NET Developer Environment Setup Script
# ===============================================
# This script sets up a fresh Ubuntu environment (WSL or native) with
# all tools needed to develop and build SisoDis.NET.
#
# Requirements: Ubuntu 20.04+ or WSL with Ubuntu 20.04+
#
# Usage:
#   ./setup-ubuntu.sh           # Interactive mode (recommended)
#   ./setup-ubuntu.sh --dry-run # Show what would be installed
#   ./setup-ubuntu.sh --skip-git # Skip git configuration
#
# What this script installs:
#   - .NET 10 SDK
#   - OpenCode AI Assistant
#   - Build essentials and required dependencies
#
# After running this script:
#   1. Clone the repository: git clone <repo-url>
#   2. Run: dotnet restore
#   3. Run: dotnet build
#

set -euo pipefail

# ============================================================================
# Configuration
# ============================================================================

DOTNET_VERSION="${DOTNET_VERSION:-10}"

# OpenCode configuration
OPENCODE_VERSION="${OPENCODE_VERSION:-latest}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# ============================================================================
# Functions
# ============================================================================

log_info() {
    echo -e "${BLUE}[INFO]${NC} $*"
}

log_success() {
    echo -e "${GREEN}[OK]${NC} $*"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $*"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $*"
}

# Check if running as root or with sudo
check_privileges() {
    if [[ $EUID -eq 0 ]]; then
        SUDO=""
    elif command -v sudo &> /dev/null; then
        SUDO="sudo"
    else
        log_error "This script requires root privileges or sudo installed."
        exit 1
    fi
}

# Detect OS version
detect_os() {
    if [[ -f /etc/os-release ]]; then
        source /etc/os-release
        OS_NAME="$NAME"
        OS_VERSION="$VERSION_ID"
    else
        log_error "Cannot detect OS version"
        exit 1
    fi
    
    log_info "Detected: $OS_NAME $OS_VERSION"
    
    # Check if Ubuntu
    if [[ "$ID" != "ubuntu" ]]; then
        log_warn "This script is optimized for Ubuntu. Detected: $ID"
        read -p "Continue anyway? (y/n) " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            exit 0
        fi
    fi
    
    # Check version (20.04+ required)
    if [[ "${OS_VERSION%%.*}" -lt 20 ]]; then
        log_error "Ubuntu 20.04 or later is required. Detected: $OS_VERSION"
        exit 1
    fi
}

# Update package lists
update_packages() {
    log_info "Updating package lists..."
    $SUDO apt-get update -qq
    log_success "Package lists updated"
}

install_build_essentials() {
    log_info "Installing build essentials..."
    
    local packages=(
        "build-essential"
        "curl"
        "wget"
        "git"
        "vim"
        "unzip"
        "xz-utils"
        "zip"
        "libssl-dev"
        "libffi-dev"
        "libstdc++-12-dev"
        "tmux"
    )
    
    $SUDO apt-get install -y "${packages[@]}" -qq
    log_success "Build essentials installed"
}

install_dotnet() {
    log_info "Installing .NET SDK $DOTNET_VERSION..."
    
    if command -v dotnet &> /dev/null; then
        local installed_version
        installed_version=$(dotnet --version 2>/dev/null || echo "unknown")
        log_info ".NET SDK already installed: version $installed_version"
        
        if dotnet --list-sdks 2>/dev/null | grep -q "^${DOTNET_VERSION}\."; then
            log_success ".NET SDK $DOTNET_VERSION is already installed"
            return 0
        fi
    fi
    
    $SUDO apt-get install -y -qq wget apt-transport-https
    
    local wget_opts="--quiet -O /tmp/packages-microsoft-prod.deb"
    
    if [[ "$OS_VERSION" == "20.04" ]]; then
        $SUDO wget $wget_opts https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb
    elif [[ "$OS_VERSION" == "22.04" ]]; then
        $SUDO wget $wget_opts https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
    elif [[ "$OS_VERSION" == "24.04" ]]; then
        $SUDO wget $wget_opts https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb
    else
        $SUDO wget $wget_opts https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
    fi
    
    $SUDO dpkg -i /tmp/packages-microsoft-prod.deb
    $SUDO rm /tmp/packages-microsoft-prod.deb
    
    $SUDO apt-get update -qq
    $SUDO apt-get install -y -qq "dotnet-sdk-${DOTNET_VERSION}.0"
    
    if command -v dotnet &> /dev/null; then
        local installed_version
        installed_version=$(dotnet --version)
        log_success ".NET SDK $installed_version installed"
    else
        log_error ".NET SDK installation failed"
        exit 1
    fi
}

install_opencode() {
    log_info "Installing OpenCode AI Assistant..."
    
    if command -v opencode &> /dev/null; then
        local installed_version
        installed_version=$(opencode --version 2>/dev/null || echo "unknown")
        log_info "OpenCode already installed: $installed_version"
        return 0
    fi
    
    local temp_dir
    temp_dir=$(mktemp -d)
    
    log_info "Downloading OpenCode..."
    
    if command -v curl &> /dev/null; then
        log_warn "OpenCode requires manual installation from https://opencode.ai"
        log_info "To install manually:"
        log_info "  1. Visit https://opencode.ai"
        log_info "  2. Follow the installation instructions for your platform"
        log_info "  3. Or run: curl -fsSL https://get.opencode.ai | bash"
    else
        log_error "curl is required to install OpenCode"
    fi
    
    rm -rf "$temp_dir"
    
    if command -v opencode &> /dev/null; then
        log_success "OpenCode installed"
    else
        log_warn "OpenCode not found in PATH - manual installation may be required"
    fi
}

install_tldr() {
    log_info "Installing tldr (simplified man pages)..."
    
    if command -v tldr &> /dev/null; then
        log_success "tldr already installed"
        return 0
    fi
    
    if command -v npm &> /dev/null; then
        npm install -g tldr
    else
        $SUDO apt-get install -y -qq nodejs npm
        npm install -g tldr
    fi
    
    if command -v tldr &> /dev/null; then
        log_success "tldr installed"
    else
        log_warn "tldr installation failed"
    fi
}

configure_git() {
    if [[ "$SKIP_GIT" == "true" ]]; then
        log_info "Skipping git configuration"
        return 0
    fi
    
    log_info "Configuring git..."
    
    if [[ -z "$(git config --global user.name 2>/dev/null)" ]]; then
        echo "Enter your name for git commits:"
        read -r git_name
        git config --global user.name "$git_name"
    fi
    
    if [[ -z "$(git config --global user.email 2>/dev/null)" ]]; then
        echo "Enter your email for git commits:"
        read -r git_email
        git config --global user.email "$git_email"
    fi
    
    git config --global alias.st status
    git config --global alias.co checkout
    git config --global alias.br branch
    git config --global alias.ci commit
    git config --global alias.lg "log --oneline --graph --decorate"
    git config --global init.defaultBranch master
    git config --global pull.rebase false
    
    log_success "Git configured"
}

verify_installation() {
    log_info "Verifying installation..."
    
    local errors=0
    
    if command -v dotnet &> /dev/null; then
        log_success "dotnet: $(dotnet --version)"
    else
        log_error "dotnet: NOT FOUND"
        ((errors++))
    fi
    
    if command -v git &> /dev/null; then
        log_success "git: $(git --version)"
    else
        log_error "git: NOT FOUND"
        ((errors++))
    fi
    
    if command -v opencode &> /dev/null; then
        log_success "opencode: $(opencode --version)"
    else
        log_warn "opencode: NOT FOUND (optional)"
    fi
    
    if [[ $errors -gt 0 ]]; then
        log_error "$errors critical tool(s) not found"
        return 1
    fi
    
    log_success "All required tools installed"
    return 0
}

usage() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --dry-run       Show what would be installed without installing"
    echo "  --skip-git      Skip git configuration"
    echo "  --dotnet-ver N  Set .NET version (default: 9)"
    echo "  -h, --help      Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                    # Interactive setup"
    echo "  $0 --dry-run          # Preview installation"
    echo "  $0 --dotnet-ver 10    # Install .NET 10 (when available)"
}

main() {
    DRY_RUN=false
    SKIP_GIT=false
    
    while [[ $# -gt 0 ]]; do
        case $1 in
            --dry-run)
                DRY_RUN=true
                shift
                ;;
            --skip-git)
                SKIP_GIT=true
                shift
                ;;
            --dotnet-ver)
                DOTNET_VERSION="$2"
                shift 2
                ;;
            -h|--help)
                usage
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                usage
                exit 1
                ;;
        esac
    done
    
    echo "=============================================="
    echo "SisoDis.NET Developer Environment Setup"
    echo "=============================================="
    echo ""
    echo "Configuration:"
    echo "  .NET SDK Version: $DOTNET_VERSION"
    echo "  Skip Git Config:  $SKIP_GIT"
    echo ""
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN MODE - No changes will be made"
        log_info "Would install:"
        log_info "  - Build essentials (build-essential, curl, wget, git, etc.)"
        log_info "  - .NET SDK $DOTNET_VERSION"
        log_info "  - OpenCode AI Assistant"
        [[ "$SKIP_GIT" == false ]] && log_info "  - Git configuration"
        exit 0
    fi
    
    check_privileges
    detect_os
    update_packages
    install_build_essentials
    install_dotnet
    install_opencode
    install_tldr
    configure_git
    verify_installation
    
    echo ""
    echo "=============================================="
    echo "Setup Complete!"
    echo "=============================================="
    echo ""
    echo "Next steps:"
    echo "  1. Clone the repository:"
    echo "     git clone <repo-url>"
    echo ""
    echo "  2. Restore dependencies:"
    echo "     dotnet restore"
    echo ""
    echo "  3. Build the solution:"
    echo "     dotnet build"
    echo ""
    echo "  4. Run tests:"
    echo "     dotnet test"
    echo ""
    echo "For more information, see: dev-env/README.md"
}

main "$@"
