# MES Demo — 生产执行系统（求职作品）

一个可运行的 MES 最小闭环：**工单下达 → 开工 → 报工 → 工序卡控 → 序列号追溯 → 完工报表**。

- 技术栈：C# / .NET 8 WinForms / SQL Server（LocalDB）/ ADO.NET
- 代码量：约 2000 行，单人完成
- 演示重点：跳站、重复过站、NG 不推进这三类车间异常，系统怎么拦住

---

## 1. 目录结构

```
MES-Demo/
├─ README.md                 本文件
├─ docs/
│  └─ 设计说明.md            业务主线、状态机、7 条卡控规则、数据模型、讲解脚本
├─ db/
│  └─ init.sql               建库建表 + 种子数据（可重复执行，会重建库）
└─ src/
   └─ MesDemo/
      ├─ MesDemo.csproj
      ├─ Program.cs          入口；支持 --selftest
      ├─ Data/Db.cs          连接与查询封装
      ├─ Models/Models.cs    实体
      ├─ Services/
      │  ├─ MesService.cs    核心业务：工单、SN、报工、卡控、追溯、报表
      │  └─ SelfTest.cs      无界面自测（16 项业务断言 + UI 冒烟）
      └─ Forms/              五个 WinForm 页面
```

---

## 2. 运行环境

| 组件 | 版本 | 说明 |
|---|---|---|
| Windows | 10 / 11 | WinForms 仅支持 Windows |
| .NET SDK | 8.0+ | 编译需要；只运行可装 .NET 8 Desktop Runtime |
| SQL Server | 2019/2022 Express 或 LocalDB | 默认连接 `(localdb)\MSSQLLocalDB` |

### 安装 LocalDB（如果没有 SQL Server）

下载 `SqlLocalDB.msi` 后安装：

```powershell
msiexec /i SqlLocalDB.msi /qn IACCEPTSQLLOCALDBLICENSETERMS=YES
sqllocaldb info
```

### 如果用的是 SQLEXPRESS 实例

改 `src/MesDemo/Data/Db.cs` 里的默认连接串，或设置环境变量：

```powershell
$env:MES_CONN = "Server=.\SQLEXPRESS;Database=MesDemo;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False"
```

---

## 3. 首次运行

```powershell
# 1. 建库（会重建 MesDemo，慎用）
sqlcmd -S "(localdb)\MSSQLLocalDB" -i db\init.sql
# 没有 sqlcmd 时，用 SSMS / Navicat 直接执行 db\init.sql

# 2. 编译
dotnet build src\MesDemo\MesDemo.csproj -c Release

# 3. 运行
src\MesDemo\bin\Release\net8.0-windows\MesDemo.exe
```

---

## 4. 三分钟演示脚本

1. **工单管理**：看到 `WO-20261023-001 / 前三角臂 / 5 件 / 已下达`
2. **生产报工** → 选择工单 → 工位选「机加工位」→ 生成 SN（5 个）
3. 输入 `WO-20261023-001-0001` → 回车，界面显示「当前应过：机加（10）」
4. 点 **OK 过站** → 成功，提示下一道「旋铆（20）」
5. **演示跳站**：工位改选「总装工位」，再扫同一个 SN → 红字提示「不能跳站：当前应过旋铆（20）」
6. **演示重复**：工位改回「机加工位」，再扫 → 提示「不能重复过站」
7. **演示 NG**：工位选「旋铆工位」→ 点「记录 NG」→ 提示已记录、产品不推进
8. **追溯查询**：输入 SN → 看到 5 条过站记录（含 1 条 NG）；输入工单号 → 看到 5 个 SN 状态
9. **生产报表**：工单完工率、各工序完成数

---

## 5. 自测

```powershell
src\MesDemo\bin\Release\net8.0-windows\MesDemo.exe --selftest
type src\MesDemo\bin\Release\net8.0-windows\selftest.log
```

覆盖：

- 建单、下达、开工、批量生成 SN
- 未下达禁止建 SN（R1）
- 跳站拒绝（R3）
- 重复过站拒绝（R4）
- NG 记录但不推进（R6）
- 逐序报工、SN 完工、追溯记录数
- 全部 SN 完工后工单自动完工
- 五个界面可正常创建（UI 冒烟）

当前结果：**17 项全部通过**。自测结束会自动清理测试工单。

---

## 6. 核心设计（面试可讲）

### 卡控规则

| 规则 | 场景 | 系统行为 |
|---|---|---|
| R1 | 工单未下达 | 禁止建 SN / 报工 |
| R2 | SN 不存在或不属于工单 | 拒绝 |
| R3 | 跳站 | 提示当前应过工序，拒绝 |
| R4 | 重复过站 | 拒绝 |
| R5 | 上道未完成 | 由顺序校验保证 |
| R6 | NG | 记录但不推进 |
| R7 | 超过计划数 | 拒绝建 SN |

### 为什么先判重复、再判跳站

如果先算“下一道应过工序”，再比对扫描工序，那么“重复扫上一道”会被误报成“跳站”，现场操作工看到提示会以为是工艺路线错了。正确顺序是：

1. 先查这个 SN 是否已经完成扫描的工序 → 重复
2. 再查它是不是当前应过的工序 → 跳站

这是自测里真实暴露并修掉的一个 bug，可以作为一个“我怎么发现问题”的例子讲。

### 事务边界

报工一次写三张表：`PassRecord`（流水）、`ProductSN`（当前工序）、`WorkOrderStep`（完成数），同一事务提交。避免“记录写了但状态没推进”。

### 追溯

- 正向：SN → 全部过站记录（工序、工位、操作人、结果、时间）
- 反向：工单 → 所有 SN 及状态

---

## 7. 已知限制（如实说明）

- 没有登录与权限控制，操作人只是文本框
- 没有返修流程，NG 只记录不闭环
- 没有并发压力测试，`UPDLOCK` 只是基础防护
- 报表是实时查询，没有做汇总表
- 工艺路线只支持线性路线，不支持分支/返工跳转

这些不是“没想到”，是刻意收窄范围；面试时主动说出来，比被问出来好。

---

## 8. 后续扩展

质量（检验项/缺陷）、Andon（异常呼叫/升级）、WMS（库位/出入库）、权限（角色/按钮级）、设备采集（OPC UA/Modbus 模拟）、Web 化（ASP.NET Core Web API + Vue）。

---

## 9. 面试讲解（30 秒版）

> 这是我做的一个 MES 最小闭环，核心不是增删改查，是用系统规则约束现场作业。工单下达后每个产品分配唯一 SN，按工艺路线逐序过站；每次报工系统算下一道应过工序，和扫描的工序比对，不一致就拒绝。这条规则对应我在拓普遇到的 GP12 漏序——现场网络波动或扫 A 做 B 会导致检测工序没记录，如果系统按路线强制卡控，漏序在下一站就会被拦住。数据上是三张核心表，报工在一个事务里更新，追溯支持 SN 和工单两个方向。
