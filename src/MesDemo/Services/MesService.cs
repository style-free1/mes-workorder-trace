using MesDemo.Data;
using MesDemo.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace MesDemo.Services;

public class MesService
{
    /* ---------------- 基础数据 ---------------- */

    public List<Material> GetMaterials()
    {
        var dt = Db.Query("SELECT MaterialId, MaterialCode, MaterialName, Spec FROM Material ORDER BY MaterialId");
        return dt.AsEnumerable().Select(r => new Material
        {
            MaterialId = r.Field<int>("MaterialId"),
            MaterialCode = r.Field<string>("MaterialCode"),
            MaterialName = r.Field<string>("MaterialName"),
            Spec = r.IsNull("Spec") ? null : r.Field<string>("Spec")
        }).ToList();
    }

    public List<Operation> GetOperations()
    {
        var dt = Db.Query("SELECT OperationId, OperationCode, OperationName, SeqNo FROM Operation ORDER BY SeqNo");
        return dt.AsEnumerable().Select(r => new Operation
        {
            OperationId = r.Field<int>("OperationId"),
            OperationCode = r.Field<string>("OperationCode"),
            OperationName = r.Field<string>("OperationName"),
            SeqNo = r.Field<int>("SeqNo")
        }).ToList();
    }

    public List<Workstation> GetWorkstations()
    {
        var dt = Db.Query(@"SELECT s.WorkstationId, s.WorkstationCode, s.WorkstationName, s.OperationId, o.OperationName
                            FROM Workstation s LEFT JOIN Operation o ON s.OperationId = o.OperationId
                            ORDER BY s.WorkstationCode");
        return dt.AsEnumerable().Select(r => new Workstation
        {
            WorkstationId = r.Field<int>("WorkstationId"),
            WorkstationCode = r.Field<string>("WorkstationCode"),
            WorkstationName = r.Field<string>("WorkstationName"),
            OperationId = r.IsNull("OperationId") ? null : r.Field<int>("OperationId"),
            OperationName = r.IsNull("OperationName") ? null : r.Field<string>("OperationName")
        }).ToList();
    }

    public List<WorkOrder> GetWorkOrders()
    {
        var dt = Db.Query(@"SELECT w.WorkOrderId, w.WorkOrderNo, w.MaterialId, m.MaterialName,
                                   w.RoutingId, w.PlanQty, w.Status, w.CreatedAt, w.ReleasedAt, w.CompletedAt
                            FROM WorkOrder w JOIN Material m ON w.MaterialId = m.MaterialId
                            ORDER BY w.WorkOrderId DESC");
        return dt.AsEnumerable().Select(r => new WorkOrder
        {
            WorkOrderId = r.Field<int>("WorkOrderId"),
            WorkOrderNo = r.Field<string>("WorkOrderNo"),
            MaterialId = r.Field<int>("MaterialId"),
            MaterialName = r.Field<string>("MaterialName"),
            RoutingId = r.Field<int>("RoutingId"),
            PlanQty = r.Field<int>("PlanQty"),
            Status = r.Field<string>("Status"),
            CreatedAt = r.Field<DateTime>("CreatedAt"),
            ReleasedAt = r.IsNull("ReleasedAt") ? null : r.Field<DateTime>("ReleasedAt"),
            CompletedAt = r.IsNull("CompletedAt") ? null : r.Field<DateTime>("CompletedAt")
        }).ToList();
    }

    public int? GetRoutingIdByMaterial(int materialId)
    {
        var o = Db.Scalar("SELECT TOP 1 RoutingId FROM Routing WHERE MaterialId = @m ORDER BY RoutingId",
            new SqlParameter("@m", materialId));
        return o == null ? null : Convert.ToInt32(o);
    }

    public List<WorkOrderStep> GetWorkOrderSteps(int workOrderId)
    {
        var dt = Db.Query(@"SELECT ws.WorkOrderStepId, ws.WorkOrderId, ws.OperationId, o.OperationName,
                                   ws.StepSeq, ws.PlanQty, ws.DoneQty, ws.Status
                            FROM WorkOrderStep ws JOIN Operation o ON ws.OperationId = o.OperationId
                            WHERE ws.WorkOrderId = @id ORDER BY ws.StepSeq",
            new SqlParameter("@id", workOrderId));
        return dt.AsEnumerable().Select(r => new WorkOrderStep
        {
            WorkOrderStepId = r.Field<int>("WorkOrderStepId"),
            WorkOrderId = r.Field<int>("WorkOrderId"),
            OperationId = r.Field<int>("OperationId"),
            OperationName = r.Field<string>("OperationName"),
            StepSeq = r.Field<int>("StepSeq"),
            PlanQty = r.Field<int>("PlanQty"),
            DoneQty = r.Field<int>("DoneQty"),
            Status = r.Field<string>("Status")
        }).ToList();
    }

    /* ---------------- 工单 ---------------- */

    public int CreateWorkOrder(string workOrderNo, int materialId, int planQty)
    {
        var routingId = GetRoutingIdByMaterial(materialId)
            ?? throw new InvalidOperationException("该物料没有配置工艺路线，无法创建工单");

        using var conn = Db.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            int workOrderId;
            using (var cmd = new SqlCommand(@"INSERT INTO WorkOrder (WorkOrderNo, MaterialId, RoutingId, PlanQty, Status)
                                              OUTPUT INSERTED.WorkOrderId
                                              VALUES (@no, @mid, @rid, @qty, 'Created')", conn, tx))
            {
                cmd.Parameters.AddWithValue("@no", workOrderNo.Trim());
                cmd.Parameters.AddWithValue("@mid", materialId);
                cmd.Parameters.AddWithValue("@rid", routingId);
                cmd.Parameters.AddWithValue("@qty", planQty);
                workOrderId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            using (var cmd = new SqlCommand(@"INSERT INTO WorkOrderStep (WorkOrderId, OperationId, StepSeq, PlanQty)
                                              SELECT @id, rs.OperationId, rs.StepSeq, @qty
                                              FROM RoutingStep rs WHERE rs.RoutingId = @rid", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", workOrderId);
                cmd.Parameters.AddWithValue("@qty", planQty);
                cmd.Parameters.AddWithValue("@rid", routingId);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
            return workOrderId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void ReleaseWorkOrder(int workOrderId)
    {
        using var conn = Db.Open();
        using var cmd = new SqlCommand(@"UPDATE WorkOrder SET Status = 'Released', ReleasedAt = SYSDATETIME()
                                         WHERE WorkOrderId = @id AND Status = 'Created'", conn);
        cmd.Parameters.AddWithValue("@id", workOrderId);
        if (cmd.ExecuteNonQuery() == 0)
            throw new InvalidOperationException("只有“已创建”状态的工单可以下达");
    }

    public void StartWorkOrder(int workOrderId)
    {
        using var conn = Db.Open();
        using var cmd = new SqlCommand(@"UPDATE WorkOrder SET Status = 'InProgress'
                                         WHERE WorkOrderId = @id AND Status = 'Released'", conn);
        cmd.Parameters.AddWithValue("@id", workOrderId);
        if (cmd.ExecuteNonQuery() == 0)
            throw new InvalidOperationException("只有“已下达”状态的工单可以开工");
    }

    /* ---------------- 序列号 ---------------- */

    public int CreateSnBatch(int workOrderId, int count)
    {
        if (count <= 0) throw new ArgumentException("数量必须大于 0");

        using var conn = Db.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            string workOrderNo;
            int planQty;
            string status;
            using (var cmd = new SqlCommand("SELECT WorkOrderNo, PlanQty, Status FROM WorkOrder WHERE WorkOrderId = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", workOrderId);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) throw new InvalidOperationException("工单不存在");
                workOrderNo = r.GetString(0);
                planQty = r.GetInt32(1);
                status = r.GetString(2);
            }

            if (status == "Created") throw new InvalidOperationException("工单未下达，不能创建序列号");
            if (status is "Completed" or "Closed") throw new InvalidOperationException("工单已完工或关闭，不能创建序列号");

            int existing;
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM ProductSN WHERE WorkOrderId = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", workOrderId);
                existing = Convert.ToInt32(cmd.ExecuteScalar());
            }

            if (existing + count > planQty)
                throw new InvalidOperationException($"超过工单计划数量：计划 {planQty}，已创建 {existing}，本次 {count}");

            using var insert = new SqlCommand(@"INSERT INTO ProductSN (SN, WorkOrderId, MaterialId, CurrentStepSeq, Status)
                                                SELECT @sn, w.WorkOrderId, w.MaterialId, 0, 'InProcess'
                                                FROM WorkOrder w WHERE w.WorkOrderId = @id", conn, tx);
            insert.Parameters.Add("@sn", SqlDbType.NVarChar, 50);
            insert.Parameters.AddWithValue("@id", workOrderId);

            for (int i = 1; i <= count; i++)
            {
                insert.Parameters["@sn"].Value = $"{workOrderNo}-{(existing + i):D4}";
                insert.ExecuteNonQuery();
            }

            tx.Commit();
            return count;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /* ---------------- 核心：报工与工序卡控 ---------------- */

    public PassResult PassStation(string sn, int operationId, string workstationCode, string operatorName, bool isOk)
    {
        sn = (sn ?? "").Trim();
        if (sn.Length == 0) return new PassResult(false, "请扫描或输入产品序列号");

        using var conn = Db.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted);

        try
        {
            // R2: SN 必须存在，并带出工单状态
            long snId; int workOrderId, currentStepSeq, routingId, planQty;
            string snStatus, woStatus;
            using (var cmd = new SqlCommand(@"
                SELECT s.SnId, s.WorkOrderId, s.CurrentStepSeq, s.Status AS SnStatus,
                       w.RoutingId, w.PlanQty, w.Status AS WoStatus
                FROM ProductSN s WITH (UPDLOCK, ROWLOCK)
                JOIN WorkOrder w ON s.WorkOrderId = w.WorkOrderId
                WHERE s.SN = @sn", conn, tx))
            {
                cmd.Parameters.AddWithValue("@sn", sn);
                using var r = cmd.ExecuteReader();
                if (!r.Read())
                {
                    tx.Rollback();
                    return new PassResult(false, $"SN 不存在：{sn}");
                }
                snId = r.GetInt64(0);
                workOrderId = r.GetInt32(1);
                currentStepSeq = r.GetInt32(2);
                snStatus = r.GetString(3);
                routingId = r.GetInt32(4);
                planQty = r.GetInt32(5);
                woStatus = r.GetString(6);
            }

            // R1: 工单必须已下达
            if (woStatus == "Created")
            {
                tx.Rollback();
                return new PassResult(false, "工单未下达，不能报工");
            }
            if (woStatus is "Completed" or "Closed")
            {
                tx.Rollback();
                return new PassResult(false, "工单已完工或关闭，不能报工");
            }
            if (snStatus == "Completed")
            {
                tx.Rollback();
                return new PassResult(false, "该产品已完工，不能重复报工");
            }
            if (snStatus == "Scrapped")
            {
                tx.Rollback();
                return new PassResult(false, "该产品已报废，不能报工");
            }

            // 取工艺路线，计算下一道应过工序
            var steps = new List<(int OperationId, int StepSeq, string OperationName)>();
            using (var cmd = new SqlCommand(@"
                SELECT rs.OperationId, rs.StepSeq, o.OperationName
                FROM RoutingStep rs JOIN Operation o ON rs.OperationId = o.OperationId
                WHERE rs.RoutingId = @rid ORDER BY rs.StepSeq", conn, tx))
            {
                cmd.Parameters.AddWithValue("@rid", routingId);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    steps.Add((r.GetInt32(0), r.GetInt32(1), r.GetString(2)));
            }

            // 扫描的工序必须属于当前工单的工艺路线
            var scanned = steps.FirstOrDefault(s => s.OperationId == operationId);
            if (scanned.OperationId == 0)
            {
                tx.Rollback();
                return new PassResult(false, "扫描的工序不属于当前工单的工艺路线");
            }

            // R4: 不能重复过站（先判重复，再判跳站，提示才准确）
            using (var cmd = new SqlCommand(@"SELECT COUNT(*) FROM PassRecord
                                              WHERE SN = @sn AND StepSeq = @seq AND Result = 'OK'", conn, tx))
            {
                cmd.Parameters.AddWithValue("@sn", sn);
                cmd.Parameters.AddWithValue("@seq", scanned.StepSeq);
                if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                {
                    tx.Rollback();
                    return new PassResult(false, $"该产品已完成「{scanned.OperationName}（{scanned.StepSeq}）」，不能重复过站");
                }
            }

            var next = steps.Where(s => s.StepSeq > currentStepSeq).OrderBy(s => s.StepSeq).FirstOrDefault();
            if (next.OperationId == 0)
            {
                tx.Rollback();
                return new PassResult(false, "该产品已完成工艺路线上的全部工序");
            }

            // R3: 不能跳站 / 顺序错误
            if (next.OperationId != operationId)
            {
                tx.Rollback();
                return new PassResult(false,
                    $"不能跳站：当前应过「{next.OperationName}（{next.StepSeq}）」，扫描的是「{scanned.OperationName}（{scanned.StepSeq}）」");
            }

            // R6: NG 只记录，不推进
            if (!isOk)
            {
                using (var cmd = new SqlCommand(@"INSERT INTO PassRecord
                        (SN, WorkOrderId, OperationId, StepSeq, WorkstationCode, OperatorName, Result)
                        VALUES (@sn, @wo, @op, @seq, @ws, @user, 'NG')", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@sn", sn);
                    cmd.Parameters.AddWithValue("@wo", workOrderId);
                    cmd.Parameters.AddWithValue("@op", operationId);
                    cmd.Parameters.AddWithValue("@seq", next.StepSeq);
                    cmd.Parameters.AddWithValue("@ws", (object?)workstationCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@user", (object?)operatorName ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
                return new PassResult(false, $"已记录不良（NG）：{next.OperationName}，产品不推进，请走返修流程");
            }

            // R5 + 正常过站：写记录、推进 SN、更新工序完成数
            using (var cmd = new SqlCommand(@"INSERT INTO PassRecord
                    (SN, WorkOrderId, OperationId, StepSeq, WorkstationCode, OperatorName, Result)
                    VALUES (@sn, @wo, @op, @seq, @ws, @user, 'OK')", conn, tx))
            {
                cmd.Parameters.AddWithValue("@sn", sn);
                cmd.Parameters.AddWithValue("@wo", workOrderId);
                cmd.Parameters.AddWithValue("@op", operationId);
                cmd.Parameters.AddWithValue("@seq", next.StepSeq);
                cmd.Parameters.AddWithValue("@ws", (object?)workstationCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@user", (object?)operatorName ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            bool isLast = next.StepSeq == steps.Max(s => s.StepSeq);
            using (var cmd = new SqlCommand(@"UPDATE ProductSN
                                              SET CurrentStepSeq = @seq, Status = @status
                                              WHERE SnId = @snid", conn, tx))
            {
                cmd.Parameters.AddWithValue("@seq", next.StepSeq);
                cmd.Parameters.AddWithValue("@status", isLast ? "Completed" : "InProcess");
                cmd.Parameters.AddWithValue("@snid", snId);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SqlCommand(@"UPDATE WorkOrderStep
                                              SET DoneQty = DoneQty + 1,
                                                  Status = CASE WHEN DoneQty + 1 >= PlanQty THEN 'Completed' ELSE 'InProgress' END
                                              WHERE WorkOrderId = @wo AND StepSeq = @seq", conn, tx))
            {
                cmd.Parameters.AddWithValue("@wo", workOrderId);
                cmd.Parameters.AddWithValue("@seq", next.StepSeq);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SqlCommand(@"UPDATE WorkOrder SET Status = 'InProgress'
                                              WHERE WorkOrderId = @id AND Status = 'Released'", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", workOrderId);
                cmd.ExecuteNonQuery();
            }

            // 所有工序都达到计划数 → 工单完工
            using (var cmd = new SqlCommand(@"UPDATE WorkOrder SET Status = 'Completed', CompletedAt = SYSDATETIME()
                                              WHERE WorkOrderId = @id
                                                AND NOT EXISTS (SELECT 1 FROM WorkOrderStep
                                                                WHERE WorkOrderId = @id AND Status <> 'Completed')", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", workOrderId);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();

            var nextNext = steps.Where(s => s.StepSeq > next.StepSeq).OrderBy(s => s.StepSeq).FirstOrDefault();
            if (isLast)
                return new PassResult(true, $"过站成功：{next.OperationName}（{next.StepSeq}），产品已完工", "产品已完工");
            return new PassResult(true, $"过站成功：{next.OperationName}（{next.StepSeq}）",
                $"下一道：{nextNext.OperationName}（{nextNext.StepSeq}）");
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /* ---------------- 追溯与报表 ---------------- */

    public SnNextInfo? GetSnNextStep(string sn)
    {
        var dt = Db.Query(@"SELECT s.SN, s.CurrentStepSeq, s.Status, w.WorkOrderNo, m.MaterialName, w.RoutingId
                            FROM ProductSN s
                            JOIN WorkOrder w ON s.WorkOrderId = w.WorkOrderId
                            JOIN Material m ON w.MaterialId = m.MaterialId
                            WHERE s.SN = @sn", new SqlParameter("@sn", (sn ?? "").Trim()));
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        int cur = row.Field<int>("CurrentStepSeq");
        int routingId = row.Field<int>("RoutingId");

        var next = Db.Query(@"SELECT TOP 1 rs.OperationId, rs.StepSeq, o.OperationName
                              FROM RoutingStep rs JOIN Operation o ON rs.OperationId = o.OperationId
                              WHERE rs.RoutingId = @rid AND rs.StepSeq > @cur
                              ORDER BY rs.StepSeq",
            new SqlParameter("@rid", routingId), new SqlParameter("@cur", cur));

        if (next.Rows.Count == 0)
        {
            return new SnNextInfo(row.Field<string>("SN"), row.Field<string>("WorkOrderNo"),
                row.Field<string>("MaterialName"), cur, "已完成全部工序", 0, 0, row.Field<string>("Status"));
        }

        return new SnNextInfo(
            row.Field<string>("SN"),
            row.Field<string>("WorkOrderNo"),
            row.Field<string>("MaterialName"),
            cur,
            next.Rows[0].Field<string>("OperationName"),
            next.Rows[0].Field<int>("StepSeq"),
            next.Rows[0].Field<int>("OperationId"),
            row.Field<string>("Status"));
    }


    public DataTable GetMaterialTable() =>
        Db.Query("SELECT MaterialCode AS 物料编码, MaterialName AS 物料名称, Spec AS 规格 FROM Material ORDER BY MaterialId");

    public DataTable GetOperationTable() =>
        Db.Query("SELECT OperationCode AS 工序编码, OperationName AS 工序名称, SeqNo AS 工序序号 FROM Operation ORDER BY SeqNo");

    public DataTable GetRoutingTable() =>
        Db.Query(@"SELECT r.RoutingCode AS 路线编码, r.RoutingName AS 路线名称, m.MaterialName AS 适用物料,
                          rs.StepSeq AS 工序号, o.OperationName AS 工序, o.OperationCode AS 工序编码
                   FROM Routing r
                   JOIN Material m ON r.MaterialId = m.MaterialId
                   JOIN RoutingStep rs ON rs.RoutingId = r.RoutingId
                   JOIN Operation o ON rs.OperationId = o.OperationId
                   ORDER BY r.RoutingId, rs.StepSeq");

    public DataTable GetWorkstationTable() =>
        Db.Query(@"SELECT s.WorkstationCode AS 工位编码, s.WorkstationName AS 工位名称, o.OperationName AS 绑定工序
                   FROM Workstation s LEFT JOIN Operation o ON s.OperationId = o.OperationId
                   ORDER BY s.WorkstationCode");

    public List<PassRecord> TraceBySn(string sn)
    {
        var dt = Db.Query(@"SELECT p.PassRecordId, p.SN, p.StepSeq, o.OperationName,
                                   p.WorkstationCode, p.OperatorName, p.Result, p.PassedAt
                            FROM PassRecord p JOIN Operation o ON p.OperationId = o.OperationId
                            WHERE p.SN = @sn ORDER BY p.PassedAt, p.PassRecordId",
            new SqlParameter("@sn", sn.Trim()));
        return dt.AsEnumerable().Select(r => new PassRecord
        {
            PassRecordId = r.Field<long>("PassRecordId"),
            SN = r.Field<string>("SN"),
            StepSeq = r.Field<int>("StepSeq"),
            OperationName = r.Field<string>("OperationName"),
            WorkstationCode = r.IsNull("WorkstationCode") ? null : r.Field<string>("WorkstationCode"),
            OperatorName = r.IsNull("OperatorName") ? null : r.Field<string>("OperatorName"),
            Result = r.Field<string>("Result"),
            PassedAt = r.Field<DateTime>("PassedAt")
        }).ToList();
    }

    public ProductSn? GetSn(string sn)
    {
        var dt = Db.Query(@"SELECT s.SnId, s.SN, s.WorkOrderId, s.CurrentStepSeq, s.Status, s.CreatedAt,
                                   w.WorkOrderNo, m.MaterialName, w.PlanQty
                            FROM ProductSN s
                            JOIN WorkOrder w ON s.WorkOrderId = w.WorkOrderId
                            JOIN Material m ON s.MaterialId = m.MaterialId
                            WHERE s.SN = @sn", new SqlParameter("@sn", sn.Trim()));
        if (dt.Rows.Count == 0) return null;
        var r = dt.Rows[0];
        return new ProductSn
        {
            SnId = r.Field<long>("SnId"),
            SN = r.Field<string>("SN"),
            WorkOrderId = r.Field<int>("WorkOrderId"),
            CurrentStepSeq = r.Field<int>("CurrentStepSeq"),
            Status = r.Field<string>("Status"),
            CreatedAt = r.Field<DateTime>("CreatedAt")
        };
    }

    public DataTable GetSnTable(string workOrderNo)
    {
        return Db.Query(@"SELECT s.SN, s.CurrentStepSeq AS 当前工序序号, s.Status AS 状态,
                                 CASE s.Status WHEN 'InProcess' THEN N'在制'
                                               WHEN 'Completed' THEN N'已完工'
                                               WHEN 'Scrapped' THEN N'已报废' ELSE s.Status END AS 状态说明,
                                 s.CreatedAt AS 创建时间
                          FROM ProductSN s
                          JOIN WorkOrder w ON s.WorkOrderId = w.WorkOrderId
                          WHERE w.WorkOrderNo = @no
                          ORDER BY s.SN", new SqlParameter("@no", workOrderNo.Trim()));
    }

    public DataTable GetRecentPassRecords(string? sn, int top = 10)
    {
        if (string.IsNullOrWhiteSpace(sn))
        {
            return Db.Query($@"SELECT TOP {top} p.SN, o.OperationName AS 工序, p.StepSeq AS 工序号,
                                      p.WorkstationCode AS 工位, p.OperatorName AS 操作人,
                                      p.Result AS 结果, p.PassedAt AS 时间
                               FROM PassRecord p JOIN Operation o ON p.OperationId = o.OperationId
                               ORDER BY p.PassRecordId DESC");
        }

        return Db.Query($@"SELECT TOP {top} p.SN, o.OperationName AS 工序, p.StepSeq AS 工序号,
                                  p.WorkstationCode AS 工位, p.OperatorName AS 操作人,
                                  p.Result AS 结果, p.PassedAt AS 时间
                           FROM PassRecord p JOIN Operation o ON p.OperationId = o.OperationId
                           WHERE p.SN = @sn
                           ORDER BY p.PassRecordId DESC", new SqlParameter("@sn", sn.Trim()));
    }

    public DataTable GetWorkOrderProgress()
    {
        return Db.Query(@"SELECT w.WorkOrderNo AS 工单号, m.MaterialName AS 物料, w.PlanQty AS 计划数,
                                 (SELECT COUNT(*) FROM ProductSN s
                                  WHERE s.WorkOrderId = w.WorkOrderId AND s.Status = 'Completed') AS 完工数,
                                 CAST(CASE WHEN w.PlanQty = 0 THEN 0
                                      ELSE (SELECT COUNT(*) * 100.0 FROM ProductSN s
                                            WHERE s.WorkOrderId = w.WorkOrderId AND s.Status = 'Completed') / w.PlanQty
                                 END AS DECIMAL(5,1)) AS 完工率,
                                 CASE w.Status WHEN 'Created' THEN N'已创建'
                                               WHEN 'Released' THEN N'已下达'
                                               WHEN 'InProgress' THEN N'生产中'
                                               WHEN 'Completed' THEN N'已完工'
                                               WHEN 'Closed' THEN N'已关闭' ELSE w.Status END AS 状态
                          FROM WorkOrder w JOIN Material m ON w.MaterialId = m.MaterialId
                          ORDER BY w.WorkOrderId DESC");
    }
}
