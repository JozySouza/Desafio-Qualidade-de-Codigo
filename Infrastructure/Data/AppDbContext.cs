using Microsoft.EntityFrameworkCore;

namespace EcommerceRefatorado;

// Infrastructure/Data/AppDbContext.cs
public class AppDbContext : DbContext
{
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.Property(p => p.Status)
                .HasConversion<string>();
            entity.Property(p => p.FormaPagamento)
                .HasConversion<string>()
                .HasDefaultValue(FormaPagamento.NaoInformado);
        });
    }
}