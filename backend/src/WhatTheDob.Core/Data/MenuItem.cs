using System;
using System.Collections.Generic;

namespace WhatTheDob.Core.Data;

public partial class MenuItem
{
    public int Id { get; set; }

    public string Value { get; set; } = null!;

    public string? Tags { get; set; }

    public int RatingTotal { get; set; }

    public int RatingCount { get; set; }

    public int CategoryId { get; set; }

    public virtual Category Category { get; set; } = null!;
}
