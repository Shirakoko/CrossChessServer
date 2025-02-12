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

## 功能拆解

### 连接管理

连接管理模块负责处理客户端的连接和断开，确保服务器的稳定运行。具体功能包括：

1. **客户端连接**：
   - 当客户端连接到服务器时，服务器会为其分配一个唯一的 `clientID`，并将其加入客户端字典 `clientDict`。
   - 服务器会开启心跳检测线程，定期检查客户端是否在线。
2. **客户端断开**：
   - 当客户端断开连接时，服务器会将其从客户端字典 `clientDict` 和大厅用户字典 `hallClientDict` 中移除。
   - 服务器会通知所有在线玩家更新大厅用户列表。
3. **心跳检测**：
   - 服务器会定期检查客户端的心跳消息，如果客户端长时间未发送心跳消息，则判定其已断开连接。
   - 服务器会清理断开连接的客户端，并释放相关资源。

### 联机大厅

联机大厅是玩家点击【进入大厅】后的第一个界面，用于管理玩家的状态、匹配对手以及查看当前在线玩家列表。

1. **玩家进入大厅**：
   - 当玩家成功连接到服务器后，会发送 `EnterHall` 消息，服务器会记录玩家的用户名和客户端ID，并将其加入大厅用户字典 `hallClientDict`。
   - 服务器会向该玩家发送 `AllowEnterHall` 消息，确认其成功进入大厅。
   - 同时，服务器会通知所有在线玩家更新大厅用户列表。
2. **玩家退出大厅**：
   - 当玩家主动退出大厅时，会发送 `QuitHall` 消息，服务器会将其从大厅用户字典 `hallClientDict` 中移除。
   - 服务器会通知所有在线玩家更新大厅用户列表。
3. **大厅用户列表更新**：
   - 当有玩家进入或退出大厅时，服务器会向所有在线玩家广播最新的 `HallClients` 消息，更新大厅用户列表。
   - 玩家可以随时发送 `RequestHallClients` 消息，请求获取当前大厅用户列表。
4. **玩家状态管理**：
   - 玩家在大厅中的状态分为“空闲”和“繁忙”两种。空闲状态的玩家可以接收对战请求，繁忙状态的玩家则不能。
   - 服务器通过 `SetHallClientIdle` 方法更新玩家的状态，并在状态变化时通知所有在线玩家。

### 联网对战

联网对战是游戏的核心功能，玩家可以在大厅中发起对战请求，与其他玩家进行实时对战。

1. **发起对战请求**：
   - 玩家可以选择大厅中的其他空闲玩家，向其发送 `SendBattleRequest` 消息。
   - 服务器会验证目标玩家的状态，如果目标玩家空闲，则转发对战请求，并将其状态设置为“繁忙”。
2. **处理对战请求**：
   - 目标玩家收到对战请求后，可以选择接受或拒绝。
   - 如果目标玩家拒绝，服务器会将其状态重置为“空闲”，并通知发起玩家对战请求被拒绝。
   - 如果目标玩家接受，服务器会创建一个新的战局，并将双方玩家的状态设置为“繁忙”。
3. **进入对战**：
   - 服务器会向双方玩家发送 `EnterRound` 消息，通知他们进入对战界面。
   - 服务器会为对战分配一个唯一的 `onlineRoundIndex`，并初始化对战状态 `OnlineRoundState`，记录双方的玩家ID和棋盘状态。
4. **对战过程**：
   - 对战过程中，玩家每次落子会发送 `MoveInfo` 消息，服务器会更新对战状态，并将落子信息转发给对手。
   - 服务器会检查对战状态，判断是否有玩家获胜或对战是否结束。
5. **结束对战**：
   - 当对战结束时，服务器会向双方玩家发送对战结果，并将他们的状态重置为“空闲”。
   - 服务器会记录对战信息，并将其保存到战局历史中。

### 战局信息

1. **在线战局状态管理：**
   - 使用`onlineRoundDict`字典管理在线战局状态。
   - 收到客户端发来的表示落子信息的`MoveInfo`消息后：
     - 用`GetRiverClientID`从中获取某个客户端的对手客户端ID，并给对手客户端发送落子信息。
     - 用`UpdateOnlineRoundState`更新指定战局中某个格子的状态。
2. **联机对战结果：**
   - 使用`onlineRoundResultDict`字典管理客户端发送过来的联机对战结果。
   - 接收到客户端发来的 `OnlineRoundResult` 消息后，解析对战结果数据。
   - 根据 `roundID` 判断是否已经存在对应的战局信息：
     - 如果不存在，创建新的 `Round` 对象并存入字典。
     - 如果存在，进行校验（`result` 和 `steps`），校验通过后保存到文件并删除字典中的键值对。
3. 序列化和反序列化：
   - 使用 `RoundManager` 静态类对战局信息进行序列化和反序列化：
     - 序列化：`SaveRoundInfo`将 `Round` 对象保存到文件中。
     - 反序列化：`GetRoundList`从文件中读取所有战局信息并返回 `Round` 数组。

## 协议处理

### 协议类型

| 消息ID                 | 数值 | 方向            | 关键行为                                                     |
| :--------------------- | :--- | :-------------- | :----------------------------------------------------------- |
| **EnterHall**          | 1    | 客户端 → 服务端 | 携带用户名初始化连接，触发服务端分配clientID                 |
| **QuitHall**           | 2    | 客户端 → 服务端 | 清除用户在大厅的在线状态                                     |
| **SendBattleRequest**  | 3    | 客户端 ↔ 服务端 | 请求方发送目标clientID，服务端转发请求并标记目标为繁忙       |
| **ReplyBattleRequest** | 4    | 客户端 → 服务端 | 包含接受/拒绝标志，触发战局创建或状态重置                    |
| **EnterRound**         | 5    | 服务端 → 客户端 | 携带先手标识(isPrevPlayer)和战局索引(onlineRoundIndex)       |
| **RoundInfo**          | 6    | 客户端 → 服务端 | 持久化保存到`RoundManager`管理的txt文件                      |
| **RequestRoundList**   | 7    | 客户端 → 服务端 | 触发服务端从文件加载历史数据                                 |
| **ProvideRoundList**   | 8    | 服务端 → 客户端 | 携带所有历史战局的Round对象数组                              |
| **AllowEnterHall**     | 11   | 服务端 → 客户端 | 返回分配的clientID（数值型标识）                             |
| **HallClients**        | 12   | 服务端 → 客户端 | 包含大厅用户字典（clientID-用户名映射）                      |
| **RequestHallClients** | 13   | 客户端 → 服务端 | 手动请求刷新大厅列表                                         |
| **MoveInfo**           | 18   | 客户端 ↔ 服务端 | 包含棋子位置(pos)和战局索引(onlineRoundIndex)                |
| **ClientQuit**         | 99   | 客户端 → 服务端 | 显式断开连接指令                                             |
| **HeartMessage**       | 100  | 客户端 → 服务端 | 维持TCP连接活性                                              |
| **OnlineRoundResult**  | 9    | 客户端 → 服务端 | 战局结束后，双方客户端自动向服务端发送，包含战局ID(roundID)、落子信息(steps)、胜负结果(result) |

### 服务端协议处理

#### 核心消息处理

在 `ClientSocket->HandleMessage` 方法中实现，根据不同的 `MessageID` 来处理不同的消息类型。

1. **战局管理**
   - **RoundInfo (6)**
     → 解析战局数据 → 调用`RoundManager.SaveRoundInfo()`持久化到txt文件

   ```csharp
   case (int)MessageID.RoundInfo:
       Round round = new Round();
       round.ReadFromBytes(buffer, sizeof(int)); // 跳过消息ID
       Console.WriteLine("客户端{0}保存战局", this.clientID);
       RoundManager.SaveRoundInfo(round); // 保存战局信息到txt
       break;
   ```

   - **RequestRoundList (7)**
     → 调用`RoundManager.GetRoundList()`读取文件 → 发送`ProvideRoundList`消息给客户端

   ```csharp
   case (int)MessageID.RequestRoundList:
       Round[] rounds = RoundManager.GetRoundList(); // 收到客户端请求后从txt中读取战局信息
       Console.WriteLine("客户端{0}请求战局信息", this.clientID);
       this.Send(new ProvideRoundList(rounds)); // 把战局信息发送给客户端
       break;
   ```

   - **OnlineRoundResult (9)**

     → 解析对战结果 → 根据 `roundID` 是否在`onlineRoundResultDict`中决定新增或校验 → 校验通过后保存到文件。

   ```csharp
   case (int)MessageID.OnlineRoundResult:
       OnlineRoundResult onlineRoundResult = new OnlineRoundResult();
       onlineRoundResult.ReadFromBytes(buffer, sizeof(int));
       // 战局ID
       int roundIndex = onlineRoundResult.roundID;
       bool isPrevPlayer = onlineRoundResult.isPrevPlayer;
       string playerName = onlineRoundResult.playerName;
       int result = onlineRoundResult.result;
       int[] steps = onlineRoundResult.steps;
       Console.WriteLine("收到客户端{0}发来的联机对战结果，roundID为{1}", clientID, roundIndex);
       if(!ServerSocket.Instance.onlineRoundResultDict.ContainsKey(roundIndex))
       {
           Round newRound = new Round();
           newRound.roundID = roundIndex;
           if(isPrevPlayer) {
               newRound.player1 = playerName;
           } else
           {
               newRound.player2 = playerName;
           }
           newRound.result = result;
   
           // 深拷贝 steps 数组
           newRound.steps = new int[steps.Length];
           Array.Copy(steps, newRound.steps, steps.Length);
   
           // 第一个客户端发来的OnlineRoundResult存入字典
           ServerSocket.Instance.onlineRoundResultDict.Add(roundIndex, newRound);
       }
       else
       {
           // 第二个客户端发来的OnlineRoundResult用于校验
           // 从字典中取出第一个客户端发来的Round
           Round existRound = ServerSocket.Instance.onlineRoundResultDict[roundIndex];
           // 校验结果
           bool pass = true;
           // 校验 result，是否【一个为1一个为2】或【两个都为0】
           if (!((existRound.result == 1 && result == 2) ||
                 (existRound.result == 2 && result == 1) ||
                 (existRound.result == 0 && result == 0))) {
               pass = false;
           }
   
           // 校验 steps，是否【完全相同】
           if (!existRound.steps.SequenceEqual(steps)) {
               pass = false;
           }
   
           // 若校验通过
           if (pass) {
               // 补全剩下那个没有赋值过的 player（1或2）
               if (string.IsNullOrEmpty(existRound.player1))
               {
                   existRound.player1 = playerName;
               }
               else if (string.IsNullOrEmpty(existRound.player2))
               {
                   existRound.player2 = playerName;
               }
   
               // 保存 Round 到 txt 文件
               RoundManager.SaveRoundInfo(existRound);
               Console.WriteLine("战局ID {0} 校验通过并保存成功", roundIndex);
   
               // 从字典中删除该键值对
               ServerSocket.Instance.onlineRoundResultDict.Remove(roundIndex);
               Console.WriteLine("战局ID {0} 已从字典中移除", roundIndex);
           }
       }
       break;
   ```
   
   
   
2. **大厅管理**

   - **EnterHall (1)**
     → 记录用户名 → 分配clientID → 发送`AllowEnterHall` → 加入大厅字典

   ```csharp
   case (int)MessageID.EnterHall:
       EnterHall enterHall = new EnterHall();
       enterHall.ReadFromBytes(buffer, sizeof(int));
       Console.WriteLine("客户端{0}进入大厅，用户名: {1}", this.clientID, enterHall.userName);
       this.Send(new AllowEnterHall(this.clientID)); // 给客户端发送准许进入大厅的消息
       ServerSocket.Instance.AddToHallClientDict(this.clientID, enterHall.userName); // 把进入大厅的客户端信息保存到大厅列表
       break;
   ```

   - **QuitHall (2)**
     → 从大厅字典移除 → 触发全厅广播更新

   ```csharp
   case (int)MessageID.RequestHallClients:
       this.Send(new HallClients(ServerSocket.Instance.hallClientDict)); // 向客户端发送大厅用户数据
       break;
   ```

   - **RequestHallClients (13)**
     → 立即发送当前大厅用户字典给客户端

   ```csharp
   case (int)MessageID.QuitHall:
       Console.WriteLine("客户端{0}退出大厅", this.clientID);
       ServerSocket.Instance.RemoveFromHallClientDict(this.clientID);
       break;
   ```

3. **对战流程**
   - **SendBattleRequest (3)**
     → 验证目标状态 → 转发请求 → 标记目标为`繁忙`

   ```csharp
   case (int)MessageID.SendBattleRequest:
       SendBattleRequest sendBattleRequest = new SendBattleRequest();
       sendBattleRequest.ReadFromBytes(buffer, sizeof(int));
       Console.WriteLine("客户端{0}向客户端{1}发送对战请求", this.clientID, sendBattleRequest.riverClientID);
       // 给被发送请求的客户端发送对战请求
       ServerSocket.Instance.clientDict[sendBattleRequest.riverClientID].Send(
           new SendBattleRequest(this.clientID, sendBattleRequest.senderClientName)); 
       // 把被发送请求的客户端设置成繁忙
       ServerSocket.Instance.SetHallClientIdle(sendBattleRequest.riverClientID, false);
       break;
   ```

   - **ReplyBattleRequest (4)**
     - 拒绝：重置双方为`空闲`
     - 接受：
       → 创建`OnlineRoundState`对象 → 分配全局`onlineRoundIndex`
       → 标记双方为`繁忙` → 发送`EnterRound`（先手标记不同） → 创建战局

   ```csharp
   case (int)MessageID.ReplyBattleRequest:
       ReplyBattleRequest replyBattleRequest = new ReplyBattleRequest();
       replyBattleRequest.ReadFromBytes(buffer, sizeof(int));
       int riverClientID = replyBattleRequest.riverClientID;
       bool accept = replyBattleRequest.accept;
       Console.WriteLine("客户端{0}回复客户端{1}的对战请求，是否接受: {2}", this.clientID, riverClientID, accept);
       if(!accept) {
           // TODO 如果是false，把客户端{0}设置成空闲，给客户端{1}发送"你被拒绝了"
           ServerSocket.Instance.SetHallClientIdle(this.clientID, true);
       } else {
           // 如果是true，把双方设置成繁忙
           ServerSocket.Instance.SetHallClientIdle(this.clientID, false);
           ServerSocket.Instance.SetHallClientIdle(riverClientID, false);
   
           // 通知双方进入对战
           this.Send(new EnterRound(true, ONLINE_ROUND_INDEX)); // 被请求方是先手
           ServerSocket.Instance.clientDict[riverClientID].Send(new EnterRound(false, ONLINE_ROUND_INDEX)); // 请求方是后手
   
           // 创建战局
           ServerSocket.Instance.onlineRoundDict.Add(ONLINE_ROUND_INDEX, new OnlineRoundState(this.clientID, riverClientID));
           ONLINE_ROUND_INDEX++;
       }
       break;
   ```

   - **MoveInfo (18)**
     → 校验对战合法性 → 更新棋盘状态/战局信息 → 转发给对手客户端

   ```csharp
   case (int)MessageID.MoveInfo:
       MoveInfo moveInfo = new MoveInfo();
       moveInfo.ReadFromBytes(buffer, sizeof(int));
       // 更新战局信息
       ServerSocket.Instance.UpdateOnlineRoundState(moveInfo.onlineRoundIndex, moveInfo.pos, this.clientID);
       // 从战局信息中获取对手的clientID
       int riverID = ServerSocket.Instance.GetRiverClient(moveInfo.onlineRoundIndex, this.clientID);
       // 给它的对手发送同样的落子信息
       ServerSocket.Instance.clientDict[riverID].Send(moveInfo);
       break;
   ```

1. **连接管理**
   - **ClientQuit (99)**
     → 清除数据（大厅&战局） → 关闭Socket
   - **HeartMessage (100)**
     → 更新`lastHeartbeatTime`时间戳

   ```csharp
   case (int)MessageID.ClientQuit:
       Console.WriteLine("客户端{0}发来断开连接", this.clientID);
       ServerSocket.Instance.RemoveClient(this.clientID);
       this.Close();
       break;
   case (int)MessageID.HeartMessage:
       lastHeartbeatTime = DateTime.UtcNow;
       Console.WriteLine($"Heartbeat received. lastHeartbeatTime updated to: {lastHeartbeatTime}");
       break;
   ```

#### 技术细节

- **状态同步**
  大厅用户变化（新增、删除、闲忙状态改变）时，自动向全体在线用户广播`HallClients`消息
  
  ```csharp
  public void AddToHallClientDict(int clientID, string name)
  {
      lock (_hallClientDictLock)
      {
          if (hallClientDict.ContainsKey(clientID) == false)
          {
              // 默认用户是空闲的
              UserInfo userInfo = new UserInfo(name, true);
              hallClientDict.Add(clientID, userInfo);
          }
      }
  
      // 通知所有客户端大厅用户数据变化
      foreach (int clinetID in hallClientDict.Keys)
      {
          clientDict[clinetID].SendHallClients();
      }
  }
  
  public void RemoveFromHallClientDict(int clientID)
  {
      if (hallClientDict.ContainsKey(clientID))
      {
          hallClientDict.Remove(clientID);
      }
      // 通知所有客户端大厅用户数据变化
      foreach (int clinetID in hallClientDict.Keys)
      {
          clientDict[clinetID].SendHallClients();
      }
  }
  
  public void SetHallClientIdle(int clientID, bool isIdle)
  {
      if (hallClientDict.ContainsKey(clientID))
      {
          // 获取当前用户信息
          UserInfo userInfo = hallClientDict[clientID];
          // 更新用户状态
          userInfo.IsIdle = isIdle;
          // 将更新后的用户信息重新存入字典
          hallClientDict[clientID] = userInfo;
      }
  
      // 通知所有客户端大厅用户数据变化
      foreach (int clinetID in hallClientDict.Keys)
      {
          clientDict[clinetID].SendHallClients();
      }
  }
  ```
  
- **心跳检测**
  在客户端连入后，立即开启心跳检测，用独立线程每0.1秒检查时间戳，60秒未更新则强制断开
  
  ```csharp
  /// <summary>
  /// 开启心跳消息检测的线程
  /// </summary>
  public void StartCheckTimeOut()
  {
      ThreadPool.QueueUserWorkItem(CheckHeartMessage);
  }
  
  // 线程方法，每0.1s检测一次心跳消息超时
  private void CheckHeartMessage(object obj)
  {
      while(isConnected)
      {
          CheckHeartMessageTimeOut();
          Thread.Sleep(100);
      }
  }
  private void CheckHeartMessageTimeOut()
  {
      if (lastHeartbeatTime == DateTime.MinValue || !isConnected)
      {
          return;
      }
      TimeSpan timeSpan = DateTime.UtcNow - lastHeartbeatTime;
      if (timeSpan.TotalSeconds > TIME_OUT)
      {
          Console.WriteLine("客户端{0}心跳超时，即将断开连接", this.clientID);
          ServerSocket.Instance.RemoveClient(this.clientID);
          this.Close();
      }
  }
  ```
  
- **战局索引：**通过自增`ONLINE_ROUND_INDEX`确保全局唯一性。
  
  ```csharp
  public static int ONLINE_ROUND_INDEX = 1001;
  
  public void CreateNewRound(int player1ID, int player2ID)
  {
      int roundID = ONLINE_ROUND_INDEX++;
      OnlineRoundState newRound = new OnlineRoundState(player1ID, player2ID);
      onlineRoundDict.Add(roundID, newRound);
      Console.WriteLine("新战局创建，ID: {0}", roundID);
  }
  ```
  
- **线程锁：**多个线程可能会同时访问共享资源（如`clientDict`和`hallClientDict`），导致`foreach`报错；使用了线程锁（`lock`）来确保同一时间只有一个线程可以访问这些共享资源。

  ```csharp
  private readonly object _clientDictLock = new object();
  private readonly object _hallClientDictLock = new object();
  
  public void RemoveClient(int clientID)
  {
      lock (_clientDictLock)
      {
          if (clientDict.ContainsKey(clientID))
          {
              clientDict.Remove(clientID);
              Console.WriteLine("客户端{0}从字典中移除", clientID);
          }
      }
  
      lock (_hallClientDictLock)
      {
          if (hallClientDict.ContainsKey(clientID))
          {
              hallClientDict.Remove(clientID);
              Console.WriteLine("客户端{0}从大厅用户字典中移除", clientID);
  
              // 通知所有客户端大厅用户数据变化
              foreach (int clinetID in hallClientDict.Keys)
              {
                  clientDict[clinetID].SendHallClients();
              }
          }
      }
  }
  ```

## **异常处理**

### **客户端断线重连**

//TODO
