using System;
using System.Collections.Generic;

namespace WhatTheDob.Domain.Data;

public partial class Category
{
    public int Id { get; set; }

    public string Value { get; set; } = null!;

    public virtual ICollection<ItemCategoryMapping> ItemCategoryMappings { get; set; } = new List<ItemCategoryMapping>();
}
