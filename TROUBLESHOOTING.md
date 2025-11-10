# 15,568连接限制问题排查和解决

## 问题描述

在测试设备模拟器时，发现连接数达到约15,568后不再增长，无法达到预期的200万并发连接。

## 问题分析

### 1. 症状观察

- 连接数增长到约15,568后停止
- 设备模拟器继续尝试连接但失败
- 错误日志可能显示连接超时或拒绝

### 2. 根本原因

**临时端口（Ephemeral Ports）耗尽**

在Linux系统中：
- 默认临时端口范围: `32768 - 60999`（可通过 `cat /proc/sys/net/ipv4/ip_local_port_range` 查看）
- 理论可用端口数: `60999 - 32768 + 1 = 28,232`
- 实际可用端口数: 约15,000-16,000（由于TIME_WAIT状态占用）

每个从客户端发起的出站TCP连接都需要：
```
源IP + 源端口 + 目标IP + 目标端口 = 唯一的连接
```

当所有设备从同一台机器模拟时，它们共享：
- 同一个源IP（客户端IP）
- 同一个目标IP（服务器IP）
- 同一个目标端口（5000）

因此只有**源端口**可以变化来区分连接，可用端口耗尽后无法建立新连接。

### 3. TIME_WAIT状态的影响

TCP连接关闭后会进入TIME_WAIT状态（默认60秒），在此期间：
- 端口被占用，不能被新连接使用
- 这是TCP协议的安全特性，防止旧连接的数据包影响新连接

在高并发场景下，大量连接同时建立和关闭，导致大量端口处于TIME_WAIT状态。

## 解决方案

### 方案1: 扩展临时端口范围（推荐）

```bash
# 扩展到最大范围 1024-65535（保留1-1023给系统服务）
sudo sysctl -w net.ipv4.ip_local_port_range="1024 65535"
```

**效果**: 可用端口数从28k增加到64k

### 方案2: 启用TIME_WAIT重用（关键！）

```bash
# 允许TIME_WAIT状态的套接字被新连接重用
sudo sysctl -w net.ipv4.tcp_tw_reuse=1
```

**效果**: 显著减少TIME_WAIT状态占用的端口数

### 方案3: 减少TIME_WAIT超时时间

```bash
# 将TIME_WAIT超时从默认60秒减少到15秒
sudo sysctl -w net.ipv4.tcp_fin_timeout=15
```

**效果**: 端口可以更快被释放和重用

### 方案4: 增加TIME_WAIT桶数量

```bash
# 增加系统可以维护的TIME_WAIT连接数
sudo sysctl -w net.ipv4.tcp_max_tw_buckets=2000000
```

**效果**: 允许更多连接同时处于TIME_WAIT状态

### 综合解决方案：使用tune-system.sh脚本

我们提供了自动化脚本 `tune-system.sh`，它会：

1. 设置文件描述符限制
2. 扩展临时端口范围
3. 启用TIME_WAIT重用
4. 优化TCP参数
5. 使更改在重启后持久化

**使用方法**:
```bash
sudo ./tune-system.sh
```

## 验证修复

### 1. 检查端口范围

```bash
cat /proc/sys/net/ipv4/ip_local_port_range
# 应该显示: 1024 65535
```

### 2. 检查TIME_WAIT重用

```bash
cat /proc/sys/net/ipv4/tcp_tw_reuse
# 应该显示: 1
```

### 3. 监控TIME_WAIT连接数

```bash
# 实时监控TIME_WAIT状态的连接数
watch -n 1 "netstat -an | grep TIME_WAIT | wc -l"
```

### 4. 监控可用端口

```bash
# 查看当前使用的端口数
ss -tan | grep ESTAB | wc -l
```

## 服务器端优化

除了客户端调优，我们也优化了服务器配置：

### 1. Kestrel配置

```csharp
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxConcurrentConnections = null; // 无限制
    serverOptions.Limits.MaxConcurrentUpgradedConnections = null; // 无限制
    serverOptions.ListenAnyIP(5000); // 绑定到5000端口
});
```

### 2. 移除HTTPS重定向

```csharp
// 注释掉HTTPS重定向，减少连接开销
// app.UseHttpsRedirection();
```

## 性能测试结果

应用修复后的预期结果：

| 测试规模 | 连接时间 | 内存使用 | 备注 |
|---------|---------|---------|------|
| 1,000 | <5秒 | <100MB | 基本测试 |
| 10,000 | ~20秒 | ~500MB | 小规模 |
| 100,000 | ~3分钟 | ~3GB | 中等规模 |
| 1,000,000 | ~30分钟 | ~10GB | 大规模 |
| 2,000,000 | ~40-60分钟 | ~16GB | 完整规模 |

## 其他注意事项

### 1. 文件描述符限制

确保文件描述符限制足够：
```bash
ulimit -n 2100000
```

### 2. 内存限制

确保系统有足够内存，建议至少16GB

### 3. CPU资源

建议至少8核CPU以处理并发连接

### 4. 网络带宽

确保网络带宽足够，建议至少1Gbps

## 参考资料

- [Linux TCP/IP调优](https://www.kernel.org/doc/Documentation/networking/ip-sysctl.txt)
- [TIME_WAIT状态说明](https://vincent.bernat.ch/en/blog/2014-tcp-time-wait-state-linux)
- [高并发系统调优](https://www.kernel.org/doc/html/latest/admin-guide/sysctl/net.html)

## 常见问题

### Q: 为什么不能直接禁用TIME_WAIT？

A: TIME_WAIT是TCP协议的重要特性，用于：
- 确保最后的ACK能够到达对端
- 防止旧连接的延迟数据包影响新连接
- 直接禁用会导致网络不稳定

### Q: tcp_tw_reuse安全吗？

A: 是的，它只会在以下条件都满足时重用TIME_WAIT套接字：
- 新连接是出站连接（客户端主动连接）
- 时间戳选项启用
- 新连接的时间戳大于记录的时间戳

### Q: 如何在不同机器上分布设备模拟？

A: 最佳方案是在多台机器上运行设备模拟器：
```bash
# 机器1: 模拟前100万设备
TOTAL_DEVICES=1000000 dotnet run

# 机器2: 模拟后100万设备  
TOTAL_DEVICES=1000000 dotnet run
```

这样每台机器只需要建立100万连接，完全避免端口耗尽问题。
