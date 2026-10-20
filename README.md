# EcommerceRefatorado

API de e-commerce em ASP.NET Core/.NET 10, com persistência SQLite, documentação Swagger e uma CLI de apoio à squad.

## Estado atual

- A API expõe operações CRUD para produtos.
- O Swagger UI está habilitado em ambiente de desenvolvimento.
- `AppDbContext` configura as tabelas de produtos, pedidos e itens de pedido.
- A inicialização cria o esquema SQLite e insere quatro produtos de exemplo quando a tabela `Produtos` está vazia.
- Os repositórios assíncronos de produtos e pedidos usam Entity Framework Core e estão registrados na injeção de dependências.
- `StatusPedido` e `FormaPagamento` são persistidos como texto.
- Os repositórios de pedidos estão implementados, mas ainda não há endpoints de pedidos na API.
- A CLI contém comandos de apoio em diferentes níveis de implementação; consulte a seção [CLI da squad](#cli-da-squad).

## Estrutura

```text
.
├── Api/
│   └── Controllers/
├── Application/
│   ├── DTOs/
│   └── Interfaces/
├── Domain/
│   ├── Entities/
│   └── Enums/
├── Infrastructure/
│   ├── Data/
│   └── Repositories/
├── EcommerceRefatorado.Api/
│   └── Projeto ASP.NET Core e arquivo .csproj
├── SquadCli/
│   └── CLI da squad
└── EcommerceRefatorado.slnx
```

As pastas `Api`, `Application`, `Domain` e `Infrastructure` ficam na raiz do repositório. O projeto web em `EcommerceRefatorado.Api/` as inclui como arquivos vinculados no `EcommerceRefatorado.csproj`; por isso, aparecem dentro do projeto no Solution Explorer sem serem movidas fisicamente.

## Requisitos

- .NET SDK 10
- Git, para usar o comando `start` da CLI

## Compilar e executar

Na raiz do repositório:

```powershell
dotnet restore .\EcommerceRefatorado.slnx
dotnet build .\EcommerceRefatorado.slnx
dotnet run --project .\EcommerceRefatorado.Api\EcommerceRefatorado.csproj
```

No perfil HTTP, a API usa `http://localhost:5259`. Em ambiente de desenvolvimento:

- Swagger UI: `http://localhost:5259/swagger`
- Documento OpenAPI: `http://localhost:5259/swagger/v1/swagger.json`

Se a porta 5259 já estiver ocupada, informe outra ao iniciar a API:

```powershell
dotnet run --project .\EcommerceRefatorado.Api\EcommerceRefatorado.csproj --urls http://localhost:5261
```

## Produtos

As rotas disponíveis são:

| Método | Rota | Resultado |
| --- | --- | --- |
| `GET` | `/api/Produtos` | Lista os produtos. |
| `GET` | `/api/Produtos/{id}` | Retorna um produto ou `404`. |
| `POST` | `/api/Produtos` | Cria um produto e retorna `201`. |
| `PUT` | `/api/Produtos/{id}` | Atualiza um produto; retorna `204` ou `404`. |
| `DELETE` | `/api/Produtos/{id}` | Exclui um produto; retorna `204` ou `404`. |

Corpo esperado por `POST` e `PUT`:

```json
{
  "nome": "Camiseta",
  "descricao": "Camiseta basica",
  "preco": 49.90,
  "estoque": 100
}
```

Os campos são validados: nome obrigatório (até 150 caracteres), descrição obrigatória (até 500 caracteres), preço maior que zero e estoque não negativo.

## Banco de dados

A connection string `Default` está em `EcommerceRefatorado.Api/appsettings.json` e aponta para `ecommerce.db`. O banco é criado na inicialização da API; com a configuração atual, o arquivo fica no diretório de conteúdo do projeto web (`EcommerceRefatorado.Api/`).

O seeder insere quatro produtos (`Camiseta`, `Tenis`, `Bone` e `Mochila`) somente quando a tabela `Produtos` está vazia. Se já houver produtos, os dados existentes são mantidos e o seeder não substitui nem completa o catálogo.

O seeder também adiciona as colunas `Descricao` em `Produtos` e `FormaPagamento` em `Pedidos` quando elas ainda não existem, preservando os registros existentes. O projeto usa `EnsureCreated`; migrations do EF Core ainda não estão configuradas.

## Repositórios e domínio

`IProdutoRepository` e `IPedidoRepository` definem operações assíncronas de consulta, inclusão, atualização e exclusão. As implementações `ProdutoRepository` e `PedidoRepository` ficam em `Infrastructure/Repositories`. A consulta de pedidos inclui os itens e os produtos relacionados.

Os enums persistidos como texto são:

- `StatusPedido`: `Pendente`, `Pago`, `Enviado` e `Cancelado`.
- `FormaPagamento`: `NaoInformado`, `Pix`, `CartaoCredito`, `CartaoDebito` e `Boleto`.

## CLI da squad

Execute a partir da raiz:

```powershell
dotnet run --project .\SquadCli\SquadCli.csproj -- --help
```

Comandos atuais:

- `setup`: exibe mensagens de configuração; não restaura dependências nem configura o banco.
- `tasks [--me]`: exibe uma tabela de tarefas fictícias; `--me` ainda não filtra a lista.
- `start <id>`: tenta criar a branch `feature/issue-<id>` usando Git no diretório atual.
- `test`: executa `dotnet test --filter Category=Unit`; não há testes automatizados implementados atualmente.

## Planejamento registrado

O script `criar_issues.sh` contém comandos `gh issue create` para registrar etapas de planejamento. Ele depende do GitHub CLI (`gh`) autenticado.
