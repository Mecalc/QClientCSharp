// -------------------------------------------------------------------------
// Copyright (c) Mecalc (Pty) Limited. All rights reserved.
// -------------------------------------------------------------------------

using QProtocol.ErrorHandling;
using QProtocol.Interfaces;
using System.Text;
using System.Text.Json;

namespace QProtocolExtended.RestfulClient;

public class RestfulInterface : IRestfulInterface
{
    private const int timeout = 60;
    private JsonElementToInferredTypesConverter customConverter = new();
    private JsonSerializerOptions serializerOptions = new();

    public string Url { get; }

    public static HttpClient Client { get; internal set; } = new() { Timeout = new TimeSpan(0, 0, timeout) };

    public string? LastResponse { get; internal set; }

    public RestfulInterface(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException($"Argument {nameof(url)} may not be null or empty!");
        }

        Url = url;
        serializerOptions.Converters.Add(customConverter);
    }

    public virtual void Put(string request, params HttpParameter[] parameters)
    {
        Put(request, body: null, parameters: parameters);
    }

    public virtual void Put(string request, object body, params HttpParameter[] parameters)
    {
        var buildUri = new StringBuilder();
        buildUri.Append(Url);
        buildUri.Append(request);
        if (parameters != null && parameters.Length > 0)
        {
            buildUri.Append($"?{string.Join("&", parameters.Select(item => $"{item.Name}={item.Value}"))}");
        }

        var jsonBody = JsonSerializer.Serialize(body);
        var httpJsonBody = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        try
        {
            using var httpResponse = Client.PutAsync(buildUri.ToString(), httpJsonBody).Result;
            LastResponse = httpResponse.Content.ReadAsStringAsync().Result;

            if (IsSupportedStatusCode(httpResponse.StatusCode) == false)
            {
                throw new ApplicationException($"PUT command failed: {request} with error message: {LastResponse}");
            }

            QProtocolResponseChecks.CheckAndThrow(LastResponse);
        }
        catch (AggregateException info)
        {
            if (info.InnerException is TaskCanceledException canceledException)
            {
                throw new TimeoutException($"Unable to reach the QServer on the provided URL {Url}.");
            }
            else
            {
                throw info.InnerException;
            }
        }
    }

    public virtual T Get<T>(string request, params HttpParameter[] parameters)
    {
        var buildUri = new StringBuilder();
        buildUri.Append(Url);
        buildUri.Append(request);
        if (parameters != null && parameters.Length > 0)
        {
            buildUri.Append($"?{string.Join("&", parameters.Select(item => $"{item.Name}={item.Value}"))}");
        }

        try
        {
            using var httpResponse = Client.GetAsync(buildUri.ToString()).Result;
            LastResponse = httpResponse.Content.ReadAsStringAsync().Result;

            if (IsSupportedStatusCode(httpResponse.StatusCode) == false)
            {
                throw new ApplicationException($"GET command failed: {request} with error message: {LastResponse}");
            }

            QProtocolResponseChecks.CheckAndThrow(LastResponse);
            return JsonSerializer.Deserialize<T>(LastResponse, serializerOptions)!;
        }
        catch (AggregateException info)
        {
            if (info.InnerException is TaskCanceledException canceledException)
            {
                throw new TimeoutException($"Unable to reach the QServer on the provided URL {Url}.");
            }
            else
            {
                throw info.InnerException;
            }
        }
    }

    public virtual void Delete(string request, params HttpParameter[] parameters)
    {
        var buildUri = new StringBuilder();
        buildUri.Append(Url);
        buildUri.Append(request);
        if (parameters != null && parameters.Length > 0)
        {
            buildUri.Append($"?{string.Join("&", parameters.Select(item => $"{item.Name}={item.Value}"))}");
        }

        try
        {
            using var httpResponse = Client.DeleteAsync(buildUri.ToString()).Result;
            LastResponse = httpResponse.Content.ReadAsStringAsync().Result;

            if (IsSupportedStatusCode(httpResponse.StatusCode) == false)
            {
                throw new ApplicationException($"DELETE command failed: {request} with error message: {LastResponse}");
            }

            QProtocolResponseChecks.CheckAndThrow(LastResponse);
        }
        catch (AggregateException info)
        {
            if (info.InnerException is TaskCanceledException canceledException)
            {
                throw new TimeoutException($"Unable to reach the QServer on the provided URL {Url}.");
            }
            else
            {
                throw info.InnerException;
            }
        }
    }

    private bool IsSupportedStatusCode(System.Net.HttpStatusCode receivedCode)
    {
        switch (receivedCode)
        {
            case System.Net.HttpStatusCode.OK:
            case System.Net.HttpStatusCode.NoContent:
            case System.Net.HttpStatusCode.BadRequest:
            case System.Net.HttpStatusCode.NotFound:
            case System.Net.HttpStatusCode.MethodNotAllowed:
            case System.Net.HttpStatusCode.InternalServerError:
            case System.Net.HttpStatusCode.NotImplemented:
                return true;
            default:
                return false;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
        }
    }

    ~RestfulInterface()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }
}
