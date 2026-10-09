using Microsoft.EntityFrameworkCore;
using LibraryRfidPoC.Models.Entities;

namespace LibraryRfidPoC.Data;

/// <summary>
/// DbContext quản lý kết nối CSDL và các tập thực thể thư viện.
/// </summary>
public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Reader> Readers => Set<Reader>();
    public DbSet<Librarian> Librarians => Set<Librarian>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookItem> BookItems => Set<BookItem>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<Fine> Fines => Set<Fine>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cấu hình kế thừa TPT (Table-Per-Type): User -> Reader, Librarian
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.UserID);
        });

        modelBuilder.Entity<Reader>(entity =>
        {
            entity.ToTable("Readers");
            entity.Property(r => r.TotalFines).HasColumnType("decimal(18,0)");
        });

        modelBuilder.Entity<Librarian>(entity =>
        {
            entity.ToTable("Librarians");
        });

        // Cấu hình các entity còn lại
        modelBuilder.Entity<Book>(entity =>
        {
            entity.ToTable("Books");
            entity.HasKey(b => b.Isbn);
        });

        modelBuilder.Entity<BookItem>(entity =>
        {
            entity.ToTable("BookItems");
            entity.HasKey(bi => bi.BookID);
        });

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.ToTable("Loans");
            entity.HasKey(l => l.LoanID);
        });

        modelBuilder.Entity<Fine>(entity =>
        {
            entity.ToTable("Fines");
            entity.HasKey(f => f.FineID);
            entity.Property(f => f.Amount).HasColumnType("decimal(18,0)");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.ToTable("Reservations");
            entity.HasKey(r => r.ReservationID);
        });
    }
}

