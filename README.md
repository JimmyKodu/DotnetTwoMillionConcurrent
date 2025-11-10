# DotnetTwoMillionConcurrent

.NET 9 ASP.NET Core MVC + MQTT处理二百万设备每三分钟上报一次数据，附带控制台程序模拟二百万台设备

## 项目概述

这是一个高性能的MQTT服务器和设备模拟器项目，用于处理200万台设备每3分钟上报一次数据的场景。

### 特性

- ✅ .NET 9 ASP.NET Core MVC Web应用
- ✅ MQTT服务器支持WebSocket连接
- ✅ 支持200万并发设备连接
- ✅ 高性能数据处理管道（使用Channel进行异步处理）
- ✅ 实时统计仪表盘
- ✅ 设备模拟器控制台应用
- ✅ 可配置的连接速率和报告间隔

## 项目结构

```
DotnetTwoMillionConcurrent/
├── MqttServer/              # MQTT服务器（ASP.NET Core MVC）
│   ├── Controllers/         # MVC控制器
│   ├── Models/             # 数据模型
│   ├── Services/           # 后台服务
│   └── Views/              # 视图
└── DeviceSimulator/         # 设备模拟器（控制台应用）
```

## 快速开始

### 前置要求

- .NET 9 SDK
- 至少 8GB RAM（用于运行200万设备模拟）
- Linux/Windows/macOS

### 1. 启动MQTT服务器

```bash
cd MqttServer
dotnet run
```

服务器将在 `http://localhost:5000` 启动，MQTT端点为 `ws://localhost:5000/mqtt`

### 2. 运行设备模拟器

```bash
cd DeviceSimulator
dotnet run
```

### 配置选项

设备模拟器支持通过环境变量配置：

```bash
# 配置MQTT服务器地址
export MQTT_SERVER=localhost
export MQTT_PORT=5000

# 配置设备数量（默认: 2,000,000）
export TOTAL_DEVICES=2000000

# 配置批量大小（默认: 10,000）
export BATCH_SIZE=10000

# 配置报告间隔（秒，默认: 180 = 3分钟）
export REPORT_INTERVAL=180

# 配置每秒连接数（默认: 1,000）
export CONNECTIONS_PER_SECOND=1000

# 运行模拟器
dotnet run
```

### 小规模测试

对于测试目的，可以使用较小的设备数量：

```bash
# 测试 1000 台设备
TOTAL_DEVICES=1000 REPORT_INTERVAL=10 dotnet run
```

## 架构说明

### MQTT服务器

- **端点**: WebSocket (`ws://localhost:5000/mqtt`)
- **连接限制**: 配置为支持2,100,000并发连接
- **数据处理**: 使用 `Channel<DeviceData>` 进行高性能异步处理
- **统计**: 实时显示连接设备数、消息接收数和处理速度

### 设备模拟器

- 每个设备每3分钟发送一次遥测数据
- 数据包含: 设备ID、时间戳、温度、湿度和状态
- 使用随机延迟避免所有设备同时发送数据
- 支持批量连接以提高性能

### 数据模型

```json
{
  "deviceId": "Device_0000001",
  "timestamp": "2024-01-01T00:00:00Z",
  "temperature": 25.5,
  "humidity": 65.0,
  "status": "online"
}
```

## 性能优化

1. **Kestrel配置**: 提高最大并发连接数
2. **异步处理**: 使用Channel进行高性能数据流处理
3. **内存优化**: 避免阻塞操作和大对象分配
4. **连接池**: 复用MQTT客户端连接
5. **批量处理**: 设备按批次连接

## 监控和统计

访问 `http://localhost:5000` 查看实时统计仪表盘：

- 当前连接设备数
- 接收的总消息数
- 处理的总消息数
- 每秒消息处理速率
- 服务器运行时间

仪表盘每5秒自动刷新。

## 系统要求

### 运行200万设备模拟

- CPU: 8核或更多
- RAM: 16GB+
- 网络: 高带宽连接
- OS: Linux推荐（更好的网络栈性能）

### 系统调优（Linux）- **必须执行！**

⚠️ **重要**: 不执行系统调优会导致连接数停在约15,568个（临时端口耗尽）

**快速调优（推荐）:**
```bash
# 使用提供的脚本一键调优
sudo ./tune-system.sh
```

**手动调优:**
```bash
# 增加文件描述符限制
ulimit -n 2100000

# 扩展临时端口范围（解决15k连接限制）
sudo sysctl -w net.ipv4.ip_local_port_range="1024 65535"

# 启用TIME_WAIT重用（关键！）
sudo sysctl -w net.ipv4.tcp_tw_reuse=1

# 减少TIME_WAIT超时
sudo sysctl -w net.ipv4.tcp_fin_timeout=15

# 增加TIME_WAIT桶数
sudo sysctl -w net.ipv4.tcp_max_tw_buckets=2000000
```

**常见问题**: 
- **连接数停在15,568左右？** → 系统临时端口耗尽，执行上述调优
- 详见 [QUICKSTART.md](QUICKSTART.md) 的故障排查部分

## 开发

### 构建项目

```bash
dotnet build
```

### 运行测试

```bash
dotnet test
```

## 许可证

MIT License

## 贡献

欢迎提交问题和拉取请求。