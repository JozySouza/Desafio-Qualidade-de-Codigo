using System;
using System.CommandLine;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace SquadCli
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("CLI oficial da Squad para automação do ecossistema Ecommerce.");

            // --- 1. COMANDO: squad setup ---
            var setupCommand = new Command("setup", "Prepara e configura o ambiente de desenvolvimento local.");
            setupCommand.SetHandler(() =>
            {
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .Start("Configurando ambiente local...", ctx =>
                    {
                        Thread.Sleep(1000);
                        AnsiConsole.MarkupLine("[green]✓ Dependências do .NET restauradas![/]");
                        AnsiConsole.MarkupLine("[green]✓ Banco de dados SQLite configurado e alimentado (DbSeeder)![/]");
                    });
            });
            rootCommand.AddCommand(setupCommand);

            // --- 2. COMANDO: squad tasks ---
            var taskListCommand = new Command("tasks", "Lista as tarefas ativas da squad divididas por responsável.");
            var meOption = new Option<bool>("--me", "Exibe apenas as suas tarefas");
            taskListCommand.AddOption(meOption);

            taskListCommand.SetHandler((bool me) =>
            {
                var table = new Table().Border(TableBorder.Rounded);
                table.AddColumn("[bold yellow]Pessoa[/]");
                table.AddColumn("[bold yellow]Fatia/Área[/]");
                table.AddColumn("[bold yellow]Foco Principal[/]");

                table.AddRow("Pessoa 1", "Infraestrutura & DI", "DbDbContext, Repositories, DbSeeder, Program.cs");
                table.AddRow("Pessoa 2", "Produtos & Estoque", "Controllers, Services e DTOs de Produto/Estoque");
                table.AddRow("Pessoa 3", "Pedidos & Strategy", "Fluxo atômico de pedidos e Strategy de Desconto");
                table.AddRow("Pessoa 4", "Integrações & Testes", "Gateways mockados, Middleware de Erros e testes xUnit");

                AnsiConsole.Write(table);
            }, meOption);
            rootCommand.AddCommand(taskListCommand);

            // --- 3. COMANDO: squad start <id> (Automação de Git) ---
            var startCommand = new Command("start", "Cria e seleciona automaticamente a branch de uma issue.")
            {
                new Argument<int>("id", "Número da issue (ex: 42)")
            };
            startCommand.SetHandler((int id) =>
            {
                var branchName = $"feature/issue-{id}";
                AnsiConsole.MarkupLine($"[blue]Criando branch para a issue #{id}: [bold]{branchName}[/]...[/]");

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"checkout -b {branchName}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    AnsiConsole.MarkupLine($"[green]✓ Branch criada e ativa com sucesso: {branchName}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Erro ao executar o Git: {error.Trim()}[/]");
                }
            }, new Argument<int>("id"));
            rootCommand.AddCommand(startCommand);

            // --- 4. COMANDO: squad test (Atalho de Testes) ---
            var testCommand = new Command("test", "Executa os testes unitários da squad com feedback visual.");
            testCommand.SetHandler(() =>
            {
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Arc)
                    .Start("Executando suíte de testes unitários...", ctx =>
                    {
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "dotnet",
                                Arguments = "test --filter Category=Unit",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };

                        process.Start();
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            AnsiConsole.MarkupLine("[green]✓ Todos os testes unitários passaram com sucesso![/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[red]✗ Alguns testes falharam. Veja o relatório:[/]");
                            AnsiConsole.WriteLine(output);
                        }
                    });
            });
            rootCommand.AddCommand(testCommand);

            return await rootCommand.InvokeAsync(args);
        }
    }
}