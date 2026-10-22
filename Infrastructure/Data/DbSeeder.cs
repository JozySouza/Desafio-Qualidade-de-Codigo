using Microsoft.EntityFrameworkCore;

namespace EcommerceRefatorado;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();
        await EnsureDescricaoColumnAsync(db);
        await EnsureFormaPagamentoColumnAsync(db);
        await EnsureAtivoColumnAsync(db);

        if (await db.Produtos.AnyAsync())
        {
            return;
        }

        db.Produtos.AddRange(
            new Produto
            {
                Nome = "Camiseta",
                Descricao = "Camiseta basica",
                Preco = 49.90m,
                Estoque = 100
            },
            new Produto
            {
                Nome = "Tenis",
                Descricao = "Tenis esportivo",
                Preco = 299.90m,
                Estoque = 50
            },
            new Produto
            {
                Nome = "Bone",
                Descricao = "Bone ajustavel",
                Preco = 39.90m,
                Estoque = 200
            },
            new Produto
            {
                Nome = "Mochila",
                Descricao = "Mochila para notebook",
                Preco = 159.90m,
                Estoque = 30
            });

        await db.SaveChangesAsync();
    }

    private static async Task EnsureDescricaoColumnAsync(AppDbContext db)
    {
        if (!await HasColumnAsync(db, "Produtos", nameof(Produto.Descricao)))
        {
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "Produtos" ADD COLUMN "Descricao" TEXT NOT NULL DEFAULT '';""");
        }
    }

    private static async Task EnsureFormaPagamentoColumnAsync(AppDbContext db)
    {
        if (!await HasColumnAsync(db, "Pedidos", nameof(Pedido.FormaPagamento)))
        {
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "Pedidos" ADD COLUMN "FormaPagamento" TEXT NOT NULL DEFAULT 'NaoInformado';""");
        }
    }

    private static async Task<bool> HasColumnAsync(AppDbContext db, string tableName, string columnName)
    {
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info('{tableName}');";
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                if (reader.GetString(1) == columnName)
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static async Task EnsureAtivoColumnAsync(AppDbContext db)
    {
        if (!await HasColumnAsync(db, "Produtos", nameof(Produto.Ativo)))
        {
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "Produtos" ADD COLUMN "Ativo" INTEGER NOT NULL DEFAULT 1;""");
        }
    }
}