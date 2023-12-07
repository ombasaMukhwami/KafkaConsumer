using KafkaConsumer.Data;
using KafkaConsumer.Models;
using KafkaConsumer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace KafkaConsumer.Services;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly SpeedLimiterDbContext _dbContext;
    private readonly DbSet<T> _table;
    public Repository(SpeedLimiterDbContext dbContext)
    {
        _dbContext = dbContext;
        _table = _dbContext.Set<T>();
    }

    public async Task<T?> Find(Expression<Func<T, bool>> predicate)
        => await _table.Where(predicate)
        .AsNoTracking()
        .FirstOrDefaultAsync();
    public async Task<IEnumerable<T>> GetAll()
        => await _table
        .AsNoTracking()
        .ToListAsync();
    public async Task<IEnumerable<T>> GetAll(Expression<Func<T, bool>> predicate)
            => await _table.Where(predicate)
            .AsNoTracking()
            .ToListAsync();
    public async Task<T?> GetById(int id) => await _table.FindAsync(id);

    public void Add(T entity)
    {
        _table.Add(entity);
    }
    public async Task AddRange(List<T> entity)
    {
        await _table.AddRangeAsync(entity);
    }
    public void Update(T entity)
    {
        // _table.Entry(entity).State = EntityState.Modified;
        _table.Update(entity);
    }
}

public class PositionRepository : Repository<Position>, IPositionRepository
{
    public PositionRepository(SpeedLimiterDbContext db) : base(db)
    {

    }
}
public class DeviceRepository : Repository<Device>, IDeviceRepository
{
    public DeviceRepository(SpeedLimiterDbContext db) : base(db)
    {

    }
}


public class UnitOfWork : IUnitOfWork, IDisposable
{
    private bool _disposed;
    public SpeedLimiterDbContext _dbContext { get; }
    private ILogger<UnitOfWork> _logger;
    public UnitOfWork(SpeedLimiterDbContext dbContext, ILoggerFactory logger)
    {
        _dbContext = dbContext;
        _logger = logger.CreateLogger<UnitOfWork>();
    }

    #region private fields
    private IDeviceRepository _Devices;
    private IPositionRepository _Positions;

    #endregion

    #region public fields
    public IDeviceRepository Devices => _Devices ?? new DeviceRepository(_dbContext);
    public IPositionRepository Positions => _Positions ?? new PositionRepository(_dbContext);

    #endregion
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        try
        {
            if (!_disposed)
                if (disposing)
                    _dbContext.Dispose();
            _disposed = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }
    public async Task<int> CommitAsync()
    {
        var result = await _dbContext.SaveChangesAsync();
        return result;
    }
}