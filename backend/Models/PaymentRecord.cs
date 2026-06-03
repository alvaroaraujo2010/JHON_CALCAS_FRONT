namespace ContaNexo.API.Models;

public class PaymentRecord
{
    public int Id { get; set; }
    public int PayrollId { get; set; }
    public Payroll? Payroll { get; set; }
    public string PeriodStart { get; set; } = string.Empty;
    public string PeriodEnd { get; set; } = string.Empty;
    public string PaymentDate { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = "transfer"; // transfer | cash | check
    public string? Reference { get; set; }
    public string Status { get; set; } = "completed"; // completed | pending
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
