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
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(p => p.Sku).IsUnique();
        modelBuilder.Entity<Account>().HasIndex(a => a.Code).IsUnique();
        modelBuilder.Entity<Purchase>().HasIndex(p => p.DocumentNumber).IsUnique();
        modelBuilder.Entity<Sale>().HasIndex(s => s.DocumentNumber).IsUnique();
        modelBuilder.Entity<JournalEntry>().HasIndex(j => j.EntryNumber).IsUnique();

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
    }
}
