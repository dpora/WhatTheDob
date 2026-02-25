using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WhatTheDob.Infrastructure.Persistence;

namespace WhatTheDob.Infrastructure.Tests.Support;

public sealed class SqliteInMemoryContext : IDisposable
{
    private readonly SqliteConnection _connection;
    public WhatTheDobDbContext Context { get; }

    public SqliteInMemoryContext()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<WhatTheDobDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new WhatTheDobDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
