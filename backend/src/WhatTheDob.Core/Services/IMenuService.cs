using System.Collections.Generic;
using System.Threading.Tasks;
using WhatTheDob.Core.Entities;

namespace WhatTheDob.Core.Services
{
    /// <summary>
    /// Interface for menu-related operations implemented by the Infrastructure project.
    /// </summary>
    public interface IMenuService
    {
        Task<List<Menu>> GetMenuPagesAsync();
    }
}
