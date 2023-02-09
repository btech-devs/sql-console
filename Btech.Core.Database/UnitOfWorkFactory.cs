using System;
using Btech.Core.Database.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Btech.Core.Database;

public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IServiceProvider _serviceProvider;

    public UnitOfWorkFactory(IServiceProvider serviceProvider) => this._serviceProvider = serviceProvider;

    public IUnitOfWork GetUnitOfWork() => this._serviceProvider.GetRequiredService<IUnitOfWork>();
}