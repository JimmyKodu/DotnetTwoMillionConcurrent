#!/bin/bash

# System tuning script for 2M concurrent connections
# Run with sudo: sudo ./tune-system.sh

echo "=== System Tuning for 2 Million Concurrent MQTT Connections ==="
echo ""

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "Please run as root (use sudo)"
    exit 1
fi

echo "Applying system tuning parameters..."

# 1. Increase file descriptor limits
echo "1. Setting file descriptor limits..."
ulimit -n 2100000

cat >> /etc/security/limits.conf << EOF
# Added for MQTT server
* soft nofile 2100000
* hard nofile 2100000
EOF

# 2. Tune TCP/IP stack for high connection count
echo "2. Tuning TCP/IP stack..."

# Expand ephemeral port range to maximum
sysctl -w net.ipv4.ip_local_port_range="1024 65535"

# Enable TIME_WAIT socket reuse (critical for client connections)
sysctl -w net.ipv4.tcp_tw_reuse=1

# Reduce TIME_WAIT timeout from default 60s to 15s
sysctl -w net.ipv4.tcp_fin_timeout=15

# Increase max number of sockets in TIME_WAIT
sysctl -w net.ipv4.tcp_max_tw_buckets=2000000

# Increase socket listen backlog
sysctl -w net.core.somaxconn=65535
sysctl -w net.ipv4.tcp_max_syn_backlog=65535

# Increase network buffer sizes
sysctl -w net.core.rmem_max=16777216
sysctl -w net.core.wmem_max=16777216
sysctl -w net.ipv4.tcp_rmem="4096 87380 16777216"
sysctl -w net.ipv4.tcp_wmem="4096 65536 16777216"

# Increase connection tracking table size
sysctl -w net.netfilter.nf_conntrack_max=2000000 2>/dev/null || echo "Note: nf_conntrack not available"

# Disable connection tracking if possible (optional, improves performance)
# modprobe -r nf_conntrack 2>/dev/null || echo "Note: Cannot disable nf_conntrack"

# 3. Make changes persistent across reboots
echo "3. Making changes persistent..."
cat >> /etc/sysctl.conf << EOF

# Added for MQTT 2M connections test
net.ipv4.ip_local_port_range = 1024 65535
net.ipv4.tcp_tw_reuse = 1
net.ipv4.tcp_fin_timeout = 15
net.ipv4.tcp_max_tw_buckets = 2000000
net.core.somaxconn = 65535
net.ipv4.tcp_max_syn_backlog = 65535
net.core.rmem_max = 16777216
net.core.wmem_max = 16777216
net.ipv4.tcp_rmem = 4096 87380 16777216
net.ipv4.tcp_wmem = 4096 65536 16777216
EOF

echo ""
echo "=== System Tuning Complete ==="
echo ""
echo "Current settings:"
echo "  File descriptors (ulimit -n): $(ulimit -n)"
echo "  Ephemeral port range: $(cat /proc/sys/net/ipv4/ip_local_port_range)"
echo "  TCP TIME_WAIT reuse: $(cat /proc/sys/net/ipv4/tcp_tw_reuse)"
echo "  TCP FIN timeout: $(cat /proc/sys/net/ipv4/tcp_fin_timeout)"
echo ""
echo "IMPORTANT: Logout and login again for file descriptor limits to take effect"
echo "Or run: sudo -u <username> -i bash"
echo ""
