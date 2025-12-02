using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WhatTheDob.Application.Interfaces.Persistence;
using DomainMenu = WhatTheDob.Domain.Entities.Menu;
using PersistenceCategory = WhatTheDob.Domain.Data.Category;
using PersistenceItemCategoryMapping = WhatTheDob.Domain.Data.ItemCategoryMapping;
using PersistenceCampus = WhatTheDob.Domain.Data.Campus;
using PersistenceContext = WhatTheDob.Domain.Data.WhatTheDobDbContext;
using PersistenceMeal = WhatTheDob.Domain.Data.Meal;
using PersistenceMenu = WhatTheDob.Domain.Data.Menu;
using PersistenceMenuItem = WhatTheDob.Domain.Data.MenuItem;
using PersistenceMenuMapping = WhatTheDob.Domain.Data.MenuMapping;

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

        public async Task UpsertFiltersAsync(IDictionary<int, string> campuses, IEnumerable<string> meals, CancellationToken cancellationToken = default)
        {
            if (campuses != null)
            {
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
            }

            if (meals != null)
            {
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
            }

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task UpsertMenusAsync(IEnumerable<DomainMenu> menus, CancellationToken cancellationToken = default)
        {
            foreach (var menu in menus)
            {
                await UpsertMenuAsync(menu, cancellationToken).ConfigureAwait(false);
            }
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
                    .FirstOrDefaultAsync(mi => mi.Value == item.Value, cancellationToken)
                    .ConfigureAwait(false);

                var tags = item.Tags != null && item.Tags.Count > 0 ? string.Join(",", item.Tags) : null;

                if (menuItemEntity == null)
                {
                    menuItemEntity = new PersistenceMenuItem
                    {
                        Value = item.Value,
                        Tags = tags,
                        RatingCount = 0,
                        RatingTotal = 0
                    };

                    _dbContext.MenuItems.Add(menuItemEntity);
                    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    menuItemEntity.Tags = tags;
                }

                await GetOrCreateItemCategoryMappingAsync(menuItemEntity.Id, category.Id, cancellationToken);

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

        private async Task<PersistenceItemCategoryMapping> GetOrCreateItemCategoryMappingAsync(int menuItemId, int categoryId, CancellationToken cancellationToken)
        {

            var itemCategoryMapping = await _dbContext.ItemCategoryMappings
                    .AsTracking()
                    .FirstOrDefaultAsync(icm => icm.MenuItemId == menuItemId && icm.CategoryId == categoryId, cancellationToken)
                    .ConfigureAwait(false);

            if (itemCategoryMapping != null)
            {
                return itemCategoryMapping;
            }

            itemCategoryMapping = new PersistenceItemCategoryMapping
            {
                MenuItemId = menuItemId,
                CategoryId = categoryId
            };

            _dbContext.ItemCategoryMappings.Add(itemCategoryMapping);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return itemCategoryMapping;
        }
    }
}
