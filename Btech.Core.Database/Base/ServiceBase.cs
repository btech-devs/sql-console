using Btech.Core.Database.Interfaces;
using Microsoft.Extensions.Logging;

namespace Btech.Core.Database.Base;

public abstract class ServiceBase<TEntity> where TEntity : EntityBase, new()
{
    protected readonly ILogger Logger;
    protected readonly IUnitOfWork UnitOfWork;
    private IRepository<TEntity> _repository;

    protected ServiceBase(ILogger logger, IUnitOfWork unitOfWork)
    {
        this.Logger = logger;
        this.UnitOfWork = unitOfWork;
    }

    protected IRepository<TEntity> Repository =>
        this._repository ??= this.UnitOfWork.GetRepository<TEntity>();
}