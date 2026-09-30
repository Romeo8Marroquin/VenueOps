using System.Net;
using System.Text;

namespace VenueOps.Tests.TestDoubles;

/// <summary>
/// Hand-written HttpMessageHandler that never touches the network.
/// Records every request (method, URI and body) and replies with a canned response.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string? _json;

    public StubHttpMessageHandler(HttpStatusCode statusCode, string? json = null)
    {
        _statusCode = statusCode;
        _json = json;
    }

    public List<RecordedRequest> Requests { get; } = [];

    public RecordedRequest LastRequest => Requests[^1];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new RecordedRequest(request.Method, request.RequestUri!, body));

        HttpResponseMessage response = new(_statusCode);
        if (_json is not null)
            response.Content = new StringContent(_json, Encoding.UTF8, "application/json");
        return response;
    }

    public sealed record RecordedRequest(HttpMethod Method, Uri Uri, string? Body)
    {
        /// <summary>Path and query relative to the fake API base address.</summary>
        public string RelativeUrl => Uri.PathAndQuery.TrimStart('/');
    }
}
