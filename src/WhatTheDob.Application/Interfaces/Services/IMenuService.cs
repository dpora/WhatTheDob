using System.Collections.Generic;
using System.Threading.Tasks;
using WhatTheDob.Domain.Entities;

namespace WhatTheDob.Application.Interfaces.Services
{
    /// <summary>
    /// Interface for menu-related operations implemented by the Infrastructure project.
    /// </summary>
    public interface IMenuService
    {
        Task<Menu> GetMenuAsync(string date, int campusId, int mealId);
        // Fetch all menus from external API, will process a specific date if specified
        Task<List<Menu>> FetchMenusFromApiAsync(string date = "");
        Task<IEnumerable<Campus>> GetCampusesAsync();
        Task<IEnumerable<Meal>> GetMealsAsync();
        Task SubmitUserRatingAsync(string sessionId, string itemValue, int rating);
    }
}
