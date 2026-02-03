using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WhatTheDob.Infrastructure.Interfaces.Persistence;

using DomainMenu = WhatTheDob.Domain.Entities.Menu;
using DomainMenuItem = WhatTheDob.Domain.Entities.MenuItem;

using PersistenceContext = WhatTheDob.Infrastructure.Persistence.WhatTheDobDbContext;
using PersistenceCategory = WhatTheDob.Infrastructure.Persistence.Models.Category;
using PersistenceCampus = WhatTheDob.Infrastructure.Persistence.Models.Campus;
using PersistenceItemRating = WhatTheDob.Infrastructure.Persistence.Models.ItemRating;
using PersistenceUserRating = WhatTheDob.Infrastructure.Persistence.Models.UserRating;
using PersistenceMeal = WhatTheDob.Infrastructure.Persistence.Models.Meal;
using PersistenceMenu = WhatTheDob.Infrastructure.Persistence.Models.Menu;
using PersistenceMenuItem = WhatTheDob.Infrastructure.Persistence.Models.MenuItem;
using PersistenceMenuMapping = WhatTheDob.Infrastructure.Persistence.Models.MenuMapping;

namespace WhatTheDob.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Persists menu aggregates using Entity Framework Core.
    /// </summary>
    public class MenuRepository : IMenuRepository
    {
        private readonly PersistenceContext _dbContext;

        public MenuRepository(PersistenceContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task UpsertCampusesAsync(IDictionary<int, string> campuses, CancellationToken cancellationToken = default)
        {
            if (campuses == null || campuses.Count < 1)
            {
                return;
            }

            foreach (var campus in campuses)
            {
                var campusEntity = await _dbContext.Campuses
                    .AsTracking()
                    .FirstOrDefaultAsync(c => c.Id == campus.Key, cancellationToken)
                    .ConfigureAwait(false);

                if (campusEntity == null)
                {
                    campusEntity = new PersistenceCampus
                    {
                        Id = campus.Key,
                        Value = campus.Value,
                        Disabled = 0
                    };

                    _dbContext.Campuses.Add(campusEntity);
                }
                else
                {
                    campusEntity.Value = campus.Value;
                    campusEntity.Disabled = 0;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<PersistenceCampus>> GetCampusesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Campuses
                .AsNoTracking()
                .Where(c => c.Disabled == 0)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task UpsertMealsAsync(IEnumerable<string> meals, CancellationToken cancellationToken = default)
        {
            if (meals == null)
            {
                return;
            }

            var distinctMeals = new HashSet<string>(meals, StringComparer.OrdinalIgnoreCase);

            foreach (var mealName in distinctMeals)
            {
                var normalized = mealName?.Trim();

                if (string.IsNullOrEmpty(normalized))
                {
                    continue;
                }

                var mealEntity = await _dbContext.Meals
                    .AsTracking()
                    .FirstOrDefaultAsync(m => m.Value == normalized, cancellationToken)
                    .ConfigureAwait(false);

                if (mealEntity == null)
                {
                    mealEntity = new PersistenceMeal
                    {
                        Value = normalized,
                        Disabled = 0
                    };

                    _dbContext.Meals.Add(mealEntity);
                }
                else
                {
                    mealEntity.Disabled = 0;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<PersistenceMeal>> GetMealsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Meals
                .AsNoTracking()
                .Where(m => m.Disabled == 0)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task UpsertMenusAsync(IEnumerable<DomainMenu> menus, CancellationToken cancellationToken = default)
        {
            var menuList = menus.ToList();

            if (menuList.Count == 0) return;

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var mealNames = menuList
                        .Select(m => m.Meal)
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    var existingMeals = await _dbContext.Meals
                        .Where(m => mealNames.Contains(m.Value))
                        .ToDictionaryAsync(m => m.Value, StringComparer.OrdinalIgnoreCase, cancellationToken);


                    // Bulk upsert categories
                    var categoryNames = menuList
                        .SelectMany(m => m.Items)
                        .Select(i => i.Category?.Trim() ?? "Uncategorized")
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    var existingCategories = await _dbContext.Categories
                        .Where(c => categoryNames.Contains(c.Value))
                        .ToDictionaryAsync(c => c.Value, StringComparer.OrdinalIgnoreCase, cancellationToken);

                    foreach (var name in categoryNames)
                    {
                        if (!existingCategories.ContainsKey(name))
                        {
                            var newCat = new PersistenceCategory { Value = name };
                            _dbContext.Categories.Add(newCat);
                            existingCategories[name] = newCat;
                        }
                    }
                    await _dbContext.SaveChangesAsync(cancellationToken);


                    // Bulk upsert item ratings
                    var menuItemNames = menuList
                        .SelectMany(m => m.Items)
                        .Select(i => i.Value?.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    // Chunking to avoid SQL parameter limit (2100) if many items
                    var existingItemRatings = new Dictionary<string, PersistenceItemRating>(StringComparer.OrdinalIgnoreCase);
                    foreach (var chunk in menuItemNames.Chunk(2000))
                    {
                        var chunkRatings = await _dbContext.ItemRatings
                            .Where(r => chunk.Contains(r.Value))
                            .ToListAsync(cancellationToken);

                        foreach (var r in chunkRatings) existingItemRatings[r.Value] = r;
                    }

                    foreach (var name in menuItemNames)
                    {
                        if (!existingCategories.ContainsKey(name))
                        {
                            var newRat = new PersistenceItemRating { Value = name, TotalRating = 0, RatingCount = 0 };
                            _dbContext.ItemRatings.Add(newRat);
                            existingItemRatings[name] = newRat;
                        }
                    }
                    await _dbContext.SaveChangesAsync(cancellationToken);


                    // Bulk upsert menu items now
                    var existingMenuItems = new List<PersistenceMenuItem>();
                    foreach (var chunk in menuItemNames.Chunk(2000))
                    {
                        existingMenuItems.AddRange(await _dbContext.MenuItems
                            .Where(mi => chunk.Contains(mi.Value))
                            .ToListAsync(cancellationToken));
                    }

                    // Key = Combined string or Tuple for lookup
                    var menuItemMap = existingMenuItems
                        .GroupBy(mi => (mi.Value.Trim().ToLower(), mi.CategoryId))
                        .ToDictionary(g => g.Key, g => g.First());
                    
                    // Prepare lookup to ensuring no duplicates in this batch
                    foreach (var domainMenu in menuList)
                    {
                        foreach (var domainItem in domainMenu.Items)
                        {
                            var val = domainItem.Value.Trim();
                            var cat = domainItem.Category?.Trim() ?? "Uncategorized";
                            var catId = existingCategories[cat].Id;
                            var ratingId = existingItemRatings[val].Id;
                            var tags = domainItem.Tags?.Any() == true ? string.Join(",", domainItem.Tags) : null;

                            var key = (val.ToLower(), catId);

                            if (menuItemMap.TryGetValue(key, out var existingItem))
                            {
                                // Update logic if needed (e.g. tags changed)
                                if (existingItem.Tags != tags) existingItem.Tags = tags;
                                if (existingItem.ItemRatingId != ratingId) existingItem.ItemRatingId = ratingId;
                            }
                            else
                            {
                                var newItem = new PersistenceMenuItem
                                {
                                    Value = val,
                                    CategoryId = catId,
                                    ItemRatingId = ratingId,
                                    Tags = tags
                                };
                                _dbContext.MenuItems.Add(newItem);
                                menuItemMap[key] = newItem; // Add to dictionary for subsequent lookups in this loop
                            }
                        }
                    }
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    // Bulk upsert Menus
                    var dates = menuList.Select(m => m.Date).Distinct().ToList();
                    var campusIds = menuList.Select(m => m.CampusId).Distinct().ToList();

                    // Load existing menus matching date/campus context
                    var existingMenus = await _dbContext.Menus
                        .Where(m => dates.Contains(m.Date) && campusIds.Contains(m.CampusId))
                        .ToListAsync(cancellationToken);

                    var menuEntityMap = existingMenus
                        .GroupBy(m => (m.Date, m.CampusId, m.MealId))
                        .ToDictionary(g => g.Key, g => g.First());

                    var menusToProcess = new List<(PersistenceMenu PMenu, DomainMenu DMenu)>();

                    foreach (var dMenu in menuList)
                    {
                        var mealId = existingMeals[dMenu.Meal.Trim()].Id;
                        var key = (dMenu.Date, dMenu.CampusId, mealId);

                        if (!menuEntityMap.TryGetValue(key, out var pMenu))
                        {
                            pMenu = new PersistenceMenu
                            {
                                Date = dMenu.Date,
                                CampusId = dMenu.CampusId,
                                MealId = mealId
                            };
                            _dbContext.Menus.Add(pMenu);
                            menuEntityMap[key] = pMenu;
                        }
                        menusToProcess.Add((pMenu, dMenu));
                    }
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    // Remove old mappings
                    var menuIds = menusToProcess.Select(x => x.PMenu.Id).ToList();
                    await _dbContext.MenuMappings.Where(mm => menuIds.Contains(mm.MenuId)).ExecuteDeleteAsync();

                    // Add new mappings
                    foreach (var (pMenu, dMenu) in menusToProcess)
                    {
                        foreach (var item in dMenu.Items)
                        {
                            var catId = existingCategories[item.Category?.Trim() ?? "Uncategorized"].Id;
                            var key = (item.Value.Trim().ToLowerInvariant(), catId);
                            var pItem = menuItemMap[key];

                            _dbContext.MenuMappings.Add(new PersistenceMenuMapping
                            {
                                MenuId = pMenu.Id,
                                MenuItemId = pItem.Id
                            });
                        }
                    }
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }

        public async Task<PersistenceMenu> GetMenuAsync(string date, int campusId, int mealId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Menus
                .AsNoTracking()
                .Include(m => m.Meal)
                .Include(m => m.Campus)
                .FirstOrDefaultAsync(m => m.Date == date && m.CampusId == campusId && m.MealId == mealId, cancellationToken)
                .ConfigureAwait(false);
        }
        
        public async Task<IEnumerable<PersistenceMenuMapping>> GetMenuMappingsAsync(int menuId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.MenuMappings
                .AsNoTracking()
                .Where(mm => mm.MenuId == menuId)
                .Include(mm => mm.Menu)
                    .ThenInclude(m => m.Meal)
                .Include(mm => mm.Menu)
                    .ThenInclude(m => m.Campus)
                .Include(mm => mm.MenuItem)
                    .ThenInclude(mi => mi.Category)
                .Include(mm => mm.MenuItem)
                    .ThenInclude(mi => mi.ItemRating)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<PersistenceMenuMapping>> GetMenuMappingsAsync(string date, int campusId, int mealId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.MenuMappings
                .AsNoTracking()
                .Where(mm => mm.Menu.CampusId == campusId &&
                             mm.Menu.MealId == mealId &&
                             mm.Menu.Date == date)
                .Include(mm => mm.Menu)
                    .ThenInclude(m => m.Meal)
                .Include(mm => mm.Menu)
                    .ThenInclude(m => m.Campus)
                .Include(mm => mm.MenuItem)
                    .ThenInclude(mi => mi.Category)
                .Include(mm => mm.MenuItem)
                    .ThenInclude(mi => mi.ItemRating)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task UpsertUserRatingAsync(string sessionId, string itemValue, int rating,
            CancellationToken cancellationToken = default)
        {
            var itemRating = await _dbContext.ItemRatings
                .AsTracking()
                .FirstOrDefaultAsync(r => r.Value == itemValue.Trim(), cancellationToken)
                .ConfigureAwait(false);

            // If no item rating found, check to see if the menu item exists, and if so create the item rating
            // Most likely will not happen as we are creating item ratings when creating menu items
            if (itemRating == null)
            {
                var menuItem = await _dbContext.MenuItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Value == itemValue.Trim(), cancellationToken)
                    .ConfigureAwait(false);

                if (menuItem == null)
                {
                    throw new ArgumentException("No menu items exist for specified item value");
                }

                var itemRatingEntity = new PersistenceItemRating
                {
                    Value = itemValue.Trim(),
                    TotalRating = rating,
                    RatingCount = 1
                };

                _dbContext.ItemRatings.Add(itemRatingEntity);

                // May lead to race condition if two users rate the same item simultaneously
                // However, the likelihood is low and the impact is minimal (slightly inaccurate rating)
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                itemRating = itemRatingEntity;
            }

            var userRating = await _dbContext.UserRatings
                .AsTracking()
                .FirstOrDefaultAsync(r => r.ItemRatingId == itemRating.Id && r.SessionId == sessionId, cancellationToken)
                .ConfigureAwait(false);
            // If no existing user rating, create one
            if (userRating == null)
            {
                var userRatingEntity = new PersistenceUserRating
                {
                    ItemRatingId = itemRating.Id,
                    RatingValue = rating,
                    SessionId = sessionId,
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    UpdatedAt = null
                };
                
                _dbContext.UserRatings.Add(userRatingEntity);
                
                itemRating.TotalRating += rating;
                itemRating.RatingCount += 1;
            }
            else // Update existing user rating and ItemRating aggregate
            {
                itemRating.TotalRating += (rating - userRating.RatingValue);

                userRating.RatingValue = rating;
                userRating.UpdatedAt = DateTime.UtcNow.ToString("o");
            }
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<PersistenceMeal> GetOrCreateMealAsync(string mealName, CancellationToken cancellationToken)
        {
            var normalized = mealName.Trim();

            var meal = await _dbContext.Meals
                .AsTracking()
                .FirstOrDefaultAsync(m => m.Value == normalized, cancellationToken)
                .ConfigureAwait(false);

            if (meal != null)
            {
                return meal;
            }

            meal = new PersistenceMeal
            {
                Value = normalized,
                Disabled = 0
            };

            _dbContext.Meals.Add(meal);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return meal;
        }

        private async Task<PersistenceCategory> GetOrCreateCategoryAsync(string categoryName, CancellationToken cancellationToken)
        {
            var normalized = string.IsNullOrWhiteSpace(categoryName) ? "Uncategorized" : categoryName.Trim();

            var category = await _dbContext.Categories
                .AsTracking()
                .FirstOrDefaultAsync(c => c.Value == normalized, cancellationToken)
                .ConfigureAwait(false);

            if (category != null)
            {
                return category;
            }

            category = new PersistenceCategory
            {
                Value = normalized
            };

            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return category;
        }
       
        private async Task<PersistenceItemRating> GetOrCreateItemRatingAsync(string itemValue, CancellationToken cancellationToken)
        {
            var normalized = itemValue.Trim();

            var itemRating = await _dbContext.ItemRatings
                .AsTracking()
                .FirstOrDefaultAsync(r => r.Value == normalized, cancellationToken)
                .ConfigureAwait(false);

            if (itemRating != null)
            {
                return itemRating;
            }

            itemRating = new PersistenceItemRating
            {
                Value = normalized,
                TotalRating = 0,
                RatingCount = 0
            };

            _dbContext.ItemRatings.Add(itemRating);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return itemRating;
        }
    }
}
