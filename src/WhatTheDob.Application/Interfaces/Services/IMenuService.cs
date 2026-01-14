using System.Collections.Generic;
using System.Threading.Tasks;
using WhatTheDob.Application.DTOs;
using WhatTheDob.Domain.Entities;

namespace WhatTheDob.Application.Interfaces.Services
{
    /// <summary>
    /// Interface for menu-related operations implemented by the Infrastructure project.
    /// </summary>
    public interface IMenuService
    {
        Task<Menu> GetMenuAsync(string date, int campusId, int mealId);
        Task<List<Menu>> FetchMenusFromApiAsync();
        Task<IEnumerable<CampusDto>> GetCampusesAsync();
        Task<IEnumerable<MealDto>> GetMealsAsync();
    }
}
