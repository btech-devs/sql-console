using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Btech.Core.Database.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Btech.Sql.Console.Models.Database;

public class UserSession : EntityBase
{
    #region Public Constants

    public const string TableName = "user_sessions";

    public const string EmailColumnName = "email";
    public const string AccessTokenColumnName = "access_token";
    public const string IdTokenColumnName = "id_token";
    public const string RefreshTokenColumnName = "refresh_token";

    #endregion Public Constants

    #region Public Properties

    [Key]
    [Column(EmailColumnName)]
    public string Email { get; set; }

    [Column(AccessTokenColumnName)]
    public string AccessToken { get; set; }

    [Column(IdTokenColumnName)]
    public string IdToken { get; set; }

    [Column(RefreshTokenColumnName)]
    public string RefreshToken { get; set; }

    public List<DatabaseSession> DbSessions { get; set; }

    #endregion Public Properties

    public override void Setup(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<UserSession> entityBuilder = modelBuilder
            .Entity<UserSession>()
            .ToTable(TableName);

        entityBuilder
            .HasKey(authSession => authSession.Email);
    }
}