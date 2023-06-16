using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Btech.Core.Database.Base;
using Microsoft.EntityFrameworkCore;

namespace Btech.Sql.Console.Models.Database;

public class SavedQuery : EntityBase
{
    #region Public Constants

    public const string TableName = "saved_queries";

    public const string IdColumnName = "id";
    public const string UserEmailColumnName = "user_email";
    public const string QueryNameColumnName = "query_name";
    public const string QueryColumnName = "query";

    #endregion Public Constants

    #region Public Properties

    [Column(IdColumnName)]
    [Key]
    public long? Id { get; set; }

    [Column(UserEmailColumnName)]
    public string UserEmail { get; set; }

    [Column(QueryNameColumnName)]
    public string QueryName { get; set; }

    [Column(QueryColumnName)]
    public string Query { get; set; }

    #endregion Public Properties

    public override void Setup(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<SavedQuery>()
            .ToTable(TableName)
            .HasKey(query => query.Id);
    }
}