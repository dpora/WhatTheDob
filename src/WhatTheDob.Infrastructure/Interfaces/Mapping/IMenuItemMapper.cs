using System.Collections.Generic;
using WhatTheDob.Domain.Entities;

namespace WhatTheDob.Infrastructure.Interfaces.Mapping
{
     /// <summary>
     /// Parses HTML into MenuItem entities. Implemented by Infrastructure.
     /// </summary>
    public interface IMenuItemMapper
    {
        List<MenuItem> ParseMenuItems(string html);
    }
}
