namespace ContaNexo.API.DTOs;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string FullName, string Email, string Role, DateTime ExpiresAt);

public record UserDto(int Id, string FullName, string Email, string Role, bool IsActive);
public record CreateUserRequest(string FullName, string Email, string Password, string Role);
public record UpdateUserRequest(string FullName, string Email, string Role, bool IsActive, string? Password);

public record CompanyDto(int Id, string BusinessName, string Tagline, string? Description, string? Address,
    string? Phone, string? Email, string? Website, string? LogoUrl, string? TaxId, string Currency);
public record UpdateCompanyRequest(string BusinessName, string Tagline, string? Description, string? Address,
    string? Phone, string? Email, string? Website, string? LogoUrl, string? TaxId, string Currency);

public record CategoryDto(int Id, string Name, string? Description, bool IsActive, int ProductCount);
public record CategoryRequest(string Name, string? Description, bool IsActive);

public record ProductDto(int Id, string Sku, string Name, string? Description, int CategoryId, string CategoryName,
    decimal UnitCost, decimal UnitPrice, int Stock, int MinStock, string Unit, bool IsActive, bool LowStock);
public record ProductRequest(string Sku, string Name, string? Description, int CategoryId,
    decimal UnitCost, decimal UnitPrice, int MinStock, string Unit, bool IsActive);

public record SupplierDto(int Id, string Name, string? TaxId, string? ContactName, string? Phone, string? Email, string? Address, bool IsActive);
public record SupplierRequest(string Name, string? TaxId, string? ContactName, string? Phone, string? Email, string? Address, bool IsActive);

public record CustomerDto(int Id, string Name, string? TaxId, string? ContactName, string? Phone, string? Email, string? Address, bool IsActive);
public record CustomerRequest(string Name, string? TaxId, string? ContactName, string? Phone, string? Email, string? Address, bool IsActive);

public record PurchaseDetailDto(int ProductId, string ProductName, int Quantity, decimal UnitCost, decimal LineTotal);
public record PurchaseDto(int Id, string DocumentNumber, int SupplierId, string SupplierName, DateTime PurchaseDate,
    decimal Subtotal, decimal Tax, decimal Total, string Status, string? Notes, List<PurchaseDetailDto> Details);
public record CreatePurchaseRequest(int SupplierId, DateTime? PurchaseDate, decimal TaxRate, string? Notes,
    List<CreatePurchaseDetailRequest> Details);
public record CreatePurchaseDetailRequest(int ProductId, int Quantity, decimal UnitCost);

public record SaleDetailDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);
public record SaleDto(int Id, string DocumentNumber, int? CustomerId, string? CustomerName, DateTime SaleDate,
    decimal Subtotal, decimal Tax, decimal Total, string PaymentMethod, string Status, string? Notes,
    string ElectronicInvoiceStatus, string? ElectronicInvoiceNumber, string? Cufe, DateTime? ElectronicInvoiceIssuedAt,
    List<SaleDetailDto> Details);

public record ElectronicInvoiceLineDto(string Description, int Quantity, decimal UnitPrice, decimal LineTotal);
public record ElectronicInvoiceDto(
    int SaleId, string DocumentNumber, string? ElectronicInvoiceNumber, string ElectronicInvoiceStatus, string? Cufe,
    DateTime? IssuedAt, string IssuerName, string? IssuerTaxId, string? IssuerAddress, string? IssuerPhone, string? IssuerEmail,
    string CustomerName, string? CustomerTaxId, DateTime SaleDate, string PaymentMethod,
    decimal Subtotal, decimal Tax, decimal TaxRate, decimal Total, List<ElectronicInvoiceLineDto> Lines);
public record CreateSaleRequest(int? CustomerId, DateTime? SaleDate, decimal TaxRate, string PaymentMethod, string? Notes,
    List<CreateSaleDetailRequest> Details);
public record CreateSaleDetailRequest(int ProductId, int Quantity, decimal UnitPrice);

public record InventoryMovementDto(int Id, int ProductId, string ProductName, string Type, int Quantity,
    int StockBefore, int StockAfter, string? Reference, string? Notes, DateTime CreatedAt);
public record AdjustInventoryRequest(int ProductId, int Quantity, string Type, string? Notes);

public record AccountDto(int Id, string Code, string Name, string Type, int? ParentId, bool IsActive);
public record AccountRequest(string Code, string Name, string Type, int? ParentId, bool IsActive);

public record JournalLineDto(int AccountId, string AccountCode, string AccountName, decimal Debit, decimal Credit, string? Description);
public record JournalEntryDto(int Id, string EntryNumber, DateTime EntryDate, string Description, string? Reference,
    string Status, List<JournalLineDto> Lines, decimal TotalDebit, decimal TotalCredit);
public record CreateJournalEntryRequest(DateTime? EntryDate, string Description, string? Reference,
    List<CreateJournalLineRequest> Lines);
public record CreateJournalLineRequest(int AccountId, decimal Debit, decimal Credit, string? Description);

public record DashboardDto(
    decimal TotalSalesMonth, decimal TotalPurchasesMonth, int ProductsCount, int LowStockCount,
    int CustomersCount, int SuppliersCount, List<RecentSaleDto> RecentSales, List<LowStockProductDto> LowStockProducts);
public record RecentSaleDto(int Id, string DocumentNumber, string? CustomerName, decimal Total, DateTime SaleDate);
public record LowStockProductDto(int Id, string Sku, string Name, int Stock, int MinStock);

public record TrialBalanceLineDto(string Code, string Name, string Type, decimal Debit, decimal Credit, decimal Balance);
public record TrialBalanceDto(List<TrialBalanceLineDto> Lines, decimal TotalDebit, decimal TotalCredit);
