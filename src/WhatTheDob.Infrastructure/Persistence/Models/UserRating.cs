using System;
using System.Collections.Generic;

namespace WhatTheDob.Infrastructure.Persistence.Models;

public partial class UserRating
{
    public int Id { get; set; }

    public int RatingValue { get; set; }

    public string SessionId { get; set; } = null!;

    public string CreatedAt { get; set; } = null!;

    public string? UpdatedAt { get; set; }

    public int ItemRatingId { get; set; }

    public virtual ItemRating ItemRating { get; set; } = null!;
}