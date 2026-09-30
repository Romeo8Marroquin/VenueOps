using System.Net;
using Moq;

namespace VenueOps.Tests.TestDoubles;

/// <summary>
/// Builds a mocked IHttpClientFactory that hands out an HttpClient backed by a
/// <see cref="StubHttpMessageHandler"/> for the named "VenueOpsApi" client.
/// </summary>
public static class FakeApi
{
    public const string ClientName = "VenueOpsApi";
    public static readonly Uri BaseAddress = new("https://api.example.invalid/");

    public static Mock<IHttpClientFactory> CreateFactory(StubHttpMessageHandler handler)
    {
        Mock<IHttpClientFactory> factory = new();
        factory
            .Setup(f => f.CreateClient(ClientName))
            .Returns(() => new HttpClient(handler) { BaseAddress = BaseAddress });
        return factory;
    }

    public static StubHttpMessageHandler Respond(HttpStatusCode statusCode, string? json = null) => new(statusCode, json);
}
