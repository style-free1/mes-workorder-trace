using Microsoft.Data.SqlClient;
using System.Data;

namespace MesDemo.Data;

public static class Db
{
    /// <summary>
    /// 默认连接本机 SQL Server Express 实例。换机器时改这里，或设置环境变量 MES_CONN。
    /// </summary>
    public static string ConnectionString =
        Environment.GetEnvironmentVariable("MES_CONN")
        ?? @"Server=(localdb)\MSSQLLocalDB;Database=MesDemo;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False";

    public static SqlConnection Open()
    {
        var conn = new SqlConnection(ConnectionString);
        conn.Open();
        return conn;
    }

    public static DataTable Query(string sql, params SqlParameter[] ps)
    {
        using var conn = Open();
        using var cmd = new SqlCommand(sql, conn);
        if (ps.Length > 0) cmd.Parameters.AddRange(ps);
        using var da = new SqlDataAdapter(cmd);
        var dt = new DataTable();
        da.Fill(dt);
        return dt;
    }

    public static object? Scalar(string sql, params SqlParameter[] ps)
    {
        using var conn = Open();
        using var cmd = new SqlCommand(sql, conn);
        if (ps.Length > 0) cmd.Parameters.AddRange(ps);
        return cmd.ExecuteScalar();
    }

    public static bool TestConnection(out string message)
    {
        try
        {
            using var conn = Open();
            message = "数据库连接正常";
            return true;
        }
        catch (Exception ex)
        {
            message = "数据库连接失败：" + ex.Message;
            return false;
        }
    }
}
