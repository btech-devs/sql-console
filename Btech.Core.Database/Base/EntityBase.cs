using Microsoft.EntityFrameworkCore;

namespace Btech.Core.Database.Base;

public abstract class EntityBase
{
    // public virtual void UpdateProperties<T>(T entity) where T : EntityBase
    // {
    //     if (entity != null)
    //     {
    //         Type thisType = this.GetType();
    //         Type entityType = entity.GetType();
    //
    //         if (thisType == entityType)
    //         {
    //             foreach (PropertyInfo property in entityType.GetProperties(BindingFlags.Public))
    //             {
    //                 object propertyValue = property.GetValue(entity);
    //
    //                 if (propertyValue != null)
    //                     property.SetValue(this, propertyValue);
    //             }
    //         }
    //     }
    // }

    public abstract void Setup(ModelBuilder modelBuilder);

    public virtual void BeforeSaveChanges(EntityState state)
    {
    }
}