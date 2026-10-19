# Pipeline CI/CD (GitHub Actions) – EcommerceRefatorado

## O que o pipeline faz

| Etapa | Quando roda | O que faz |
| --- | --- | --- |
| **CI** (`build-e-testes`) | Todo push e todo Pull Request | `dotnet restore`, `dotnet build`, `dotnet test`. Salva resultados (.trx) e cobertura como artefato `resultado-testes`. |
| **CD** (`publicar-api`) | Só em push na `main` e só se o CI passou | `dotnet publish` da API e salva o pacote como artefato `ecommerce-api`. |

## Recomendado para a equipe

Em **Settings → Branches → Add branch ruleset** (ou *Branch protection rule*) para a `main`:

- Marque **Require a pull request before merging**.
- Marque **Require status checks to pass** e escolha **build-e-testes**.

Assim ninguém faz merge com build ou teste quebrado. (O check só aparece na lista depois da primeira execução do workflow.)

## Como a equipe usa durante a refatoração

Crie os testes novos em `EcommerceRefatorado.Tests/`, sempre com `[Trait("Category", "Unit")]`. Exemplo para a Pessoa 3 (descontos):

```csharp
public class CalculadoraDescontoTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void ClienteVip_RecebeDesconto()
    {
        // Arrange: crie o pedido e a regra DescontoVip
        // Act: calcule o desconto
        // Assert: confira o valor esperado
    }
}
```

Quando as classes do `Domain`/`Application` virarem projetos ou entrarem no projeto web, o workflow continua igual. O `SmokeTests.cs` pode ser apagado depois que houver testes reais.

## Observações

- O workflow usa `dotnet-version: 10.0.x`, igual ao `net10.0` dos projetos.
- Se a equipe quiser que o CD rode também em outra branch, mude a condição `github.ref == 'refs/heads/main'` no job `publicar-api`.
- O artefato fica disponível por 90 dias (padrão do GitHub).
