
using System.Text.Json;
using EcommerceRefatorado;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EcommerceRefatorado.Tests.Api;

public class ErrorHandlingMiddlewareTests
{
    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("dominio", 400)]
    [InlineData("naoEncontrado", 404)]
    [InlineData("inesperado", 500)]
    public async Task Middleware_DeveRetornarStatusCorreto(
        string tipoErro,
        int statusEsperado)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => tipoErro switch
        {
            "dominio" => throw new DomainException(
                "Regra de negócio inválida."),

            "naoEncontrado" => throw new KeyNotFoundException(
                "Recurso não encontrado."),

            _ => throw new Exception(
                "Erro inesperado.")
        };

        var logger =
            NullLogger<ErrorHandlingMiddleware>.Instance;

        var middleware = new ErrorHandlingMiddleware(
            next, logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(
            statusEsperado,
            context.Response.StatusCode);

        Assert.Equal(
            "application/json",
            context.Response.ContentType);

        context.Response.Body.Position = 0;

        using var documento =
            await JsonDocument.ParseAsync(context.Response.Body);

        Assert.Equal(
            statusEsperado,
            documento.RootElement
                .GetProperty("status")
                .GetInt32());

        var mensagem = documento.RootElement
            .GetProperty("mensagem")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(mensagem));

        if (statusEsperado == 500)
        {
            Assert.Equal(
                "Ocorreu um erro interno no servidor.",
                mensagem);
        }
    }
}
