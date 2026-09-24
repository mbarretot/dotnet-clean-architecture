using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Persistence.Migrations;

/// <summary>
/// Replaces <c>is_active</c> with soft-delete columns. Hand-edited: the scaffolder emitted a rename of
/// <c>is_active</c> to <c>is_deleted</c>, which would invert every row. Instead the new flag is added, back-filled
/// from the old one (<c>is_deleted = NOT is_active</c>), and only then is the old column dropped.
/// </summary>
public partial class SoftDeleteProducts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "is_deleted",
            table: "products",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "deleted_on_utc",
            table: "products",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "deleted_by",
            table: "products",
            type: "character varying(256)",
            maxLength: 256,
            nullable: true);

        // Previously deactivated products become soft-deleted; when and by whom was never recorded, so those stay null.
        migrationBuilder.Sql("UPDATE products SET is_deleted = NOT is_active;");

        migrationBuilder.DropColumn(
            name: "is_active",
            table: "products");

        migrationBuilder.DropIndex(
            name: "ix_products_sku",
            table: "products");

        migrationBuilder.CreateIndex(
            name: "ix_products_sku",
            table: "products",
            column: "sku",
            unique: true,
            filter: "is_deleted = FALSE");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Fails if a deleted product's SKU was reused meanwhile: the unfiltered index cannot hold both rows.
        migrationBuilder.DropIndex(
            name: "ix_products_sku",
            table: "products");

        migrationBuilder.CreateIndex(
            name: "ix_products_sku",
            table: "products",
            column: "sku",
            unique: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_active",
            table: "products",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.Sql("UPDATE products SET is_active = NOT is_deleted;");

        migrationBuilder.DropColumn(
            name: "is_deleted",
            table: "products");

        migrationBuilder.DropColumn(
            name: "deleted_on_utc",
            table: "products");

        migrationBuilder.DropColumn(
            name: "deleted_by",
            table: "products");
    }
}
