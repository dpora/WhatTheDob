using System.Collections.Generic;

namespace WhatTheDob.Infrastructure.Interfaces.Mapping
{
    /// <summary>
    /// Parses filter options (campus, meals) from HTML. Implemented by Infrastructure.
    /// </summary>
    public interface IMenuFilterMapper
    {
        Dictionary<string, string> ParseCampusOptions(string html);
        List<string> ParseMealOptions(string html);
    }
}
