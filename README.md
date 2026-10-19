# EcommerceRefatorado

Projeto em andamento para refatorar uma aplicação de e-commerce em .NET, com uma API e uma CLI de apoio à squad. Até aqui, foram criados a solução, os projetos iniciais e a estrutura de pastas; os fluxos de negócio ainda não estão implementados nem conectados à API.

## Estado atual

| Área | O que existe hoje |
| --- | --- |
| API | Projeto ASP.NET Core em .NET 10, ainda com o endpoint de exemplo `/weatherforecast` do template. |
| Estrutura da aplicação | Pastas para API, aplicação, domínio e infraestrutura, com arquivos planejados para controllers, serviços, DTOs, interfaces, entidades, gateways e repositórios. A maior parte desses arquivos está vazia. |
| Persistência | Há um arquivo inicial `AppDbContext.cs` com configuração SQLite fixa, mas ele está fora da pasta do projeto web e não está integrado à inicialização da API. |
| CLI | Projeto de console `SquadCli` com comandos `setup`, `tasks`, `start` e `test`. |
| Testes | Não há projeto ou arquivos de teste no repositório neste momento. |

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
├── EcommerceRefatorado/
│   └── Projeto ASP.NET Core
├── SquadCli/
│   └── CLI da squad
└── EcommerceRefatorado.slnx
```

As pastas `Api`, `Application`, `Domain` e `Infrastructure` representam a organização planejada para a solução, mas ainda não fazem parte do projeto web compilado. A API que está configurada na solução fica em `EcommerceRefatorado/`.

## Requisitos

- .NET SDK 10
- Git, para usar o comando `squad start`

## Compilar e executar

Na raiz do repositório:

```powershell
dotnet restore EcommerceRefatorado.slnx
dotnet build EcommerceRefatorado.slnx
dotnet run --project EcommerceRefatorado\EcommerceRefatorado.csproj
```

No perfil HTTP, a API usa `http://localhost:5259`. O endpoint atualmente disponível é:

```http
GET http://localhost:5259/weatherforecast
```

Em ambiente de desenvolvimento, o documento OpenAPI também é disponibilizado em `/openapi/v1.json`.

## CLI da squad

Execute os comandos a partir da raiz do repositório:

```powershell
dotnet run --project SquadCli\SquadCli.csproj -- --help
```

Comandos disponíveis:

- `setup`: exibe mensagens de configuração; ainda não restaura dependências nem configura ou popula um banco de dados.
- `tasks [--me]`: exibe uma tabela com áreas e responsáveis fictícios. A opção `--me` ainda não filtra as tarefas.
- `start <id>`: tenta criar e selecionar a branch `feature/issue-<id>` usando Git no diretório atual.
- `test`: executa `dotnet test --filter Category=Unit`; ainda não há testes no repositório.

## Planejamento registrado

O script `criar_issues.sh` contém comandos `gh issue create` para registrar etapas de planejamento e frentes de trabalho de infraestrutura, produtos/estoque, pedidos/descontos e integrações/testes. O script depende do GitHub CLI (`gh`) autenticado e ainda não representa funcionalidades já concluídas.
