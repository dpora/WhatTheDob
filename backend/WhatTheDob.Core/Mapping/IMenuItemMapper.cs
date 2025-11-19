using System.Collections.Generic;
using WhatTheDob.Core.Entities;

namespace WhatTheDob.Core.Mapping
{
 /// <summary>
 /// Parses HTML into MenuItem entities. Implemented by Infrastructure.
 /// </summary>
 public interface IMenuItemMapper
 {
 List<MenuItem> ParseMenuItems(string html);
 }
}
