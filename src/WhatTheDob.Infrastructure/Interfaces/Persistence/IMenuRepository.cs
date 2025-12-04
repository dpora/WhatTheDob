using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WhatTheDob.Domain.Entities;
using DomainMenu = WhatTheDob.Domain.Entities.Menu;
using PersistenceContext = WhatTheDob.Infrastructure.Persistence.WhatTheDobDbContext;
using PersistenceCategory = WhatTheDob.Infrastructure.Persistence.Models.Category;
using PersistenceCampus = WhatTheDob.Infrastructure.Persistence.Models.Campus;
using PersistenceItemRating = WhatTheDob.Infrastructure.Persistence.Models.ItemRating;
using PersistenceMeal = WhatTheDob.Infrastructure.Persistence.Models.Meal;
using PersistenceMenu = WhatTheDob.Infrastructure.Persistence.Models.Menu;
using PersistenceMenuItem = WhatTheDob.Infrastructure.Persistence.Models.MenuItem;
using PersistenceMenuMapping = WhatTheDob.Infrastructure.Persistence.Models.MenuMapping;


namespace WhatTheDob.Infrastructure.Interfaces.Persistence
{
    /// <summary>
    /// Abstraction for persisting menu aggregates.
    /// </summary>
    public interface IMenuRepository
    {
        Task UpsertFiltersAsync(IDictionary<int, string> campuses, IEnumerable<string> meals, CancellationToken cancellationToken = default);
        Task UpsertMenusAsync(IEnumerable<Menu> menus, CancellationToken cancellationToken = default);
        Task UpdateItemRating(string itemValue, int rating, CancellationToken cancellationToken = default);
        Task<PersistenceMenu> GetMenuAsync(string date, int campusId, int mealId, CancellationToken cancellationToken = default);
        Task<IEnumerable<PersistenceMenuMapping>> GetMenuMappingsAsync(int menuId, CancellationToken cancellationToken = default);
        Task<IEnumerable<PersistenceMenuMapping>> GetMenuMappingsAsync(string date, int campusId, int mealId, CancellationToken cancellationToken = default);
    }
}
