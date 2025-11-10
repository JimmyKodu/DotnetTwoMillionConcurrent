# 快速测试指南

## 小规模测试（推荐先运行）

在尝试200万设备之前，建议先进行小规模测试以验证系统：

### 1. 启动服务器

打开第一个终端：

```bash
cd MqttServer
dotnet run
```

等待看到:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### 2. 测试 100 个设备

打开第二个终端：

```bash
cd DeviceSimulator

# 配置100个设备，每10秒报告一次
TOTAL_DEVICES=100 REPORT_INTERVAL=10 CONNECTIONS_PER_SECOND=50 dotnet run
```

### 3. 查看统计

浏览器打开: http://localhost:5000

你应该能看到:
- Connected Devices: ~100
- Messages Received: 持续增长
- Messages Processed: 持续增长
- Messages/Second: 取决于报告间隔

## 中等规模测试

### 测试 10,000 个设备

```bash
cd DeviceSimulator
TOTAL_DEVICES=10000 REPORT_INTERVAL=30 CONNECTIONS_PER_SECOND=500 dotnet run
```

### 测试 100,000 个设备

```bash
cd DeviceSimulator
TOTAL_DEVICES=100000 REPORT_INTERVAL=60 CONNECTIONS_PER_SECOND=1000 dotnet run
```

## 完整规模测试（2,000,000 设备）

**注意**: 需要充足的系统资源！

### 系统准备

```bash
# Linux 系统调优
ulimit -n 2100000
sudo sysctl -w net.ipv4.ip_local_port_range="1024 65535"
sudo sysctl -w net.ipv4.tcp_tw_reuse=1
sudo sysctl -w net.core.somaxconn=65535
```

### 运行测试

```bash
cd DeviceSimulator

# 使用默认配置（2百万设备，3分钟间隔）
dotnet run

# 或自定义配置
TOTAL_DEVICES=2000000 \
REPORT_INTERVAL=180 \
CONNECTIONS_PER_SECOND=1000 \
dotnet run
```

## 性能指标参考

### 100 设备
- 连接时间: < 5秒
- 内存使用: < 100MB
- CPU使用: < 5%

### 10,000 设备
- 连接时间: ~20秒
- 内存使用: ~500MB
- CPU使用: < 20%

### 100,000 设备
- 连接时间: ~2-3分钟
- 内存使用: ~2-3GB
- CPU使用: < 50%

### 2,000,000 设备
- 连接时间: ~30-40分钟
- 内存使用: ~8-16GB
- CPU使用: < 80%

## 故障排查

### 连接失败

如果看到大量连接错误：
1. 降低 CONNECTIONS_PER_SECOND
2. 检查防火墙设置
3. 确认系统资源充足

### 内存不足

如果遇到内存问题：
1. 减少 TOTAL_DEVICES
2. 增加系统内存
3. 调整 GC 设置

### 性能下降

如果性能不如预期：
1. 确保使用 Release 配置构建
2. 检查 CPU 和网络利用率
3. 考虑分布式部署

## 监控建议

### 服务器端
- 使用 `top` 或 `htop` 监控资源使用
- 检查日志输出查看处理统计

### 客户端
- 观察终端输出的统计信息
- 每10秒会显示连接数和消息发送数

## 停止测试

两个程序都可以用 `Ctrl+C` 优雅地停止。
