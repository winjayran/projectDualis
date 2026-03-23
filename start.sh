#!/bin/bash
# Project Dualis - Startup Script
# Works with or without Docker

set -e

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$PROJECT_DIR"

# Create logs directory
mkdir -p logs

echo "==================================="
echo "Project Dualis - Launcher"
echo "==================================="
echo ""

# Check if Docker is available
HAS_DOCKER=0
if command -v docker &> /dev/null; then
    HAS_DOCKER=1
    # Test if Docker can actually pull images
    if docker info > /dev/null 2>&1; then
        echo "✓ Docker is available"
    else
        echo "⚠ Docker daemon not responding"
        HAS_DOCKER=0
    fi
fi

# Check if Windows Docker is accessible (WSL)
if [ $HAS_DOCKER -eq 0 ] && command -v docker.exe &> /dev/null; then
    HAS_DOCKER=2
    echo "✓ Windows Docker detected"
fi

# Check if Qdrant is running
check_qdrant() {
    if [ $HAS_DOCKER -eq 1 ]; then
        docker ps --filter "name=dualis-qdrant" --format '{{.Names}}' 2>/dev/null | grep -q "dualis-qdrant"
    elif [ $HAS_DOCKER -eq 2 ]; then
        docker.exe ps --filter "name=dualis-qdrant" --format '{{.Names}}' 2>/dev/null | grep -q "dualis-qdrant"
    else
        return 1
    fi
}

# Check if backend is running
check_backend() {
    pgrep -f "backend/main.py" > /dev/null
}

# Start Qdrant
start_qdrant() {
    if check_qdrant; then
        echo "✓ Qdrant is already running"
        return 0
    fi

    if [ $HAS_DOCKER -eq 0 ]; then
        echo "⚠ Docker not available. Skipping Qdrant (memory features disabled)."
        echo "  Chat will work normally, but memory features require Qdrant."
        return 0
    fi

    echo "Starting Qdrant..."

    # Try to pull the image first
    if [ $HAS_DOCKER -eq 1 ]; then
        if ! docker pull qdrant/qdrant:latest > /dev/null 2>&1; then
            echo "⚠ Could not pull Qdrant image (network issue)"
            echo "  Proceeding without Qdrant. Chat will still work!"
            return 0
        fi
    else
        if ! docker.exe pull qdrant/qdrant:latest > /dev/null 2>&1; then
            echo "⚠ Could not pull Qdrant image (network issue)"
            echo "  Proceeding without Qdrant. Chat will still work!"
            return 0
        fi
    fi

    # Run Qdrant container
    if [ $HAS_DOCKER -eq 1 ]; then
        docker run -d \
            --name dualis-qdrant \
            --restart unless-stopped \
            -p 6333:6333 \
            -p 6334:6334 \
            -v "$PROJECT_DIR/qdrant_storage:/qdrant/storage" \
            qdrant/qdrant > /dev/null 2>&1
    else
        docker.exe run -d \
            --name dualis-qdrant \
            --restart unless-stopped \
            -p 6333:6333 \
            -p 6334:6334 \
            -v "$(wslpath -w "$PROJECT_DIR/qdrant_storage"):/qdrant/storage" \
            qdrant/qdrant > /dev/null 2>&1
    fi

    sleep 3

    if check_qdrant; then
        echo "✓ Qdrant started successfully"
    else
        echo "⚠ Qdrant failed to start (continuing without it)"
    fi
}

# Start Backend
start_backend() {
    if check_backend; then
        echo "✓ Backend is already running"
        return 0
    fi

    if [ ! -f "env.sh" ]; then
        echo "✗ env.sh not found. Please create it from env_example.sh"
        return 1
    fi

    echo "Starting Backend Server..."
    source env.sh

    # Kill any existing backend processes
    pkill -f "backend/main.py" 2>/dev/null || true

    # Start in background with logging
    PYTHONPATH=./backend nohup python backend/main.py > logs/backend.log 2>&1 &
    BACKEND_PID=$!
    echo $BACKEND_PID > logs/backend.pid

    sleep 3

    if check_backend; then
        echo "✓ Backend started successfully (PID: $BACKEND_PID)"
        echo "  Logs: logs/backend.log"
        echo "  API: http://localhost:8000/docs"
    else
        echo "✗ Failed to start Backend. Check logs/backend.log"
        echo ""
        tail -20 logs/backend.log
        return 1
    fi
}

# Stop all services
stop_all() {
    echo "Stopping all services..."

    # Stop backend
    if check_backend; then
        pkill -f "backend/main.py" || true
        if [ -f "logs/backend.pid" ]; then
            kill $(cat logs/backend.pid) 2>/dev/null || true
            rm logs/backend.pid
        fi
        echo "✓ Backend stopped"
    fi

    # Stop Qdrant
    if check_qdrant; then
        if [ $HAS_DOCKER -eq 1 ]; then
            docker stop dualis-qdrant > /dev/null 2>&1
            docker rm dualis-qdrant > /dev/null 2>&1
        elif [ $HAS_DOCKER -eq 2 ]; then
            docker.exe stop dualis-qdrant > /dev/null 2>&1
            docker.exe rm dualis-qdrant > /dev/null 2>&1
        fi
        echo "✓ Qdrant stopped"
    fi
}

# Show status
show_status() {
    echo "=== System Status ==="

    if [ $HAS_DOCKER -eq 0 ]; then
        echo "⚠ Docker: Not available (memory features disabled)"
    elif check_qdrant; then
        echo "✓ Qdrant: Running"
    else
        echo "✗ Qdrant: Not running"
    fi

    if check_backend; then
        echo "✓ Backend: Running (PID: $(cat logs/backend.pid 2>/dev/null || echo "unknown"))"
        if [ -f "logs/backend.log" ]; then
            echo "  Recent log:"
            tail -3 logs/backend.log 2>/dev/null | sed 's/^/    /'
        fi
    else
        echo "✗ Backend: Not running"
    fi

    # Test API
    if curl -s http://localhost:8000/health > /dev/null 2>&1; then
        echo "✓ Backend API: Responding at http://localhost:8000"
        echo ""
        curl -s http://localhost:8000/health 2>/dev/null | python3 -m json.tool 2>/dev/null | sed 's/^/  /'
    else
        echo "✗ Backend API: Not responding"
    fi
}

# View logs
view_logs() {
    if [ -f "logs/backend.log" ]; then
        echo "=== Backend Logs (live - Ctrl+C to exit) ==="
        tail -f logs/backend.log
    else
        echo "No backend logs found. Start the backend first."
    fi
}

# Open Unity
open_unity() {
    if grep -qi microsoft /proc/version 2>/dev/null; then
        WIN_PATH=$(wslpath -w "$PROJECT_DIR/frontend")
        echo "Opening Unity project path in Windows Explorer..."
        echo "Path: $WIN_PATH"
        explorer.exe "$WIN_PATH"
        echo ""
        echo "To open in Unity:"
        echo "1. Open Unity Hub on Windows"
        echo "2. Click 'Add project' and paste this path:"
        echo "   $WIN_PATH"
    else
        echo "Unity not found on this system."
        echo "Please open Unity Hub and add: $PROJECT_DIR/frontend"
    fi
}

# Quick start - backend only (no Docker needed)
quick_start() {
    echo "Quick Start (Backend only)..."
    echo ""

    if ! check_backend; then
        start_backend
    fi

    echo ""
    echo "✓ Backend is running!"
    echo "  API: http://localhost:8000/docs"
    echo ""
    echo "Now:"
    echo "1. Open Unity"
    echo "2. Press Play"
    echo "3. Click 'Send Test Hello' button"
}

# Menu
echo "Select an option:"
echo "1) Quick Start (Backend only - No Docker needed)"
echo "2) Start Backend Only"
echo "3) Start Qdrant (Requires Docker)"
echo "4) Check Status"
echo "5) View Backend Logs (Live)"
echo "6) Stop All Services"
echo "7) Open Unity Project"
echo "q) Quit"
echo ""
read -p "> " choice

case $choice in
    1)
        quick_start
        ;;
    2)
        start_backend
        ;;
    3)
        start_qdrant
        ;;
    4)
        show_status
        ;;
    5)
        view_logs
        ;;
    6)
        stop_all
        ;;
    7)
        open_unity
        ;;
    q)
        echo "Goodbye!"
        exit 0
        ;;
    *)
        echo "Invalid option"
        ;;
esac

echo ""
echo "Press Enter to continue..."
read
