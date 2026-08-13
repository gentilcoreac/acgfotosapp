using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcgFotos.Base.SqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class SeparacionMarcaAgua : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "separacionporcentaje",
                table: "fot_capasmarcaagua",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            // Hasta acá el paso del mosaico era 1.25x el ancho del tile, y el tile ocupa
            // escalaporcentaje del ancho de la foto: esa multiplicacion es la separacion sobre la foto
            // que reproduce exactamente la densidad que cada capa ya tenia. Sin esto, las capas
            // existentes quedarian en 0 y el mosaico no avanzaria.
            migrationBuilder.Sql(
                "UPDATE fot_capasmarcaagua SET separacionporcentaje = escalaporcentaje * 1.25;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "separacionporcentaje",
                table: "fot_capasmarcaagua");
        }
    }
}
