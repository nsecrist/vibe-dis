#!/bin/bash
# Integration test: runs Producer and Receiver in tmux for live viewing

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "=============================================="
echo "SisoDis.NET Integration Test (tmux)"
echo "=============================================="
echo ""

# Install tmux if not present
if ! command -v tmux &> /dev/null; then
    echo "[Installing tmux...]"
    if command -v apt-get &> /dev/null; then
        sudo apt-get update -qq && sudo apt-get install -y -qq tmux
    elif command -v brew &> /dev/null; then
        brew install tmux
    elif command -v yum &> /dev/null; then
        sudo yum install -y tmux
    elif command -v dnf &> /dev/null; then
        sudo dnf install -y tmux
    else
        echo "ERROR: Cannot install tmux - please install it manually"
        exit 1
    fi
    echo "      tmux installed"
fi
echo ""

# Check if solution builds
echo "[1/3] Building solution..."
dotnet build --verbosity quiet
echo "      Build OK"
echo ""

# Kill any existing processes
echo "[2/3] Cleaning up stale processes..."
pkill -f "SisoDis.Producer" 2>/dev/null || true
pkill -f "SisoDis.Receiver" 2>/dev/null || true
tmux kill-session -t siso 2>/dev/null || true
sleep 1
echo "      Done"
echo ""

# Create new tmux session
echo "Starting tmux session..."
tmux new-session -d -s siso

# Wait for tmux to be ready
sleep 0.5

# Start Producer in first pane
tmux send-keys -t siso.0 "cd $SCRIPT_DIR && dotnet run --project SisoDis.Producer" C-m

# Split horizontally and start Receiver in second pane
tmux split-window -h -t siso.0
sleep 0.3
tmux send-keys -t siso.1 "cd $SCRIPT_DIR && dotnet run --project SisoDis.Receiver" C-m

# Select first pane (Producer)
tmux select-pane -t siso.0

echo ""
echo "Controls:"
echo "  Ctrl+B then 1 : Switch to Producer pane"
echo "  Ctrl+B then 2 : Switch to Receiver pane"
echo "  Ctrl+B then d : Detach (leave running)"
echo "  tmux kill-session -t siso : Stop all"
echo ""

# Attach to the session
tmux attach-session -t siso

# When user detaches, cleanup
echo ""
echo "[Cleanup] Stopping tmux session..."
tmux kill-session -t siso 2>/dev/null || true

echo "Done."
