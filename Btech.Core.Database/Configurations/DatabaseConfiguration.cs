using System.Diagnostics.CodeAnalysis;

namespace Btech.Core.Database.Configurations;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class DatabaseConfiguration
{
    public string Host { get; init; }

    public string Database { get; init; }

    public string Username { get; init; }

    public string Password { get; init; }

    /// <summary>
    /// Default is 'true'.
    /// </summary>
    public bool Pooling { get; init; } = true;

    /// <summary>
    /// Default is 20.
    /// </summary>
    public int MaxPoolSize { get; init; } = 50;

    /// <summary>
    /// Default is 600.
    /// </summary>
    public int CommandTimeout { get; init; } = 600;

    /// <summary>
    /// Default is 1000.
    /// </summary>
    public int MaxBatchSize { get; init; } = 1000;

    /// <summary>
    /// Default is 'true'.
    /// </summary>
    public bool Ssl { get; init; } = false;

    public string ConnectionString => this.Ssl
        ? $"Host={this.Host};Database={this.Database};Username={this.Username};Password={this.Password};Pooling={this.Pooling};Maximum Pool Size={this.MaxPoolSize};Ssl={this.Ssl};Ssl Mode=verify-ca;Root Certificate=ssl/server-ca.pem;SSL Certificate=ssl/client-cert.pemSSL Key=ssl/client-key.pem"
        : $"Host={this.Host};Database={this.Database};Username={this.Username};Password={this.Password};Pooling={this.Pooling};Maximum Pool Size={this.MaxPoolSize}";
}