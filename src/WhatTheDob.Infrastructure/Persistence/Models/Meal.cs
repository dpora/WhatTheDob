using System;
using System.Collections.Generic;

namespace WhatTheDob.Domain.Data;

public partial class Meal
{
    public int Id { get; set; }

    public string Value { get; set; } = null!;

    public int Disabled { get; set; }

    public virtual ICollection<Menu> Menus { get; set; } = new List<Menu>();
}
