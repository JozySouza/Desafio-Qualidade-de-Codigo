# Pipeline CI/CD (GitHub Actions) – EcommerceRefatorado

## O que o pipeline faz

| Etapa | Quando roda | O que faz |
| --- | --- | --- |
| **CI** (`build-e-testes`) | Todo push e todo Pull Request | `dotnet restore`, `dotnet build`, `dotnet test`. Salva resultados (.trx) e cobertura como artefato `resultado-testes`. |
| **CD** (`publicar-api`) | Só em push na `main` e só se o CI passou | `dotnet publish` da API e salva o pacote como artefato `ecommerce-api`. |

## Onde colocar cada arquivo

Extraia o zip na **raiz do repositório** (a pasta onde está o `.git`, ao lado de `EcommerceRefatorado/` e `SquadCli/`):

```text
.
├── .github/
│   └── workflows/
│       └── ci-cd.yml                  <- NOVO (o pipeline)
├── EcommerceRefatorado.Tests/         <- NOVO (projeto xUnit)
│   ├── EcommerceRefatorado.Tests.csproj
│   └── SmokeTests.cs
├── EcommerceRefatorado.slnx           <- SUBSTITUI o atual (só adiciona o projeto de testes)
├── EcommerceRefatorado/               (sem mudanças)
├── SquadCli/                          (sem mudanças)
└── PASSO-A-PASSO-CI-CD.md             <- este guia (pode apagar depois)
```

A API e a CLI não foram alteradas. O `.gitignore` de vocês já ignora `bin/` e `obj/`, então não precisa trocar.

## Passo a passo

1. **Crie uma branch** (ex.: `feature/ci-cd`) para não mexer direto na `main`:
   ```bash
   git checkout -b feature/ci-cd
   ```
2. **Extraia o zip** na raiz do repositório e aceite substituir o `EcommerceRefatorado.slnx`.
3. **Teste localmente** na raiz:
   ```bash
   dotnet restore EcommerceRefatorado.slnx
   dotnet build EcommerceRefatorado.slnx
   dotnet test EcommerceRefatorado.slnx
   ```
   Deve aparecer "Passed: 4" (1 teste + 3 casos do `Theory`).
   O comando `squad test` também funciona: os testes têm `[Trait("Category", "Unit")]`.
4. **Commit e push**:
   ```bash
   git add .github EcommerceRefatorado.Tests EcommerceRefatorado.slnx PASSO-A-PASSO-CI-CD.md
   git commit -m "ci: adiciona pipeline CI/CD e projeto de testes xUnit"
   git push -u origin feature/ci-cd
   ```
5. **Veja rodando**: no GitHub, aba **Actions** → workflow **CI-CD**. O push da branch já dispara o job `build-e-testes`.
6. **Abra o Pull Request** para a `main`. O check `build-e-testes` aparece no PR.
7. **Depois do merge na `main`**, o job `publicar-api` roda e, ao terminar, o pacote fica na página da execução, em **Artifacts → ecommerce-api**.

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
