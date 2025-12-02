using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WhatTheDob.Domain.Entities;

namespace WhatTheDob.Application.Interfaces.Persistence
{
    /// <summary>
    /// Abstraction for persisting menu aggregates.
    /// </summary>
    public interface IMenuRepository
    {
        Task UpsertFiltersAsync(IDictionary<int, string> campuses, IEnumerable<string> meals, CancellationToken cancellationToken = default);
        Task UpsertMenusAsync(IEnumerable<Menu> menus, CancellationToken cancellationToken = default);
    }
}
