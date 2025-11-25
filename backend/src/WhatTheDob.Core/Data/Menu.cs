using System;
using System.Collections.Generic;

namespace WhatTheDob.Core.Data;

public partial class Menu
{
    public int Id { get; set; }

    public string Date { get; set; } = null!;

    public int MealId { get; set; }

    public int CampusId { get; set; }

    public virtual Campus Campus { get; set; } = null!;

    public virtual Meal Meal { get; set; } = null!;
}
