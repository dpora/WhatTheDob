using System;
using System.Collections.Generic;

namespace WhatTheDob.Domain.Data;

public partial class ItemCategoryMapping
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public int MenuItemId { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual MenuItem MenuItem { get; set; } = null!;
}