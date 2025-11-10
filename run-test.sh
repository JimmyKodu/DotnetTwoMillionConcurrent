#!/bin/bash

# Simple test script to run a small-scale test
# Usage: ./run-test.sh [number_of_devices]

DEVICES=${1:-100}
INTERVAL=${2:-10}

echo "Starting MQTT Server and Device Simulator Test"
echo "Devices: $DEVICES"
echo "Report Interval: $INTERVAL seconds"
echo ""

# Check if server is already running
if curl -s http://localhost:5000 > /dev/null 2>&1; then
    echo "Server is already running at http://localhost:5000"
else
    echo "Starting MQTT Server..."
    cd MqttServer
    dotnet run &
    SERVER_PID=$!
    echo "Server PID: $SERVER_PID"
    cd ..
    
    # Wait for server to start
    echo "Waiting for server to start..."
    for i in {1..30}; do
        if curl -s http://localhost:5000 > /dev/null 2>&1; then
            echo "Server is ready!"
            break
        fi
        sleep 1
    done
fi

echo ""
echo "Starting Device Simulator with $DEVICES devices..."
cd DeviceSimulator
TOTAL_DEVICES=$DEVICES REPORT_INTERVAL=$INTERVAL CONNECTIONS_PER_SECOND=50 dotnet run

# Cleanup
if [ ! -z "$SERVER_PID" ]; then
    echo ""
    echo "Stopping server..."
    kill $SERVER_PID
fi
