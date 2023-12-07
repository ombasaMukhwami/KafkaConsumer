using KafkaConsumer.Data;
using KafkaConsumer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace KafkaConsumer.Services.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> Find(Expression<Func<T, bool>> predicate);
    Task<IEnumerable<T>> GetAll();
    Task<IEnumerable<T>> GetAll(Expression<Func<T, bool>> predicate);
    Task<T?> GetById(int id);
    void Update(T entity);
    void Add(T entity);
    Task AddRange(List<T> entity);
}

public interface IDeviceRepository : IRepository<Device> { }
public interface IPositionRepository : IRepository<Position> { }
public interface IUnitOfWork
{
    Task<int> CommitAsync();
    IDeviceRepository Devices { get; }
    IPositionRepository Positions { get; }
}
