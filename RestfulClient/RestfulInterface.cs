// -------------------------------------------------------------------------
// Copyright (c) Mecalc (Pty) Limited. All rights reserved.
// -------------------------------------------------------------------------

using QClient.ErrorHandling;
using QProtocol.Interfaces;
using System.Text;
using System.Text.Json;

namespace QClient.RestfulClient
{
    /// <summary>
    /// This class will establish the HTTP connection to the QServer and provide the actions available.
    /// </summary>
    public class RestfulInterface : IRestfulInterface
    {
        private const int timeout = 60;
        private JsonElementToInferredTypesConverter customConverter = new();
        private JsonSerializerOptions serializerOptions = new();

        public static HttpClient Client { get; internal set; } = new() { Timeout = new TimeSpan(0, 0, timeout) };

        /// <summary>
        /// Gets the URL provided for this instance of the <see cref="RestfulInterface"/>.
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// Gets the last response received from the QServer.
        /// </summary>
        public string? LastResponse { get; internal set; }

        /// <summary>
        /// Creates a new instance of the <see cref="RestfulInterface"/> class.
        /// </summary>
        /// <param name="url">Provide the URL used to connect to the QServer.</param>
        /// <exception cref="ArgumentException">Thrown when the URL is null or empty.</exception>
        public RestfulInterface(string url)
        {
            ArgumentException.ThrowIfNullOrEmpty(nameof(url));

            Url = url;
            serializerOptions.Converters.Add(customConverter);
        }

        /// <summary>
        /// Sends a Put request to the QServer with the specified endpoint and parameters.
        /// </summary>
        /// <param name="endpoint">Specify the endpoint to be used for the request.</param>
        /// <param name="parameters">Specify the parameters if applicable.</param>
        public virtual void Put(string endpoint, params HttpParameter[] parameters)
        {
            Put(endpoint, body: null, parameters: parameters);
        }

        /// <summary>
        /// Sends a Put request to the QServer with the specified endpoint and parameters.
        /// </summary>
        /// <param name="endpoint">Specify the endpoint to be used for the request.</param>
        /// <param name="body">Specify a body for the request.</param>
        /// <param name="parameters">Specify the parameters if applicable.</param>
        public virtual void Put(string endpoint, object body, params HttpParameter[] parameters)
        {
            var buildUri = new StringBuilder();
            buildUri.Append(Url);
            buildUri.Append(endpoint);
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
                    throw new ApplicationException($"PUT command failed: {endpoint} with error message: {LastResponse}");
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

        /// <summary>
        /// Sends a Get requests to the QServer with the specified endpoint and parameters.
        /// </summary>
        /// <typeparam name="T">Specify a Type to which the response will be casted to.</typeparam>
        /// <param name="endpoint">Specify the endpoint to be used for the request.</param>
        /// <param name="parameters">Specify the parameters if applicable.</param>
        /// <returns>An instance of the Type specified.</returns>
        public virtual T Get<T>(string endpoint, params HttpParameter[] parameters)
        {
            var buildUri = new StringBuilder();
            buildUri.Append(Url);
            buildUri.Append(endpoint);
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
                    throw new ApplicationException($"GET command failed: {endpoint} with error message: {LastResponse}");
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

        /// <summary>
        /// Sends a Delete request to the QServer with the specified endpoint and parameters.
        /// </summary>
        /// <param name="endpoint">Specify the endpoint to be used for the request.</param>
        /// <param name="parameters">Specify the parameters if applicable.</param>
        public virtual void Delete(string endpoint, params HttpParameter[] parameters)
        {
            var buildUri = new StringBuilder();
            buildUri.Append(Url);
            buildUri.Append(endpoint);
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
                    throw new ApplicationException($"DELETE command failed: {endpoint} with error message: {LastResponse}");
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
}
