using System;
using System.Collections.Generic;

namespace WhatTheDob.Core.Data;

public partial class Category
{
    public int Id { get; set; }

    public string Value { get; set; } = null!;

    public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
