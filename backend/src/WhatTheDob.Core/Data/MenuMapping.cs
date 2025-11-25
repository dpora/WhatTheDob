using System;
using System.Collections.Generic;

namespace WhatTheDob.Core.Data;

public partial class MenuMapping
{
    public int Id { get; set; }

    public int MenuId { get; set; }

    public int MenuItemId { get; set; }

    public virtual Menu Menu { get; set; } = null!;

    public virtual MenuItem MenuItem { get; set; } = null!;
}
