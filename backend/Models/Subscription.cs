namespace ContaNexo.API.Models;

public class Subscription
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
    public decimal MonthlyFee { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime UpdatedAt { get; set; }
}
