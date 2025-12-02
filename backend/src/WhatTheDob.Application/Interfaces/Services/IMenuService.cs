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
        Task<List<Menu>> GetMenuPagesAsync();
    }
}
