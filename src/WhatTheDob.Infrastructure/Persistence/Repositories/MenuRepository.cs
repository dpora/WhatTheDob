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
            foreach (var menu in menus)
            {
                await UpsertMenuAsync(menu, cancellationToken).ConfigureAwait(false);
            }
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

        private async Task UpsertMenuAsync(DomainMenu menu, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var meal = await GetOrCreateMealAsync(menu.Meal, cancellationToken).ConfigureAwait(false);

            var menuEntity = await _dbContext.Menus
                .AsTracking()
                .FirstOrDefaultAsync(m => m.Date == menu.Date && m.MealId == meal.Id && m.CampusId == menu.CampusId, cancellationToken)
                .ConfigureAwait(false);

            if (menuEntity == null)
            {
                menuEntity = new PersistenceMenu
                {
                    Date = menu.Date,
                    MealId = meal.Id,
                    CampusId = menu.CampusId
                };

                _dbContext.Menus.Add(menuEntity);
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var existingMappings = _dbContext.MenuMappings.Where(mapping => mapping.MenuId == menuEntity.Id);
                _dbContext.MenuMappings.RemoveRange(existingMappings);
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            foreach (var item in menu.Items)
            {
                var category = await GetOrCreateCategoryAsync(item.Category, cancellationToken).ConfigureAwait(false);

                var menuItemEntity = await _dbContext.MenuItems
                    .AsTracking()
                    .FirstOrDefaultAsync(mi => mi.Value == item.Value && mi.CategoryId == category.Id, cancellationToken)
                    .ConfigureAwait(false);

                var tags = item.Tags != null && item.Tags.Count > 0 ? string.Join(",", item.Tags) : null;

                var itemRating = await GetOrCreateItemRatingAsync(item.Value, cancellationToken).ConfigureAwait(false);

                if (menuItemEntity == null)
                {
                    menuItemEntity = new PersistenceMenuItem
                    {
                        Value = item.Value,
                        Tags = tags,
                        CategoryId = category.Id,
                        ItemRatingId = itemRating.Id
                    };

                    _dbContext.MenuItems.Add(menuItemEntity);
                    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    menuItemEntity.Tags = tags;
                    menuItemEntity.CategoryId = category.Id;
                    menuItemEntity.ItemRatingId = itemRating.Id;
                }

                _dbContext.MenuMappings.Add(new PersistenceMenuMapping
                {
                    MenuId = menuEntity.Id,
                    MenuItemId = menuItemEntity.Id
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
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
