/* =========================================================
   MES Demo - 数据库初始化脚本
   目标：SQL Server 2019/2022（Express 亦可）
   说明：脚本会删除并重建 MesDemo 库，仅用于演示
   ========================================================= */

IF DB_ID('MesDemo') IS NOT NULL
BEGIN
    ALTER DATABASE MesDemo SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE MesDemo;
END
GO

CREATE DATABASE MesDemo;
GO

USE MesDemo;
GO

/* ---------------- 基础数据 ---------------- */

CREATE TABLE Material (
    MaterialId   INT IDENTITY(1,1) PRIMARY KEY,
    MaterialCode NVARCHAR(50)  NOT NULL UNIQUE,
    MaterialName NVARCHAR(100) NOT NULL,
    Spec         NVARCHAR(100) NULL,
    CreatedAt    DATETIME2     NOT NULL DEFAULT SYSDATETIME()
);

CREATE TABLE Operation (
    OperationId   INT IDENTITY(1,1) PRIMARY KEY,
    OperationCode NVARCHAR(20) NOT NULL UNIQUE,
    OperationName NVARCHAR(50) NOT NULL,
    SeqNo         INT          NOT NULL
);

CREATE TABLE Routing (
    RoutingId   INT IDENTITY(1,1) PRIMARY KEY,
    RoutingCode NVARCHAR(30)  NOT NULL UNIQUE,
    RoutingName NVARCHAR(100) NOT NULL,
    MaterialId  INT           NOT NULL REFERENCES Material(MaterialId)
);

CREATE TABLE RoutingStep (
    RoutingStepId INT IDENTITY(1,1) PRIMARY KEY,
    RoutingId     INT NOT NULL REFERENCES Routing(RoutingId),
    OperationId   INT NOT NULL REFERENCES Operation(OperationId),
    StepSeq       INT NOT NULL,
    IsKeyStep     BIT NOT NULL DEFAULT 0,
    CONSTRAINT UQ_RoutingStep UNIQUE (RoutingId, StepSeq)
);

CREATE TABLE Workstation (
    WorkstationId   INT IDENTITY(1,1) PRIMARY KEY,
    WorkstationCode NVARCHAR(30) NOT NULL UNIQUE,
    WorkstationName NVARCHAR(50) NOT NULL,
    OperationId     INT NULL REFERENCES Operation(OperationId)
);

/* ---------------- 工单与生产 ---------------- */

CREATE TABLE WorkOrder (
    WorkOrderId  INT IDENTITY(1,1) PRIMARY KEY,
    WorkOrderNo  NVARCHAR(30) NOT NULL UNIQUE,
    MaterialId   INT NOT NULL REFERENCES Material(MaterialId),
    RoutingId    INT NOT NULL REFERENCES Routing(RoutingId),
    PlanQty      INT NOT NULL,
    Status       NVARCHAR(20) NOT NULL DEFAULT 'Created',  -- Created / Released / InProgress / Completed / Closed
    CreatedAt    DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ReleasedAt   DATETIME2 NULL,
    CompletedAt  DATETIME2 NULL
);

CREATE TABLE WorkOrderStep (
    WorkOrderStepId INT IDENTITY(1,1) PRIMARY KEY,
    WorkOrderId     INT NOT NULL REFERENCES WorkOrder(WorkOrderId),
    OperationId     INT NOT NULL REFERENCES Operation(OperationId),
    StepSeq         INT NOT NULL,
    PlanQty         INT NOT NULL,
    DoneQty         INT NOT NULL DEFAULT 0,
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending / InProgress / Completed
    CONSTRAINT UQ_WorkOrderStep UNIQUE (WorkOrderId, StepSeq)
);

CREATE TABLE ProductSN (
    SnId           BIGINT IDENTITY(1,1) PRIMARY KEY,
    SN             NVARCHAR(50) NOT NULL UNIQUE,
    WorkOrderId    INT NOT NULL REFERENCES WorkOrder(WorkOrderId),
    MaterialId     INT NOT NULL REFERENCES Material(MaterialId),
    CurrentStepSeq INT NOT NULL DEFAULT 0,
    Status         NVARCHAR(20) NOT NULL DEFAULT 'InProcess', -- InProcess / Completed / Scrapped
    CreatedAt      DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);

CREATE TABLE PassRecord (
    PassRecordId  BIGINT IDENTITY(1,1) PRIMARY KEY,
    SN            NVARCHAR(50) NOT NULL,
    WorkOrderId   INT NOT NULL,
    OperationId   INT NOT NULL,
    StepSeq       INT NOT NULL,
    WorkstationCode NVARCHAR(30) NULL,
    OperatorName  NVARCHAR(50) NULL,
    Result        NVARCHAR(10) NOT NULL DEFAULT 'OK', -- OK / NG
    PassedAt      DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
CREATE INDEX IX_PassRecord_SN ON PassRecord(SN);
CREATE INDEX IX_PassRecord_WorkOrder ON PassRecord(WorkOrderId);

CREATE TABLE SysUser (
    UserId      INT IDENTITY(1,1) PRIMARY KEY,
    UserName    NVARCHAR(50) NOT NULL UNIQUE,
    DisplayName NVARCHAR(50) NOT NULL,
    Role        NVARCHAR(20) NOT NULL  -- Admin / Operator / QC
);

/* ---------------- 种子数据 ---------------- */

INSERT INTO Material (MaterialCode, MaterialName, Spec) VALUES
('MAT-001', N'前三角臂', N'铝合金 / 左'),
('MAT-002', N'U 臂',     N'钢制 / 右');

INSERT INTO Operation (OperationCode, OperationName, SeqNo) VALUES
('OP10', N'机加',      10),
('OP20', N'旋铆',      20),
('OP30', N'总装',      30),
('OP40', N'压装',      40),
('OP50', N'GP12 检测', 50);

INSERT INTO Routing (RoutingCode, RoutingName, MaterialId)
VALUES ('RT-001', N'前三角臂标准路线', 1);

INSERT INTO RoutingStep (RoutingId, OperationId, StepSeq, IsKeyStep) VALUES
(1, 1, 10, 0),
(1, 2, 20, 1),
(1, 3, 30, 0),
(1, 4, 40, 1),
(1, 5, 50, 1);

INSERT INTO Workstation (WorkstationCode, WorkstationName, OperationId) VALUES
('WS-10', N'机加工位',   1),
('WS-20', N'旋铆工位',   2),
('WS-30', N'总装工位',   3),
('WS-40', N'压装工位',   4),
('WS-50', N'GP12 检测工位', 5);

INSERT INTO SysUser (UserName, DisplayName, Role) VALUES
('admin', N'管理员', 'Admin'),
('op01',  N'张三',   'Operator');

INSERT INTO WorkOrder (WorkOrderNo, MaterialId, RoutingId, PlanQty, Status, ReleasedAt)
VALUES ('WO-20261023-001', 1, 1, 5, 'Released', SYSDATETIME());

INSERT INTO WorkOrderStep (WorkOrderId, OperationId, StepSeq, PlanQty)
SELECT 1, rs.OperationId, rs.StepSeq, 5
FROM RoutingStep rs
WHERE rs.RoutingId = 1;

GO

PRINT N'MesDemo 初始化完成';
SELECT 'WorkOrder' AS TableName, COUNT(*) AS Cnt FROM WorkOrder
UNION ALL SELECT 'WorkOrderStep', COUNT(*) FROM WorkOrderStep
UNION ALL SELECT 'RoutingStep', COUNT(*) FROM RoutingStep;
