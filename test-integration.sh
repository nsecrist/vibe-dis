#!/bin/bash
# Integration test: runs Producer and Receiver together to verify DIS PDU streaming

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "=============================================="
echo "SisoDis.NET Integration Test"
echo "=============================================="
echo ""

# Check if solution builds
echo "[1/4] Building solution..."
dotnet build --verbosity quiet
echo "      Build OK"
echo ""

# Kill any existing processes on our ports
echo "[2/4] Cleaning up stale processes..."
pkill -f "SisoDis.Producer" 2>/dev/null || true
pkill -f "SisoDis.Receiver" 2>/dev/null || true
sleep 1
echo "      Done"
echo ""

# Start Producer in background
echo "[3/4] Starting Producer..."
dotnet run --project SisoDis.Producer -- \
    --rate 2 \
    --entities 3 \
    --pattern Linear \
    --duration 60 \
    > /tmp/sisodis-producer.log 2>&1 &
PRODUCER_PID=$!
echo "      Producer started (PID: $PRODUCER_PID)"
echo ""

# Give producer time to start
sleep 2

# Start Receiver (this will run in foreground)
echo "[4/4] Starting Receiver..."
echo "      Press Ctrl+Q to quit"
echo ""
echo "=============================================="
echo ""

dotnet run --project SisoDis.Receiver

# Cleanup
echo ""
echo "[Cleanup] Stopping Producer..."
kill $PRODUCER_PID 2>/dev/null || true
wait $PRODUCER_PID 2>/dev/null || true

echo "Done."
