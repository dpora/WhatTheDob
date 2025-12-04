using System;
using System.Collections.Generic;

namespace WhatTheDob.Infrastructure.Persistence.Models;

public partial class Meal
{
    public int Id { get; set; }

    public string Value { get; set; } = null!;

    public int Disabled { get; set; }

    public virtual ICollection<Menu> Menus { get; set; } = new List<Menu>();
}
