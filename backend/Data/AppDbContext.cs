using ContaNexo.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseDetail> PurchaseDetails => Set<PurchaseDetail>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<InventoryLot> InventoryLots => Set<InventoryLot>();

    // RBAC
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // E-commerce
    public DbSet<CatalogProduct> CatalogProducts => Set<CatalogProduct>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<PaymentSettings> PaymentSettings => Set<PaymentSettings>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();

    // Módulo Nómina
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<PayrollDeduction> PayrollDeductions => Set<PayrollDeduction>();
    public DbSet<Payroll> Payrolls => Set<Payroll>();
    public DbSet<PayrollDetail> PayrollDetails => Set<PayrollDetail>();
    public DbSet<PayrollDeductionLine> PayrollDeductionLines => Set<PayrollDeductionLine>();
    public DbSet<SocialSecurityPayment> SocialSecurityPayments => Set<SocialSecurityPayment>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();

    // Suscripción / bloqueo admin
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    // Módulo Nómina — Cumplimiento legal Colombia
    public DbSet<LegalParameter> LegalParameters => Set<LegalParameter>();
    public DbSet<WithholdingTaxBracket> WithholdingTaxBrackets => Set<WithholdingTaxBracket>();
    public DbSet<PayrollProvision> PayrollProvisions => Set<PayrollProvision>();
    public DbSet<PayrollSettlement> PayrollSettlements => Set<PayrollSettlement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(p => p.Sku).IsUnique();
        modelBuilder.Entity<Account>().HasIndex(a => a.Code).IsUnique();
        modelBuilder.Entity<Purchase>().HasIndex(p => p.DocumentNumber).IsUnique();
        modelBuilder.Entity<Sale>().HasIndex(s => s.DocumentNumber).IsUnique();
        modelBuilder.Entity<JournalEntry>().HasIndex(j => j.EntryNumber).IsUnique();

        // Índices únicos de nómina
        modelBuilder.Entity<LegalParameter>().HasIndex(l => l.Year).IsUnique();
        modelBuilder.Entity<PayrollProvision>().HasIndex(p => new { p.EmployeeId, p.Year, p.Month }).IsUnique();

        modelBuilder.Entity<PurchaseDetail>()
            .HasOne(d => d.Purchase).WithMany(p => p.Details).HasForeignKey(d => d.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SaleDetail>()
            .HasOne(d => d.Sale).WithMany(s => s.Details).HasForeignKey(d => d.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JournalEntryLine>()
            .HasOne(l => l.JournalEntry).WithMany(j => j.Lines).HasForeignKey(l => l.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Account>()
            .HasOne(a => a.Parent).WithMany(a => a.Children).HasForeignKey(a => a.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // RBAC: PKs y unique constraints
        modelBuilder.Entity<Permission>().HasKey(p => p.Key);
        modelBuilder.Entity<RolePermission>()
            .HasIndex(r => new { r.Role, r.PermissionKey }).IsUnique();

        // Relaciones Nómina
        modelBuilder.Entity<PayrollDetail>()
            .HasOne(d => d.Payroll).WithMany(p => p.Details).HasForeignKey(d => d.PayrollId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PayrollDeductionLine>()
            .HasOne(l => l.PayrollDetail).WithMany(d => d.Deductions).HasForeignKey(l => l.PayrollDetailId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PaymentRecord>()
            .HasOne(r => r.Payroll).WithMany().HasForeignKey(r => r.PayrollId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PayrollProvision>()
            .HasOne(p => p.Payroll).WithMany().HasForeignKey(p => p.PayrollId)
            .OnDelete(DeleteBehavior.SetNull);

        // E-commerce
        modelBuilder.Entity<CatalogProduct>().HasIndex(p => p.Slug);
        modelBuilder.Entity<CatalogProduct>()
            .HasOne(p => p.InternalProduct)
            .WithMany()
            .HasForeignKey(p => p.InternalProductId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Order>().HasIndex(o => o.OrderNumber).IsUnique();
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Sale)
            .WithMany()
            .HasForeignKey(o => o.SaleId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<PaymentSettings>().HasIndex(p => p.Provider).IsUnique();
        modelBuilder.Entity<OrderItem>()
            .HasOne(i => i.Order).WithMany(o => o.Items).HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PayrollSettlement>()
            .HasOne(s => s.Employee).WithMany().HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Subscription>().ToTable("subscription");
    }
}
