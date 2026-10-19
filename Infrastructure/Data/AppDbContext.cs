using Microsoft.EntityFrameworkCore;

namespace EcommerceRefatorado;

public class AppDbContext : DbContext
{
    public DbSet<Produto> Produtos { get; set; }
    public DbSet<Pedido> Pedidos { get; set; }
    public DbSet<ItemPedido> ItensPedido { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // connection string fixa no codigo, ignorando appsettings.json e injecao de dependencia
        optionsBuilder.UseSqlite("Data Source=ecommerce.db");
    }
}