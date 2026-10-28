# EcommerceRefatorado

API de e-commerce desenvolvida em ASP.NET Core/.NET 10, com persistência em SQLite, documentação Swagger e uma CLI de apoio à squad.

O projeto foi desenvolvido como parte do desafio de **Qualidade de Código e Arquitetura da WoMakersCode**, com o objetivo de refatorar uma API de e-commerce que apresentava problemas intencionais de organização, manutenção e regras de negócio.

Durante o desenvolvimento, buscamos aplicar conceitos de **Clean Code, SOLID, Programação Orientada a Objetos, separação de responsabilidades, injeção de dependências, padrões de projeto e testes automatizados**.

O foco não foi apenas fazer a aplicação funcionar, mas também melhorar a maneira como o código está organizado, tornando as responsabilidades mais claras e facilitando futuras alterações.

## Estado atual

A aplicação conta com as seguintes funcionalidades:

- CRUD de produtos, com validação dos dados de entrada.
- Exclusão lógica de produtos, preservando os registros no banco.
- Consulta e reabastecimento de estoque.
- Criação, listagem e consulta de pedidos.
- Processamento de pagamentos por meio de um gateway simulado.
- Reprocessamento de pagamentos de pedidos com status `Falhou`.
- Envio e reenvio de pedidos para a logística simulada.
- Aplicação de descontos utilizando o padrão Strategy.
- Reserva atômica de estoque para evitar vendas acima da quantidade disponível.
- Controle de transações no SQLite.
- Devolução de estoque em caso de pagamento recusado ou falha simulada.
- Tratamento global de exceções.
- Documentação dos endpoints com Swagger.
- Testes automatizados com xUnit.
- Pipeline de CI/CD com GitHub Actions.
- CLI com comandos de apoio ao desenvolvimento.

O projeto utiliza Entity Framework Core para acesso ao banco de dados e injeção de dependências para conectar os serviços, repositórios e gateways.

A versão integrada à branch `main` foi validada com **41 testes automatizados aprovados**.

## Estrutura

```text
.
├── Api/
│   ├── Controllers/
│   └── Middlewares/
│
├── Application/
│   ├── DTOs/
│   ├── Interfaces/
│   └── Services/
│
├── Domain/
│   ├── Descontos/
│   ├── Entities/
│   ├── Enums/
│   └── Exceptions/
│
├── Infrastructure/
│   ├── Data/
│   ├── Gateways/
│   └── Repositories/
│
├── EcommerceRefatorado.Api/
│   └── Projeto ASP.NET Core e arquivo .csproj
│
├── EcommerceRefatorado.Tests/
│   ├── Api/
│   ├── Application/
│   ├── Domain/
│   ├── Fakes/
│   └── Infrastructure/
│
├── SquadCli/
│   └── CLI da squad
│
└── EcommerceRefatorado.slnx
```

As pastas `Api`, `Application`, `Domain` e `Infrastructure` ficam na raiz do repositório.

O projeto web, localizado em `EcommerceRefatorado.Api/`, inclui esses arquivos por meio do `EcommerceRefatorado.csproj`.

### Organização das responsabilidades

| Camada | Responsabilidade |
|---|---|
| **Api** | Receber requisições HTTP, disponibilizar endpoints e tratar erros. |
| **Application** | Coordenar os fluxos da aplicação e aplicar as regras de negócio. |
| **Domain** | Representar entidades, enums, exceções e estratégias de desconto. |
| **Infrastructure** | Realizar persistência de dados e implementar integrações externas simuladas. |
| **EcommerceRefatorado.Tests** | Validar as regras e os comportamentos da aplicação. |

Essa organização substitui a estrutura original, na qual grande parte das responsabilidades estava concentrada em um único controller.

## Requisitos

Para executar o projeto localmente, é necessário:

- .NET SDK 10.
- Git, caso queira utilizar os comandos de Git da CLI.

## Compilar e executar

Na raiz do repositório, execute:

```powershell
dotnet restore .\EcommerceRefatorado.slnx
dotnet build .\EcommerceRefatorado.slnx
dotnet run --project .\EcommerceRefatorado.Api\EcommerceRefatorado.csproj
```

No perfil HTTP, a aplicação utiliza:

```text
http://localhost:5259
```

Em ambiente de desenvolvimento, o Swagger está disponível em:

```text
http://localhost:5259/swagger
```

O documento OpenAPI pode ser consultado em:

```text
http://localhost:5259/swagger/v1/swagger.json
```

Caso a porta esteja ocupada, é possível iniciar a aplicação utilizando outra:

```powershell
dotnet run --project .\EcommerceRefatorado.Api\EcommerceRefatorado.csproj --urls http://localhost:5261
```

## Produtos

A API disponibiliza operações para cadastrar, consultar, atualizar e desativar produtos.

| Método | Rota | Resultado |
|---|---|---|
| `GET` | `/api/Produtos` | Lista os produtos ativos. |
| `GET` | `/api/Produtos/{id}` | Retorna um produto ou `404`. |
| `POST` | `/api/Produtos` | Cria um produto e retorna `201`. |
| `PUT` | `/api/Produtos/{id}` | Atualiza um produto; retorna `204` ou `404`. |
| `DELETE` | `/api/Produtos/{id}` | Desativa um produto; retorna `204` ou `404`. |

### Exemplo de cadastro

```json
{
  "nome": "Camiseta",
  "descricao": "Camiseta basica",
  "preco": 49.90,
  "estoque": 100
}
```

### Validações

Os dados enviados são validados antes do processamento.

As principais regras incluem:

- Nome obrigatório, com até 150 caracteres.
- Descrição obrigatória, com até 500 caracteres.
- Preço maior que zero.
- Quantidade de estoque não negativa.

### Exclusão lógica

A exclusão de produtos é realizada por meio de desativação lógica.

Isso significa que o produto não é removido fisicamente do banco de dados.

Em vez disso, seu status `Ativo` é alterado, preservando o registro para consultas e relacionamentos históricos.

Produtos desativados não aparecem nas listagens de produtos ativos.

## Estoque

A API oferece funcionalidades para consulta e reabastecimento de estoque.

| Método | Rota | Resultado |
|---|---|---|
| `GET` | `/api/estoque` | Lista o estoque dos produtos ativos. |
| `POST` | `/api/estoque/reabastecer/{produtoId}/{quantidade}` | Reabastece o estoque de um produto. |

### Regras implementadas

- Não é permitido reabastecer com quantidades negativas ou iguais a zero.
- Não é permitido debitar uma quantidade superior ao estoque disponível.
- Produtos desativados não podem receber novas reservas para pedidos.
- As regras de movimentação também são encapsuladas na entidade `Produto`.

### Controle de concorrência

Um dos problemas identificados durante a refatoração estava relacionado às compras simultâneas.

Imagine que um produto tenha apenas uma unidade disponível e dois clientes tentem comprá-lo ao mesmo tempo.

Se a aplicação apenas consultar o estoque e depois realizar a baixa, as duas requisições poderão encontrar a mesma unidade disponível.

Para evitar esse comportamento, implementamos uma reserva atômica diretamente no SQLite.

A operação verifica a disponibilidade e reduz o estoque em uma única instrução SQL.

```sql
UPDATE "Produtos"
SET "Estoque" = "Estoque" - @quantidade
WHERE "Id" = @produtoId
  AND "Ativo" = 1
  AND "Estoque" >= @quantidade;
```

A reserva somente é considerada bem-sucedida quando a operação atualiza o registro esperado.

Dessa forma, duas compras não conseguem reservar simultaneamente a mesma última unidade disponível.

A implementação também utiliza transações e savepoints para controlar reservas que envolvem mais de um produto.

## Pedidos

A API disponibiliza operações para criação, consulta e acompanhamento dos pedidos.

| Método | Rota | Resultado |
|---|---|---|
| `GET` | `/api/Pedidos` | Lista os pedidos cadastrados. |
| `GET` | `/api/Pedidos/{id}` | Consulta um pedido pelo identificador. |
| `POST` | `/api/Pedidos` | Cria um pedido e processa o pagamento simulado. |
| `POST` | `/api/Pedidos/{id}/pagamento` | Reprocessa o pagamento de um pedido com status `Falhou`. |
| `POST` | `/api/Pedidos/{id}/envio` | Solicita novo envio logístico para um pedido pago ou enviado. |

### Exemplo de criação de pedido

```json
{
  "clienteNome": "Cliente Teste",
  "clienteEmail": "cliente@exemplo.com",
  "formaPagamento": 1,
  "itens": [
    {
      "produtoId": 1,
      "quantidade": 2
    }
  ]
}
```

No exemplo, o valor `1` representa a forma de pagamento `Pix`, conforme o enum utilizado na aplicação.

### Fluxo de criação de pedidos

O processamento é coordenado pelo `PedidoService`.

O fluxo funciona da seguinte maneira:

1. A aplicação recebe os dados do pedido.
2. Valida as informações do cliente e dos produtos.
3. Consulta os produtos e verifica inicialmente a disponibilidade.
4. Calcula o valor total e aplica as estratégias de desconto.
5. Registra o pedido com status `Pendente`.
6. Inicia uma transação no SQLite.
7. Tenta reservar o estoque de forma atômica.
8. Processa o pagamento por meio do gateway simulado.
9. Atualiza o status do pedido de acordo com o resultado.
10. Confirma a transação.
11. Se o pagamento for aprovado, solicita o envio à logística.

A validação inicial evita pedidos inválidos, enquanto a reserva atômica protege o estoque contra alterações simultâneas.

### Pagamento aprovado

Quando o pagamento é aprovado:

- O estoque reservado permanece debitado.
- O pedido recebe status `Pago`.
- A transação é confirmada.
- A aplicação solicita o envio logístico.

Se o envio for concluído, o pedido recebe status `Enviado` e um código de rastreamento.

### Pagamento recusado

Quando o pagamento simulado é recusado:

- O estoque reservado é liberado.
- O pedido recebe status `Falhou`.
- A falha fica registrada para permitir um novo processamento.
- A logística não é acionada.

### Falha no gateway de pagamento

O sistema também trata exceções simuladas durante o processamento do pagamento.

Nesse cenário:

- A falha é registrada em log.
- O estoque reservado é liberado.
- O pedido é atualizado para `Falhou`.
- A aplicação informa que não foi possível concluir o pagamento.

### Reprocessamento de pagamento

Pedidos com status `Falhou` podem passar por uma nova tentativa de pagamento.

O reprocessamento reutiliza o fluxo de reserva de estoque e processamento de pagamento.

Também existe uma verificação no banco para impedir que duas requisições iniciem simultaneamente o reprocessamento do mesmo pedido.

Pedidos já pagos não podem ser cobrados novamente por esse fluxo.

### Reenvio logístico

A aplicação permite solicitar novamente o envio logístico de pedidos com status `Pago` ou `Enviado`.

Pedidos que não tiveram o pagamento confirmado não podem ser encaminhados à logística.

### Falha na logística

Se ocorrer um erro durante o envio logístico após a confirmação do pagamento, o pedido permanece com status `Pago`.

A falha é registrada em log, sem desfazer o pagamento nem devolver o estoque.

Isso permite realizar uma nova tentativa de envio posteriormente.

### Limitação dos gateways simulados

O desafio utiliza gateways mockados de pagamento e logística.

Por esse motivo, o fluxo transacional implementado foi desenvolvido para o cenário simulado da atividade.

Em uma integração real com um serviço de pagamentos externo, seriam necessários mecanismos adicionais, como idempotência, reconciliação de cobranças e tratamento de falhas de comunicação.

## Estratégias de desconto

A aplicação utiliza o padrão de projeto **Strategy** para organizar as regras de desconto.

Em vez de manter essas regras diretamente no fluxo de criação do pedido, cada estratégia possui uma responsabilidade específica.

As implementações são:

- `DescontoValorAlto`
- `DescontoVip`

Ambas utilizam a interface `IRegraDesconto`.

### Regras implementadas

| Condição | Desconto |
|---|---|
| Pedido com valor superior a R$ 500,00 | 10% |
| Cliente identificado como VIP | 20% |
| Pedido sem condição de desconto | 0% |

Quando mais de uma condição é atendida, a aplicação utiliza o maior percentual de desconto disponível.

O cálculo é centralizado na classe `CalculadoraDesconto`.

Essa abordagem permite adicionar ou modificar estratégias sem concentrar todas as condições em um único método.

## Integrações

As integrações de pagamento e logística utilizam interfaces.

Essa separação permite testar os serviços da aplicação sem depender diretamente das implementações concretas.

### Pagamento

O `PagamentoMockGateway` simula o processamento de pagamentos.

Diferentemente do projeto original, o resultado não depende de sorteios aleatórios.

A implementação utiliza um comportamento determinístico baseado no valor e na forma de pagamento informados.

### Logística

O `LogisticaMockGateway` simula o envio de pedidos e a geração de códigos de rastreamento.

As operações utilizam métodos assíncronos e oferecem suporte a `CancellationToken`.

### Injeção de dependências

As interfaces e suas implementações são registradas no sistema de injeção de dependências do ASP.NET Core.

Isso reduz o acoplamento entre as camadas e facilita a substituição das implementações durante os testes.

## Banco de dados

O projeto utiliza **SQLite com Entity Framework Core**.

A connection string `Default` está configurada em:

```text
EcommerceRefatorado.Api/appsettings.json
```

O banco de dados é criado automaticamente durante a inicialização da aplicação.

### Entidades principais

- `Produto`
- `Pedido`
- `ItemPedido`

### Dados iniciais

O `DbSeeder` insere quatro produtos de exemplo quando a tabela `Produtos` está vazia:

| Produto | Estoque inicial |
|---|---:|
| Camiseta | 100 |
| Tenis | 50 |
| Bone | 200 |
| Mochila | 30 |

Se a tabela já possuir produtos, os registros existentes são preservados.

O seeder também verifica a existência de algumas colunas utilizadas pela versão refatorada e realiza ajustes quando necessário.

Atualmente, a aplicação utiliza `EnsureCreated` para criar o esquema inicial.

As migrations do Entity Framework Core ainda não estão configuradas.

## Repositórios e domínio

O acesso aos dados é realizado por meio de repositórios.

As interfaces principais são:

- `IProdutoRepository`
- `IPedidoRepository`

As implementações ficam em:

```text
Infrastructure/Repositories/
```

Os repositórios utilizam operações assíncronas do Entity Framework Core.

### Status dos pedidos

A aplicação utiliza o enum `StatusPedido`:

```csharp
public enum StatusPedido
{
    Pendente,
    Pago,
    Enviado,
    Cancelado,
    Falhou
}
```

### Formas de pagamento

As formas de pagamento são representadas pelo enum `FormaPagamento`:

```csharp
public enum FormaPagamento
{
    NaoInformado,
    Pix,
    CartaoCredito,
    CartaoDebito,
    Boleto
}
```

Os enums são persistidos como texto no banco de dados.

Essa abordagem substitui o uso de strings espalhadas pelo código para representar estados e formas de pagamento.

## Tratamento de erros

A aplicação utiliza o `ErrorHandlingMiddleware` para centralizar o tratamento de exceções.

O middleware registra as falhas utilizando `ILogger` e retorna respostas padronizadas em JSON.

| Exceção | Status HTTP |
|---|---|
| `ArgumentException` | 400 Bad Request |
| `DomainException` | 400 Bad Request |
| `KeyNotFoundException` | 404 Not Found |
| Exceções inesperadas | 500 Internal Server Error |

Para erros internos, a API retorna uma mensagem genérica, evitando expor detalhes técnicos ao consumidor.

Com isso, os controllers não precisam repetir blocos de tratamento de exceções para os mesmos tipos de erro.

## Testes automatizados

O projeto utiliza **xUnit** para validar regras de negócio e comportamentos da aplicação.

Os testes estão organizados em:

```text
EcommerceRefatorado.Tests/
├── Api/
├── Application/
├── Domain/
├── Fakes/
└── Infrastructure/
```

### Cenários validados

A suíte inclui testes para:

- Reabastecimento e baixa de estoque.
- Quantidades inválidas.
- Estoque insuficiente.
- Desativação lógica de produtos.
- Descontos por valor do pedido.
- Descontos para clientes VIP.
- Pagamentos aprovados.
- Pagamentos recusados.
- Falhas simuladas no gateway de pagamento.
- Reprocessamento de pagamentos.
- Bloqueio de reprocessamento de pedidos já pagos.
- Envio e reenvio logístico.
- Falhas logísticas após pagamento aprovado.
- Falhas na atualização de pedidos.
- Tratamento de exceções HTTP 400, 404 e 500.
- Concorrência durante a reserva de estoque.
- Rollback de transações no SQLite.

### Testes de concorrência

Foi implementado um teste de integração utilizando um banco SQLite real.

O teste simula duas compras simultâneas disputando a última unidade disponível de um produto.

O resultado esperado é que somente uma das operações consiga reservar o estoque.

Também foi implementado um teste que provoca uma falha durante uma transação e verifica se o SQLite restaura o estoque ao valor anterior.

Esses testes ajudam a validar o comportamento do banco de dados, além das regras verificadas nos testes unitários.

### Executar os testes

Na raiz do repositório:

```powershell
dotnet test .\EcommerceRefatorado.slnx
```

### Resultado da validação

A versão integrada na branch `main` foi validada em **28/10/2026**.

| Indicador | Resultado |
|---|---:|
| Testes executados | 41 |
| Testes aprovados | 41 |
| Testes com falha | 0 |

A execução também foi confirmada pelo GitHub Actions após a integração das alterações.

## CI/CD

O projeto possui um workflow configurado no GitHub Actions:

```text
.github/workflows/ci-cd.yml
```

### Integração contínua

O pipeline executa automaticamente:

1. Download do código.
2. Configuração do .NET 10.
3. Restauração das dependências.
4. Compilação da solução.
5. Execução dos testes unitários.
6. Execução da suíte completa de testes.
7. Coleta dos resultados e dados de cobertura.

### Geração do pacote da API

Quando ocorre um push na branch `main` e a etapa de testes é aprovada, o workflow também gera um pacote publicável da API.

O pacote é disponibilizado como artefato do GitHub Actions.

A configuração atual não realiza implantação automática em um servidor externo.

### Resultado final

A execução realizada após o merge das alterações de pedidos e concorrência foi concluída com sucesso.

Foram aprovadas as etapas de:

- Compilação.
- Testes unitários.
- Testes completos.
- Geração do pacote da API.

## CLI da squad

O projeto também possui uma CLI desenvolvida como ferramenta de apoio à squad.

Para consultar os comandos:

```powershell
dotnet run --project .\SquadCli\SquadCli.csproj -- --help
```

### Comandos disponíveis

| Comando | Funcionalidade |
|---|---|
| `setup` | Exibe mensagens simuladas de preparação do ambiente. |
| `tasks` | Apresenta uma tabela de tarefas da squad. |
| `tasks --me` | Aceita a opção, mas ainda não aplica filtro por integrante. |
| `start <id>` | Tenta criar uma branch Git com base no identificador informado. |
| `test` | Executa os testes marcados com a categoria `Unit`. |

A CLI possui funcionalidades de apoio em diferentes níveis de implementação.

O comando `setup`, por exemplo, apresenta mensagens de configuração, mas não executa a restauração real das dependências nem a criação do banco.

## Problemas identificados e melhorias realizadas

O projeto original foi disponibilizado com problemas intencionais para que a squad pudesse identificar oportunidades de refatoração.

As principais melhorias realizadas foram:

| Problema original | Solução aplicada |
|---|---|
| Controller com muitas responsabilidades | Separação dos controllers e serviços. |
| Ausência de camadas | Organização em Api, Application, Domain e Infrastructure. |
| Dependências criadas diretamente no código | Injeção de dependências. |
| Connection string fixa | Configuração por `appsettings.json`. |
| Entidades expostas diretamente nos endpoints | Criação de DTOs. |
| Regras de desconto misturadas ao pedido | Aplicação do padrão Strategy. |
| Estoque debitado antes do pagamento | Reserva controlada e atualização conforme o resultado do pagamento. |
| Falta de proteção contra compras simultâneas | Reserva atômica de estoque no SQLite. |
| Operações assíncronas bloqueadas com `.Result` | Uso de `async/await`. |
| Duplicação de regras de negócio | Reutilização dos serviços de aplicação. |
| Classe utilitária com responsabilidades diferentes | Distribuição das responsabilidades entre os componentes. |
| Pagamento definido aleatoriamente | Gateway simulado com comportamento determinístico. |
| Tratamento inadequado de exceções | Middleware global e exceções de domínio. |
| Status representados por strings soltas | Utilização de enums. |
| Código concentrado em um único projeto sem organização | Separação lógica das responsabilidades e projeto de testes. |

## Decisões de arquitetura

Durante a refatoração, algumas decisões foram importantes para melhorar a organização e a manutenção da aplicação.

### Separação de responsabilidades

Os controllers ficaram responsáveis por receber as requisições e retornar respostas HTTP.

As regras de negócio foram direcionadas aos serviços e às entidades de domínio.

O acesso ao banco passou a ser responsabilidade dos repositórios.

### Aplicação de SOLID

A separação entre interfaces e implementações permitiu reduzir dependências diretas entre os componentes.

O uso de estratégias de desconto também facilitou a extensão das regras sem concentrar todas as condições em um único método.

### Testabilidade

A utilização de interfaces e gateways simulados permitiu testar diferentes cenários, incluindo pagamentos recusados, falhas de integração e comportamentos do domínio.

### Consistência dos dados

O fluxo de pedidos recebeu atenção especial por envolver operações relacionadas ao estoque e ao pagamento.

A utilização de transações, savepoints e atualizações atômicas no SQLite ajudou a evitar inconsistências durante o processamento simulado.

## Planejamento registrado

A organização das atividades foi realizada por meio de Issues e Pull Requests no GitHub.

As responsabilidades foram divididas entre infraestrutura, produtos e estoque, pedidos e descontos, integrações e testes.

O script `criar_issues.sh` contém comandos do GitHub CLI para registrar etapas de planejamento.

Para utilizar esse script, é necessário possuir o GitHub CLI (`gh`) instalado e autenticado.

## Considerações finais

O desafio permitiu trabalhar com situações que vão além da implementação de endpoints.

Durante a refatoração, foi necessário analisar o código existente, identificar responsabilidades misturadas, revisar regras de negócio e pensar em como evitar problemas que poderiam surgir durante a execução da aplicação.

Um dos aprendizados importantes foi perceber que um código pode funcionar em um cenário simples e ainda apresentar falhas quando diferentes operações acontecem ao mesmo tempo.

A implementação dos testes também ajudou a validar as decisões tomadas, especialmente nos cenários de estoque, pagamento e persistência.

O resultado é uma aplicação mais organizada, com responsabilidades separadas, regras de negócio mais claras e testes que ajudam a preservar os comportamentos esperados.

O projeto continua sendo uma implementação acadêmica, com integrações simuladas e oportunidades de evolução, mas representa a aplicação prática dos conceitos estudados durante o bootcamp.

---

**Desafio de Qualidade de Código e Arquitetura**

**Bootcamp Back-End .NET | WoMakersCode | 2026**