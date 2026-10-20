#!/bin/bash

# --- ETAPA 0 ---
gh issue create --title "[Etapa 0] Alinhamento e Esqueleto Inicial" --body "Ler o código, confirmar lista de bugs, combinar estrutura de pastas, nomes de interfaces e realizar o primeiro push do esqueleto." --label "planejamento,etapa-0"
criada*
# --- PESSOA 1 ---
gh issue create --title "[Pessoa 1] Infraestrutura, Dados e DI" --body "Resolver problemas 3, 4, 13 e 14. Configurar AppDbContext, Repositories assíncronos, DbSeeder, Program.cs com DI e Enums." --label "infra,backend,pessoa-1"

# --- PESSOA 2 ---
gh issue create --title "[Pessoa 2] Produtos e Estoque" --body "Resolver problemas 1, 5 e 9 (ListarEstoque) + bugs de reabastecer negativo e exclusão lógica. Criar Controllers, Services e DTOs de Produto/Estoque." --label "produtos,api,pessoa-2"

# --- PESSOA 3 ---
gh issue create --title "[Pessoa 3] Pedidos e Strategy de Desconto" --body "Resolver problemas 1, 5, 6, 7 e 9 (reprocessar/reenviar) + bugs do VIP e status. Implementar fluxo atômico de pedido e Strategy de descontos." --label "pedidos,business,pessoa-3"

# --- PESSOA 4 ---
gh issue create --title "[Pessoa 4] Integrações, Tratamento de Erros e Testes" --body "Resolver problemas 8, 10, 11 e 12. Criar Gateways mockados, ErrorHandlingMiddleware, limpar Utils.cs e escrever testes xUnit." --label "testes,arquitetura,pessoa-4"

echo "Todas as issues principais foram criadas com sucesso!"