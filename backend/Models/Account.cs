namespace ContaNexo.API.Models;

public class Account
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public int? ParentId { get; set; }
    public Account? Parent { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Account> Children { get; set; } = new List<Account>();
    public ICollection<JournalEntryLine> JournalLines { get; set; } = new List<JournalEntryLine>();
}

public enum AccountType
{
    Activo,
    Pasivo,
    Patrimonio,
    Ingreso,
    Gasto
}
