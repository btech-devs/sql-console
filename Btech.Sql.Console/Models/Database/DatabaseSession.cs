using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Btech.Core.Database.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Btech.Sql.Console.Models.Database;

public class DatabaseSession : EntityBase
{
    #region Public Constants

    public const string TableName = "database_sessions";

    public const string UserEmailColumnName = "user_email";
    public const string AccessTokenColumnName = "access_token";
    public const string ConnectionStringColumnName = "connection_string";
    public const string RefreshTokenColumnName = "refresh_token";

    #endregion Public Constants

    #region Public Properties

    [Column(UserEmailColumnName)]
    public string UserEmail { get; set; }

    [Column(ConnectionStringColumnName)]
    public string ConnectionString { get; set; }

    [Key]
    [Column(AccessTokenColumnName)]
    public string AccessToken { get; set; }

    [Column(RefreshTokenColumnName)]
    public string RefreshToken { get; set; }

    public UserSession UserSession { get; set; }

    #endregion Public Properties

    public override void Setup(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<DatabaseSession> entityBuilder = modelBuilder
            .Entity<DatabaseSession>()
            .ToTable(TableName);

        entityBuilder
            .HasOne(dbSession => dbSession.UserSession)
            .WithMany(userSession => userSession.DbSessions)
            .HasForeignKey(dbSession => dbSession.UserEmail);
    }
}