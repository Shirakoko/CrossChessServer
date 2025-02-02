# 井字棋小游戏联网模块服务端
## 使用说明
### 运行控制台应用

1. **开发环境（Visual Studio）中运行**：
   - 打开项目后，点击工具栏中的绿色箭头（启动按钮）或按 `F5` 键运行程序。
   - 程序将以调试模式启动，控制台窗口会显示 `Hello, World!`，并等待用户输入命令。
2. **生产环境（可执行文件）运行**：
   - 在项目生成目录（如 `bin\Release\netx.x`）中找到生成的 `exe` 文件。
   - 双击 `exe` 文件运行程序，控制台窗口会显示 `Hello, World!`，并等待用户输入命令。

### 命令行输入操作

程序启动后，可以通过命令行输入以下命令来操作服务器：

1. **启动服务器**：
   - 输入命令：`Start [--ip <IP地址>] [--port <端口号>] [--maxNum <最大客户端数量>]`
   - 示例：
     - `Start`：使用默认IP地址（`192.168.1.5`）、默认端口号（`8080`）和默认最大客户端数量（`10`）启动服务器。
     - `Start --ip 127.0.0.1 --port 8888 --maxNum 5`：使用IP地址 `127.0.0.1`、端口号 `8888` 和最大客户端数量 `5` 启动服务器。
   - 成功启动后，控制台会显示：`服务器开启成功，IP地址: <IP地址>，端口：<端口号>`。
2. **关闭服务器**：
   - 输入命令：`Quit`
   - 成功关闭后，控制台会显示：`服务器关闭`。
3. **其他输入**：
   - 如果输入的命令无效，程序会忽略并继续等待下一个有效命令

## 协议处理

### 协议类型

| 消息ID                 | 数值 | 方向            | 关键行为                                               |
| :--------------------- | :--- | :-------------- | :----------------------------------------------------- |
| **EnterHall**          | 1    | 客户端 → 服务端 | 携带用户名初始化连接，触发服务端分配clientID           |
| **QuitHall**           | 2    | 客户端 → 服务端 | 清除用户在大厅的在线状态                               |
| **SendBattleRequest**  | 3    | 客户端 ↔ 服务端 | 请求方发送目标clientID，服务端转发请求并标记目标为繁忙 |
| **ReplyBattleRequest** | 4    | 客户端 → 服务端 | 包含接受/拒绝标志，触发战局创建或状态重置              |
| **EnterRound**         | 5    | 服务端 → 客户端 | 携带先手标识(isPrevPlayer)和战局索引(onlineRoundIndex) |
| **RoundInfo**          | 6    | 客户端 → 服务端 | 持久化保存到`RoundManager`管理的txt文件                |
| **RequestRoundList**   | 7    | 客户端 → 服务端 | 触发服务端从文件加载历史数据                           |
| **ProvideRoundList**   | 8    | 服务端 → 客户端 | 携带所有历史战局的Round对象数组                        |
| **AllowEnterHall**     | 11   | 服务端 → 客户端 | 返回分配的clientID（数值型标识）                       |
| **HallClients**        | 12   | 服务端 → 客户端 | 包含大厅用户字典（clientID-用户名映射）                |
| **RequestHallClients** | 13   | 客户端 → 服务端 | 手动请求刷新大厅列表                                   |
| **MoveInfo**           | 18   | 客户端 ↔ 服务端 | 包含棋子位置(pos)和战局索引(onlineRoundIndex)          |
| **ClientQuit**         | 99   | 客户端 → 服务端 | 显式断开连接指令                                       |
| **HeartMessage**       | 100  | 客户端 → 服务端 | 维持TCP连接活性                                        |
| **RoundState**         | 9    | -               | // TODO                                                |

### 服务端协议处理

#### 核心消息处理

1. **战局管理**
   - **RoundInfo (6)**
     → 解析战局数据 → 调用`RoundManager.SaveRoundInfo()`持久化到txt文件
   - **RequestRoundList (7)**
     → 调用`RoundManager.GetRoundList()`读取文件 → 发送`ProvideRoundList`消息给客户端
2. **大厅管理**
   - **EnterHall (1)**
     → 记录用户名 → 分配clientID → 发送`AllowEnterHall` → 加入大厅字典
   - **QuitHall (2)**
     → 从大厅字典移除 → 触发全厅广播更新
   - **RequestHallClients (13)**
     → 立即发送当前大厅用户字典给客户端
3. **对战流程**
   - **SendBattleRequest (3)**
     → 验证目标状态 → 转发请求 → 标记目标为`繁忙`
   - **ReplyBattleRequest (4)**
     - 拒绝：重置双方为`空闲`
     - 接受：
       → 创建`OnlineRoundState`对象 → 分配全局`onlineRoundIndex`
       → 标记双方为`繁忙` → 发送`EnterRound`（先手标记不同） → 创建战局
   - **MoveInfo (18)**
     → 校验对战合法性 → 更新棋盘状态/战局信息 → 转发给对手客户端
4. **连接管理**
   - **ClientQuit (99)**
     → 清除数据（大厅&战局） → 关闭Socket
   - **HeartMessage (100)**
     → 更新`lastHeartbeatTime`时间戳

#### 技术细节

- **状态同步**
  大厅用户变化时，自动向全体在线用户广播`HallClients`消息
- **心跳检测**
  独立线程每0.1秒检查时间戳，60秒未更新则强制断开
- **战局索引**
  通过自增`ONLINE_ROUND_INDEX`确保全局唯一性 

### 客户端协议处理

#### 消息响应机制

1. **连接阶段**
   - **AllowEnterHall (11)**
     → 存储clientID → 发送`RequestHallClients`向服务端请求大厅用户数据
   - **HallClients (12)**
     → 触发注册的回调函数 → 更新大厅UI列表
2. **对战交互**
   - **SendBattleRequest (3)**
     → 显示弹窗询问用户是否接受对战 → 用户确认后发送`ReplyBattleRequest`
   - **EnterRound (5)**
     → 记录先手状态 → 加载对战场景 → 初始化棋盘
   - **MoveInfo (18)**
     → 调用`GameManager`更新棋盘 → 渲染棋子
3. **数据获取**
   - **ProvideRoundList (8)**
     → 解析历史战局数据 → 显示回放列表

#### 核心机制

- **异步处理**
  接收线程填充`receiveMsgQueue`，主线程`Update`处理消息队列
- **心跳维持**
  `InvokeRepeating`每4秒发送心跳，不等待响应
- **状态保持**
  `_onlineRoundIndex`贯穿整个对战过程，用于服务端战局匹配

### 异常处理设计

// TODO

## 功能拆解

### 战局信息

### 联机大厅

### 联网对战

