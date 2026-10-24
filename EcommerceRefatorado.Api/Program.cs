
using EcommerceRefatorado;
using EcommerceRefatorado.Interfaces;
using EcommerceRefatorado.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuração dos controllers e Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuração do banco de dados
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Repositórios
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();

// Serviços
builder.Services.AddScoped<IEstoqueService, EstoqueService>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<IPedidoService, PedidoService>();

// Integrações de pagamento e logística
builder.Services.AddScoped<IPagamentoGateway, PagamentoMockGateway>();
builder.Services.AddScoped<ILogisticaGateway, LogisticaMockGateway>();

// Estratégias de desconto
builder.Services.AddSingleton<IRegraDesconto, DescontoValorAlto>();
builder.Services.AddSingleton<IRegraDesconto, DescontoVip>();
builder.Services.AddScoped<CalculadoraDesconto>();

var app = builder.Build();

// Middleware global de tratamento de erros
app.UseMiddleware<ErrorHandlingMiddleware>();

// Inicialização do banco de dados
await DbSeeder.SeedAsync(app.Services);

// Swagger disponível em desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Mapeamento dos controllers
app.MapControllers();

app.Run();
