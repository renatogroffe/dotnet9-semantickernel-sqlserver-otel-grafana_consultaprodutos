using Microsoft.EntityFrameworkCore;

namespace ConsoleAppChatAIProdutos.Data;

public class CatalogoContext : DbContext
{
    public DbSet<Produto>? Produtos { get; set; }
    public static string? ConnectionString { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(ConnectionString)
            .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Produto>().HasKey(p => p.Id);
        modelBuilder.Entity<Produto>().Property(p => p.Preco).HasPrecision(19, 4);
    }
}