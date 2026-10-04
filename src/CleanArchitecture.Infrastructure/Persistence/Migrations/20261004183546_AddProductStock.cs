using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds <c>products.stock_quantity</c> (existing rows start at zero) and a check constraint that keeps it
/// non-negative, a database-level backstop for the aggregate's own invariant.
/// </summary>
public partial class AddProductStock : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "stock_quantity",
            table: "products",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddCheckConstraint(
            name: "ck_products_stock_quantity_non_negative",
            table: "products",
            sql: "stock_quantity >= 0");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_products_stock_quantity_non_negative",
            table: "products");

        migrationBuilder.DropColumn(
            name: "stock_quantity",
            table: "products");
    }
}
