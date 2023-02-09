namespace Btech.Core.Database.Interfaces;

public interface IUnitOfWorkFactory
{
    /// <summary>
    /// Gets a Unit of Work object.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    IUnitOfWork GetUnitOfWork();
}