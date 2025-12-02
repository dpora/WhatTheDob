using System;
using System.Collections.Generic;

namespace WhatTheDob.Domain.Data;

public partial class MenuItem
{
    public int Id { get; set; }

    public string Value { get; set; } = null!;

    public string? Tags { get; set; }

    public int RatingTotal { get; set; }

    public int RatingCount { get; set; }
    
    public virtual ICollection<ItemCategoryMapping> ItemCategoryMappings { get; set; } = new List<ItemCategoryMapping>();

}
