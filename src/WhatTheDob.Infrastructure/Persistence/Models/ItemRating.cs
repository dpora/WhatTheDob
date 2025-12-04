using System;
using System.Collections.Generic;

namespace WhatTheDob.Infrastructure.Persistence.Models;

public partial class ItemRating
{
    public int Id { get; set; }

    public string Value { get; set; } = null!;

    public int TotalRating { get; set; }

    public int RatingCount { get; set; }

    public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}