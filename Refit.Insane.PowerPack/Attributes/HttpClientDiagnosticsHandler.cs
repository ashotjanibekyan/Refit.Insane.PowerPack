using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Refit.Insane.PowerPack.Attributes;

public class HttpClientDiagnosticsHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
{
    public HttpClientDiagnosticsHandler() : this(new HttpClientHandler())
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!Debugger.IsAttached)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        HttpResponseMessage httpResponseMessage;
        var totalElapsedTime = Stopwatch.StartNew();
        Trace.WriteLine($"Request: {request}");
        if (request?.Content != null)
        {
            var str = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            Trace.WriteLine($"Request Content: {str}");
        }

        var responseElapsedTime = Stopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken);
        Trace.WriteLine($"Response: {response}");
        if (response?.Content != null)
        {
            var str = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Trace.WriteLine($"Response Content: {str}");
        }

        responseElapsedTime.Stop();
        Trace.WriteLine($"Response elapsed time: {responseElapsedTime.ElapsedMilliseconds} ms");
        totalElapsedTime.Stop();
        Trace.WriteLine($"Total elapsed time: {totalElapsedTime.ElapsedMilliseconds} ms");
        httpResponseMessage = response;
        return httpResponseMessage;
    }
}