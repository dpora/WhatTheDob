using System;
using System.Collections.Generic;

namespace WhatTheDob.Infrastructure.Persistence.Models;

public partial class MenuItem
{
    public int Id { get; set; }

    public string Value { get; set; } = null!;

    public string? Tags { get; set; }

    public int CategoryId { get; set; }

    public int ItemRatingId { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ItemRating ItemRating { get; set; } = null!;

}
