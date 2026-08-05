using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AcgFotos.Base.SqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class MarcaAguaConfigurable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "opcionespublicacionid",
                table: "fot_eventos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "perfilmarcaaguaid",
                table: "fot_eventos",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "fot_opcionespublicacion",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    esdefault = table.Column<bool>(type: "boolean", nullable: false),
                    ladomayorpreview = table.Column<int>(type: "integer", nullable: false),
                    ladomayorthumb = table.Column<int>(type: "integer", nullable: false),
                    calidad = table.Column<int>(type: "integer", nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fot_opcionespublicacion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fot_perfilesmarcaagua",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    esdefault = table.Column<bool>(type: "boolean", nullable: false),
                    marcarthumb = table.Column<bool>(type: "boolean", nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fot_perfilesmarcaagua", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fot_capasmarcaagua",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    perfilmarcaaguaid = table.Column<long>(type: "bigint", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    storagekey = table.Column<Guid>(type: "uuid", nullable: false),
                    anchopx = table.Column<int>(type: "integer", nullable: false),
                    altopx = table.Column<int>(type: "integer", nullable: false),
                    modocolocacion = table.Column<int>(type: "integer", nullable: false),
                    posicion = table.Column<int>(type: "integer", nullable: true),
                    escalaporcentaje = table.Column<float>(type: "real", nullable: false),
                    margenporcentaje = table.Column<float>(type: "real", nullable: false),
                    angulogrados = table.Column<float>(type: "real", nullable: false),
                    opacidad = table.Column<float>(type: "real", nullable: false),
                    modofusion = table.Column<int>(type: "integer", nullable: false),
                    tenantid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fot_capasmarcaagua", x => x.id);
                    table.ForeignKey(
                        name: "fk_fot_capasmarcaagua_fot_perfilesmarcaagua_perfilmarcaaguaid",
                        column: x => x.perfilmarcaaguaid,
                        principalTable: "fot_perfilesmarcaagua",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fot_eventos_opcionespublicacionid",
                table: "fot_eventos",
                column: "opcionespublicacionid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_eventos_perfilmarcaaguaid",
                table: "fot_eventos",
                column: "perfilmarcaaguaid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_capasmarcaagua_perfilmarcaaguaid",
                table: "fot_capasmarcaagua",
                column: "perfilmarcaaguaid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_capasmarcaagua_storagekey",
                table: "fot_capasmarcaagua",
                column: "storagekey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fot_capasmarcaagua_tenantid",
                table: "fot_capasmarcaagua",
                column: "tenantid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_opcionespublicacion_tenantid",
                table: "fot_opcionespublicacion",
                column: "tenantid");

            migrationBuilder.CreateIndex(
                name: "ix_fot_perfilesmarcaagua_tenantid",
                table: "fot_perfilesmarcaagua",
                column: "tenantid");

            migrationBuilder.AddForeignKey(
                name: "fk_fot_eventos_fot_opcionespublicacion_opcionespublicacionid",
                table: "fot_eventos",
                column: "opcionespublicacionid",
                principalTable: "fot_opcionespublicacion",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fot_eventos_fot_perfilesmarcaagua_perfilmarcaaguaid",
                table: "fot_eventos",
                column: "perfilmarcaaguaid",
                principalTable: "fot_perfilesmarcaagua",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_fot_eventos_fot_opcionespublicacion_opcionespublicacionid",
                table: "fot_eventos");

            migrationBuilder.DropForeignKey(
                name: "fk_fot_eventos_fot_perfilesmarcaagua_perfilmarcaaguaid",
                table: "fot_eventos");

            migrationBuilder.DropTable(
                name: "fot_capasmarcaagua");

            migrationBuilder.DropTable(
                name: "fot_opcionespublicacion");

            migrationBuilder.DropTable(
                name: "fot_perfilesmarcaagua");

            migrationBuilder.DropIndex(
                name: "ix_fot_eventos_opcionespublicacionid",
                table: "fot_eventos");

            migrationBuilder.DropIndex(
                name: "ix_fot_eventos_perfilmarcaaguaid",
                table: "fot_eventos");

            migrationBuilder.DropColumn(
                name: "opcionespublicacionid",
                table: "fot_eventos");

            migrationBuilder.DropColumn(
                name: "perfilmarcaaguaid",
                table: "fot_eventos");
        }
    }
}
