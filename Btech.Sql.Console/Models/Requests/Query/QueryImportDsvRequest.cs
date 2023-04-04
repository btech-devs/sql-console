using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Btech.Sql.Console.Models.Requests.Query;

public class QueryImportDsvRequest
{
    #region Public Constants

    public const string ChunkSizePropertyName = "chunkSize";
    public const string SeparatorPropertyName = "separator";
    public const string DoubleQuotesPropertyName = "doubleQuotes";
    public const string RollbackOnErrorQuotesPropertyName = "rollbackOnError";
    public const string RowsToSkipQuotesPropertyName = "rowsToSkip";
    public const string FilePropertyName = "file";

    #endregion Public Constants

    #region Public Properties

    [FromForm(Name = SeparatorPropertyName)]
    [RegularExpression(",|\t|;", ErrorMessage = "Separator is not allowed. Allowed values are ',' or '\\t' or ';'.")]
    [Required]
    public char Separator { get; set; }

    [FromForm(Name = ChunkSizePropertyName)]
    [Range(1, 10000)]
    public uint ChunkSize { get; set; } = 1000;

    [FromForm(Name = DoubleQuotesPropertyName)]
    public bool DoubleQuotes { get; set; } = false;

    [FromForm(Name = RollbackOnErrorQuotesPropertyName)]
    public bool RollbackOnError { get; set; } = false;
    
    [FromForm(Name = RowsToSkipQuotesPropertyName)]
    [Range(0, UInt32.MaxValue)]
    public uint RowsToSkip { get; set; } = 0;

    [FromForm(Name = FilePropertyName)]
    [Required]
    public IFormFile File { get; set; }

    #endregion Public Properties
}