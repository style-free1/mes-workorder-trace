using MesDemo.Data;
using MesDemo.Services;

namespace MesDemo.Services;

/// <summary>
/// 无界面自测：跑一遍“建单 → 下达 → 开工 → 报工 → 卡控 → 追溯 → 完工”，结果写入 selftest.log。
/// 用法：MesDemo.exe --selftest
/// </summary>
public static class SelfTest
{
    public static int Run()
    {
        var log = new List<string>();
        int pass = 0, fail = 0;

        void Check(string name, bool ok, string detail = "")
        {
            if (ok) { pass++; log.Add($"[PASS] {name}"); }
            else { fail++; log.Add($"[FAIL] {name}　{detail}"); }
        }

        log.Add("MES Demo 自测　" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        log.Add("连接：" + Db.ConnectionString);
        log.Add("");

        var no = "WO-TEST-" + DateTime.Now.ToString("HHmmss");
        int woId = 0;

        try
        {
            var svc = new MesService();

            woId = svc.CreateWorkOrder(no, 1, 2);
            Check("创建工单", woId > 0);

            try
            {
                svc.CreateSnBatch(woId, 1);
                Check("未下达不能创建 SN", false, "本应抛出异常");
            }
            catch
            {
                Check("未下达不能创建 SN", true);
            }

            svc.ReleaseWorkOrder(woId);
            svc.StartWorkOrder(woId);
            Check("下达并开工", true);

            var n = svc.CreateSnBatch(woId, 2);
            Check("批量生成 2 个 SN", n == 2);

            var sn1 = no + "-0001";
            var sn2 = no + "-0002";

            var r = svc.PassStation(sn1, 3, "WS-30", "selftest", true);
            Check("跳站被拒绝（当前应过 OP20，扫 OP30）", !r.Success && r.Message.Contains("跳站"), r.Message);

            r = svc.PassStation(sn1, 1, "WS-10", "selftest", true);
            Check("OP10 机加过站成功", r.Success, r.Message);

            r = svc.PassStation(sn1, 1, "WS-10", "selftest", true);
            Check("重复过站被拒绝", !r.Success && r.Message.Contains("重复"), r.Message);

            r = svc.PassStation(sn1, 2, "WS-20", "selftest", false);
            Check("NG 记录但不推进", !r.Success && r.Message.Contains("NG"), r.Message);

            r = svc.PassStation(sn1, 5, "WS-50", "selftest", true);
            Check("NG 后仍不能跳站（应过 OP20）", !r.Success && r.Message.Contains("跳站"), r.Message);

            r = svc.PassStation(sn1, 2, "WS-20", "selftest", true);
            Check("OP20 旋铆过站成功", r.Success, r.Message);

            foreach (var (op, ws) in new[] { (3, "WS-30"), (4, "WS-40"), (5, "WS-50") })
            {
                r = svc.PassStation(sn1, op, ws, "selftest", true);
                Check($"SN1 工序 {op} 过站", r.Success, r.Message);
            }

            var info = svc.GetSn(sn1);
            Check("SN1 状态为已完工", info?.Status == "Completed", info?.Status ?? "null");

            var records = svc.TraceBySn(sn1);
            var okCount = records.Count(x => x.Result == "OK");
            Check("追溯记录含 5 条 OK", okCount == 5, "实际 " + okCount);

            foreach (var (op, ws) in new[] { (1, "WS-10"), (2, "WS-20"), (3, "WS-30"), (4, "WS-40"), (5, "WS-50") })
            {
                svc.PassStation(sn2, op, ws, "selftest", true);
            }

            var wo = svc.GetWorkOrders().First(w => w.WorkOrderId == woId);
            Check("工单自动完工", wo.Status == "Completed", wo.Status);

            // UI 冒烟：五个界面都能正常构造
            try
            {
                using var f1 = new Forms.WorkOrderForm();
                using var f2 = new Forms.ReportWorkForm();
                using var f3 = new Forms.TraceForm();
                using var f4 = new Forms.StatForm();
                using var f5 = new Forms.BaseDataForm();
                Check("五个 WinForm 界面可正常创建", true);
            }
            catch (Exception ex)
            {
                Check("五个 WinForm 界面可正常创建", false, ex.Message);
            }
        }
        catch (Exception ex)
        {
            fail++;
            log.Add("[FAIL] 未预期异常：" + ex);
        }
        finally
        {
            try
            {
                if (woId > 0)
                {
                    using var conn = Db.Open();
                    using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
                        DELETE FROM PassRecord WHERE WorkOrderId = @id;
                        DELETE FROM ProductSN WHERE WorkOrderId = @id;
                        DELETE FROM WorkOrderStep WHERE WorkOrderId = @id;
                        DELETE FROM WorkOrder WHERE WorkOrderId = @id;", conn);
                    cmd.Parameters.AddWithValue("@id", woId);
                    cmd.ExecuteNonQuery();
                    log.Add("已清理自测数据：" + no);
                }
            }
            catch (Exception ex)
            {
                log.Add("清理自测数据失败：" + ex.Message);
            }
        }

        log.Add("");
        log.Add($"结果：通过 {pass}，失败 {fail}");

        var path = Path.Combine(AppContext.BaseDirectory, "selftest.log");
        File.WriteAllLines(path, log);

        return fail == 0 ? 0 : 1;
    }
}
