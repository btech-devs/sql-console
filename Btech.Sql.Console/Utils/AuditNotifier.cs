using Newtonsoft.Json;
using RestSharp;

namespace Btech.Sql.Console.Utils;

public static class AuditNotifier
{
    private const string TrackableEnvVarName = "TRACKABLE";
    private const string TrackableEnvVarValue = "FALSE";

    // Increases total connection number without collecting any user data.
    public static async Task IncreaseConnectionNumberAsync()
    {
        if (Environment.GetEnvironmentVariable(TrackableEnvVarName) != TrackableEnvVarValue)
            try
            {
                await new RestClient()
                    .ExecuteAsync(
                        new(
                            resource: $"{Constants.AuditApi}/instance-connection",
                            method: Method.Post));
            }
            catch (Exception)
            {
                // ignored
            }
    }

    // Reports about an unexpected exception without collecting any user data.
    public static async Task ReportExceptionAsync(Exception exception, string source = null)
    {
        if (Environment.GetEnvironmentVariable(TrackableEnvVarName) != TrackableEnvVarValue)
            try
            {
                RestRequest restRequest = new(
                    resource: $"{Constants.AuditApi}/report-exception",
                    method: Method.Post);

                restRequest
                    .AddJsonBody(new ExceptionBody(exception, source));

                await new RestClient().ExecuteAsync(restRequest);
            }
            catch (Exception)
            {
                // ignored
            }
    }

    private class ExceptionBody
    {
        public ExceptionBody(Exception exception, string source = null)
        {
            this.Date = DateTime.UtcNow;
            this.Message = exception.Message;
            this.Full = exception.ToString();

            if (exception.InnerException != null)
                this.InnerMessage = exception.InnerException.Message;

            this.Source = source;
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Date { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Source { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string InnerMessage { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Full { get; }
    }
}