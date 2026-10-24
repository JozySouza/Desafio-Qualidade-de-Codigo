# EcommerceRefatorado

API de e-commerce em ASP.NET Core/.NET 10, com persistência SQLite, documentação Swagger e uma CLI de apoio à squad.

O projeto foi desenvolvido como parte do desafio de Qualidade de Código da WoMakersCode, com o objetivo de refatorar uma API de e-commerce, aplicando princípios de Clean Code, SOLID, separação de responsabilidades, injeção de dependências e testes automatizados.

## Estado atual

- A API expõe operações CRUD para produtos.
- Possui endpoints para criação, listagem e consulta de pedidos.
- Disponibiliza operações de consulta e reabastecimento de estoque.
- O Swagger UI está habilitado em ambiente de desenvolvimento.
- `AppDbContext` configura as tabelas de produtos, pedidos e itens de pedido.
- A inicialização cria o esquema SQLite e insere quatro produtos de exemplo quando a tabela `Produtos` está vazia.
- Os repositórios assíncronos de produtos e pedidos usam Entity Framework Core e estão registrados na injeção de dependências.
- `StatusPedido` e `FormaPagamento` são persistidos como texto.
- O fluxo de pedidos utiliza services, repositories e DTOs.
- As integrações de pagamento e logística utilizam gateways mockados.
- O tratamento de exceções é centralizado no `ErrorHandlingMiddleware`.
- O projeto possui testes automatizados com xUnit e integração com GitHub Actions.
- A CLI contém comandos de apoio em diferentes níveis de implementação; consulte a seção [CLI da squad](#cli-da-squad).

As funcionalidades de reprocessamento de pagamentos e reenvio logístico estão previstas na Issue #4.

## Estrutura

```text
.
├── Api/
│   ├── Controllers/
│   └── Middlewares/
├── Application/
│   ├── DTOs/
│   ├── Interfaces/
│   └── Services/
├── Domain/
│   ├── Descontos/
│   ├── Entities/
│   ├── Enums/
│   └── Exceptions/
├── Infrastructure/
│   ├── Data/
│   ├── Gateways/
│   └── Repositories/
├── EcommerceRefatorado.Api/
│   └── Projeto ASP.NET Core e arquivo .csproj
├── EcommerceRefatorado.Tests/
│   ├── Api/
│   ├── Application/
│   ├── Domain/
│   ├── Fakes/
│   └── Infrastructure/
├── SquadCli/
│   └── CLI da squad
└── EcommerceRefatorado.slnx
```

As pastas `Api`, `Application`, `Domain` e `Infrastructure` ficam na raiz do repositório. O projeto web em `EcommerceRefatorado.Api/` as inclui como arquivos vinculados no `EcommerceRefatorado.csproj`; por isso, aparecem dentro do projeto no Solution Explorer sem serem movidas fisicamente.

A organização separa as responsabilidades da aplicação:

- **Api:** controllers e tratamento global de erros.
- **Application:** serviços, interfaces e DTOs.
- **Domain:** entidades, enums, exceções e regras de desconto.
- **Infrastructure:** persistência de dados, repositórios e gateways de integração.
- **EcommerceRefatorado.Tests:** testes automatizados do domínio, da aplicação e das integrações simuladas.

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
| `DELETE` | `/api/Produtos/{id}` | Desativa um produto; retorna `204` ou `404`. |

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

A exclusão de produtos utiliza desativação lógica, preservando o registro no banco de dados.

## Estoque

A API disponibiliza operações para consulta e reabastecimento de estoque.

| Método | Rota | Resultado |
| --- | --- | --- |
| `GET` | `/api/estoque` | Lista o estoque dos produtos ativos. |
| `POST` | `/api/estoque/reabastecer/{produtoId}/{quantidade}` | Reabastece o estoque de um produto. |

A operação de reabastecimento valida a quantidade informada, impedindo valores negativos ou iguais a zero.

As regras de movimentação de estoque também são encapsuladas na entidade `Produto`.

## Pedidos

A API possui operações para criação, listagem e consulta de pedidos.

| Método | Rota | Resultado |
| --- | --- | --- |
| `GET` | `/api/Pedidos` | Lista os pedidos cadastrados. |
| `GET` | `/api/Pedidos/{id}` | Consulta um pedido pelo identificador. |
| `POST` | `/api/Pedidos` | Cria um pedido com validação de estoque e processamento de pagamento. |

O fluxo de criação de pedidos utiliza o `PedidoService`, responsável por:

- Validar os produtos e as quantidades solicitadas.
- Verificar a disponibilidade de estoque.
- Calcular o valor total do pedido.
- Aplicar as estratégias de desconto.
- Processar o pagamento através do gateway.
- Persistir o pedido e a atualização de estoque.
- Solicitar o envio através da integração logística.

O processamento do pagamento ocorre antes da persistência do pedido, evitando a baixa definitiva de estoque quando o pagamento é recusado.

A persistência do pedido e a atualização de estoque são realizadas em uma transação.

Se a integração logística apresentar uma falha após a confirmação do pagamento, o pedido permanece registrado com status `Pago`, e o erro é registrado em log.

As operações de reprocessamento de pagamento e reenvio logístico estão previstas na Issue #4.

## Integrações

As integrações de pagamento e logística são implementadas através de interfaces, permitindo substituir os gateways sem acoplar os serviços às implementações concretas.

### Pagamento

O `PagamentoMockGateway` simula o processamento de pagamentos.

A implementação utiliza regras determinísticas, considerando o valor do pedido e a forma de pagamento informada, sem depender de resultados aleatórios.

### Logística

O `LogisticaMockGateway` simula o envio de pedidos e gera códigos de rastreamento.

As operações utilizam métodos assíncronos e oferecem suporte a `CancellationToken`.

Os gateways estão registrados no sistema de injeção de dependências da aplicação.

## Banco de dados

A connection string `Default` está em `EcommerceRefatorado.Api/appsettings.json` e aponta para `ecommerce.db`. O banco é criado na inicialização da API; com a configuração atual, o arquivo fica no diretório de conteúdo do projeto web (`EcommerceRefatorado.Api/`).

O seeder insere quatro produtos (`Camiseta`, `Tenis`, `Bone` e `Mochila`) somente quando a tabela `Produtos` está vazia. Se já houver produtos, os dados existentes são mantidos e o seeder não substitui nem completa o catálogo.

O seeder também adiciona as colunas `Descricao` e `Ativo` em `Produtos`, além de `FormaPagamento` em `Pedidos`, quando elas ainda não existem, preservando os registros existentes.

O projeto usa `EnsureCreated`; migrations do EF Core ainda não estão configuradas.

## Repositórios e domínio

`IProdutoRepository` e `IPedidoRepository` definem operações assíncronas de consulta, inclusão, atualização e exclusão. As implementações `ProdutoRepository` e `PedidoRepository` ficam em `Infrastructure/Repositories`. A consulta de pedidos inclui os itens e os produtos relacionados.

Os enums persistidos como texto são:

- `StatusPedido`: `Pendente`, `Pago`, `Enviado` e `Cancelado`.
- `FormaPagamento`: `NaoInformado`, `Pix`, `CartaoCredito`, `CartaoDebito` e `Boleto`.

As regras de desconto são organizadas através da interface `IRegraDesconto`, com implementações específicas para desconto por valor e cliente VIP.

A `CalculadoraDesconto` utiliza essas estratégias para calcular o valor final do pedido.

## Tratamento de erros

A aplicação utiliza o `ErrorHandlingMiddleware` para centralizar o tratamento de exceções durante as requisições HTTP.

Os erros são registrados através de `ILogger`, e as respostas são padronizadas em JSON.

| Exceção | Status HTTP |
| --- | --- |
| `ArgumentException` | 400 Bad Request |
| `DomainException` | 400 Bad Request |
| `KeyNotFoundException` | 404 Not Found |
| Exceções inesperadas | 500 Internal Server Error |

Para erros internos, a API retorna uma mensagem genérica, evitando expor detalhes técnicos ao cliente.

O `PedidosController` encaminha as exceções de negócio ao middleware global, evitando a duplicação do tratamento de erros.

## Testes automatizados

O projeto utiliza xUnit para verificar regras de negócio, comportamentos do domínio e integrações simuladas.

A suíte contempla testes para:

- Reabastecimento e baixa de estoque.
- Validação de quantidades inválidas.
- Estoque insuficiente.
- Desativação lógica de produtos.
- Regras de desconto por valor e cliente VIP.
- Pagamentos aprovados e recusados.
- Geração de códigos de rastreamento.
- Cancelamento de operações logísticas.
- Falhas de logística após pagamento aprovado.
- Falhas simuladas na atualização de pedidos.
- Tratamento de exceções HTTP 400, 404 e 500.

Para executar os testes na raiz do repositório:

```powershell
dotnet test .\EcommerceRefatorado.slnx
```

Os testes unitários utilizam implementações simuladas para validar o comportamento dos serviços de forma isolada.

A cobertura poderá ser ampliada com testes de integração utilizando o banco SQLite e os endpoints HTTP.

## CI/CD

O projeto possui um workflow de CI/CD configurado com GitHub Actions em `.github/workflows/ci-cd.yml`.

O pipeline executa:

- Restauração das dependências.
- Compilação da solução no .NET 10.
- Execução dos testes automatizados.
- Coleta e disponibilização dos resultados dos testes.
- Geração de um pacote publicável da API na branch `main`, após aprovação do CI.

O pacote gerado é disponibilizado como artefato do GitHub Actions. O workflow não realiza implantação automática em um servidor externo.

## CLI da squad

Execute a partir da raiz:

```powershell
dotnet run --project .\SquadCli\SquadCli.csproj -- --help
```

Comandos atuais:

- `setup`: exibe mensagens de configuração; não restaura dependências nem configura o banco.
- `tasks [--me]`: exibe uma tabela de tarefas fictícias; `--me` ainda não filtra a lista.
- `start <id>`: tenta criar a branch `feature/issue-<id>` usando Git no diretório atual.
- `test`: executa `dotnet test --filter Category=Unit`, utilizando os testes unitários implementados pela squad.

## Planejamento registrado

O script `criar_issues.sh` contém comandos `gh issue create` para registrar etapas de planejamento. Ele depende do GitHub CLI (`gh`) autenticado.
