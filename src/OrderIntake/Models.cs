namespace OrderIntake;

public enum OrderStatus
{
    Accepted,
    Rejected
}

public sealed class OrderError
{
    public string Field { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}


public sealed class OrderResult
{
    public OrderStatus Status { get; set; }
    public Order? Order { get; set; }
    public List<OrderError> Errors { get; set; } = new();
}

public sealed class Order
{
    public string OrderId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string SpecimenId { get; set; } = string.Empty;
    public string SpecimenType { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateOnly CollectionDate { get; set; }
    public List<string> RequestedTests { get; set; } = new();
}
