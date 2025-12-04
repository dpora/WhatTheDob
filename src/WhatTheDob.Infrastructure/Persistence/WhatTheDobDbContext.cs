using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WhatTheDob.Infrastructure.Persistence.Models;

namespace WhatTheDob.Infrastructure.Persistence;

public partial class WhatTheDobDbContext : DbContext
{
    public WhatTheDobDbContext()
    {
    }

    public WhatTheDobDbContext(DbContextOptions<WhatTheDobDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Campus> Campuses { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Meal> Meals { get; set; }

    public virtual DbSet<Menu> Menus { get; set; }

    public virtual DbSet<MenuItem> MenuItems { get; set; }

    public virtual DbSet<MenuMapping> MenuMappings { get; set; }

    public virtual DbSet<ItemRating> ItemRatings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Campus>(entity =>
        {
            entity.ToTable("Campus");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Category");

            entity.Property(e => e.Id).HasColumnName("ID");
        });

        modelBuilder.Entity<Meal>(entity =>
        {
            entity.ToTable("Meal");

            entity.Property(e => e.Id).HasColumnName("ID");
        });

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.ToTable("Menu");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CampusId).HasColumnName("CampusID");
            entity.Property(e => e.MealId).HasColumnName("MealID");

            entity.HasOne(d => d.Campus).WithMany(p => p.Menus)
                .HasForeignKey(d => d.CampusId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Meal).WithMany(p => p.Menus)
                .HasForeignKey(d => d.MealId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.ToTable("MenuItem");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.ItemRatingId).HasColumnName("ItemRatingID");

            entity.HasOne(d => d.Category).WithMany(p => p.MenuItems)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ItemRating).WithMany(p => p.MenuItems)
                .HasForeignKey(d => d.ItemRatingId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<MenuMapping>(entity =>
        {
            entity.ToTable("MenuMapping");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.MenuId).HasColumnName("MenuID");
            entity.Property(e => e.MenuItemId).HasColumnName("MenuItemID");

            entity.HasOne(d => d.Menu).WithMany()
                .HasForeignKey(d => d.MenuId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.MenuItem).WithMany()
                .HasForeignKey(d => d.MenuItemId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ItemRating>(entity =>
        {
            entity.ToTable("ItemRating");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Value).HasColumnName("Value");
            entity.Property(e => e.TotalRating).HasColumnName("TotalRating");
            entity.Property(e => e.RatingCount).HasColumnName("RatingCount");
        });


        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
