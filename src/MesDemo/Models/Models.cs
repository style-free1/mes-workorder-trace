namespace MesDemo.Models;

public class Material
{
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string? Spec { get; set; }
}

public class Operation
{
    public int OperationId { get; set; }
    public string OperationCode { get; set; } = "";
    public string OperationName { get; set; } = "";
    public int SeqNo { get; set; }
}

public class Workstation
{
    public int WorkstationId { get; set; }
    public string WorkstationCode { get; set; } = "";
    public string WorkstationName { get; set; } = "";
    public int? OperationId { get; set; }
    public string? OperationName { get; set; }
}

public class WorkOrder
{
    public int WorkOrderId { get; set; }
    public string WorkOrderNo { get; set; } = "";
    public int MaterialId { get; set; }
    public string MaterialName { get; set; } = "";
    public int RoutingId { get; set; }
    public int PlanQty { get; set; }
    public string Status { get; set; } = "Created";
    public DateTime CreatedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string StatusText => Status switch
    {
        "Created" => "已创建",
        "Released" => "已下达",
        "InProgress" => "生产中",
        "Completed" => "已完工",
        "Closed" => "已关闭",
        _ => Status
    };
}

public class WorkOrderStep
{
    public int WorkOrderStepId { get; set; }
    public int WorkOrderId { get; set; }
    public int OperationId { get; set; }
    public string OperationName { get; set; } = "";
    public int StepSeq { get; set; }
    public int PlanQty { get; set; }
    public int DoneQty { get; set; }
    public string Status { get; set; } = "Pending";
}

public class ProductSn
{
    public long SnId { get; set; }
    public string SN { get; set; } = "";
    public int WorkOrderId { get; set; }
    public int CurrentStepSeq { get; set; }
    public string Status { get; set; } = "InProcess";
    public DateTime CreatedAt { get; set; }

    public string StatusText => Status switch
    {
        "InProcess" => "在制",
        "Completed" => "已完工",
        "Scrapped" => "已报废",
        _ => Status
    };
}

public class PassRecord
{
    public long PassRecordId { get; set; }
    public string SN { get; set; } = "";
    public int StepSeq { get; set; }
    public string OperationName { get; set; } = "";
    public string? WorkstationCode { get; set; }
    public string? OperatorName { get; set; }
    public string Result { get; set; } = "OK";
    public DateTime PassedAt { get; set; }
}

public record PassResult(bool Success, string Message, string NextOperation = "");

public record SnNextInfo(string SN, string WorkOrderNo, string MaterialName,
    int CurrentStepSeq, string NextOperationName, int NextStepSeq, int NextOperationId, string SnStatus);

