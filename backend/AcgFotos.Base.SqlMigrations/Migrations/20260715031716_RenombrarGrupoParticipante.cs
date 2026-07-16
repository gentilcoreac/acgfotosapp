using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcgFotos.Base.SqlMigrations.Migrations
{
    /// <summary>
    /// Rename del naming genérico (ADR-10): fot_Cursos → fot_Grupos, fot_Albumes → fot_Participantes
    /// (con NombreAlumno → Nombre), FKs CursoId/AlbumId → GrupoId/ParticipanteId y
    /// fot_Eventos.Colegio → LugarOrganizacion. Escrita a mano como renames (el scaffold proponía
    /// drop/create y perdía datos); PKs y FKs se recrean para que los nombres de constraint queden
    /// en la convención nueva.
    /// </summary>
    public partial class RenombrarGrupoParticipante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Soltar FKs y PKs que involucran a las tablas/columnas renombradas.
            migrationBuilder.DropForeignKey(
                name: "FK_fot_CodigosAcceso_fot_Albumes_AlbumId",
                table: "fot_CodigosAcceso");

            migrationBuilder.DropForeignKey(
                name: "FK_fot_Fotos_fot_Albumes_AlbumId",
                table: "fot_Fotos");

            migrationBuilder.DropForeignKey(
                name: "FK_fot_Fotos_fot_Cursos_CursoId",
                table: "fot_Fotos");

            migrationBuilder.DropForeignKey(
                name: "FK_fot_Pedidos_fot_Albumes_AlbumId",
                table: "fot_Pedidos");

            migrationBuilder.DropForeignKey(
                name: "FK_fot_Albumes_fot_Cursos_CursoId",
                table: "fot_Albumes");

            migrationBuilder.DropForeignKey(
                name: "FK_fot_Cursos_fot_Eventos_EventoId",
                table: "fot_Cursos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_fot_Albumes",
                table: "fot_Albumes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_fot_Cursos",
                table: "fot_Cursos");

            // 2) Renombrar tablas y columnas (los datos se conservan).
            migrationBuilder.RenameTable(
                name: "fot_Cursos",
                newName: "fot_Grupos");

            migrationBuilder.RenameTable(
                name: "fot_Albumes",
                newName: "fot_Participantes");

            migrationBuilder.RenameColumn(
                name: "CursoId",
                table: "fot_Participantes",
                newName: "GrupoId");

            migrationBuilder.RenameColumn(
                name: "NombreAlumno",
                table: "fot_Participantes",
                newName: "Nombre");

            migrationBuilder.RenameColumn(
                name: "CursoId",
                table: "fot_Fotos",
                newName: "GrupoId");

            migrationBuilder.RenameColumn(
                name: "AlbumId",
                table: "fot_Fotos",
                newName: "ParticipanteId");

            migrationBuilder.RenameColumn(
                name: "AlbumId",
                table: "fot_CodigosAcceso",
                newName: "ParticipanteId");

            migrationBuilder.RenameColumn(
                name: "AlbumId",
                table: "fot_Pedidos",
                newName: "ParticipanteId");

            migrationBuilder.RenameColumn(
                name: "Colegio",
                table: "fot_Eventos",
                newName: "LugarOrganizacion");

            // 3) Renombrar índices a la convención nueva.
            migrationBuilder.RenameIndex(
                name: "IX_fot_Cursos_EventoId",
                table: "fot_Grupos",
                newName: "IX_fot_Grupos_EventoId");

            migrationBuilder.RenameIndex(
                name: "IX_fot_Cursos_TenantId",
                table: "fot_Grupos",
                newName: "IX_fot_Grupos_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_fot_Albumes_CursoId",
                table: "fot_Participantes",
                newName: "IX_fot_Participantes_GrupoId");

            migrationBuilder.RenameIndex(
                name: "IX_fot_Albumes_TenantId",
                table: "fot_Participantes",
                newName: "IX_fot_Participantes_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_fot_Fotos_CursoId",
                table: "fot_Fotos",
                newName: "IX_fot_Fotos_GrupoId");

            migrationBuilder.RenameIndex(
                name: "IX_fot_Fotos_AlbumId",
                table: "fot_Fotos",
                newName: "IX_fot_Fotos_ParticipanteId");

            migrationBuilder.RenameIndex(
                name: "IX_fot_CodigosAcceso_AlbumId",
                table: "fot_CodigosAcceso",
                newName: "IX_fot_CodigosAcceso_ParticipanteId");

            migrationBuilder.RenameIndex(
                name: "IX_fot_Pedidos_AlbumId",
                table: "fot_Pedidos",
                newName: "IX_fot_Pedidos_ParticipanteId");

            // 4) Recrear PKs y FKs con los nombres de la convención nueva (mismo comportamiento
            //    de borrado que la migración VerticalFotos original).
            migrationBuilder.AddPrimaryKey(
                name: "PK_fot_Grupos",
                table: "fot_Grupos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_fot_Participantes",
                table: "fot_Participantes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_fot_Grupos_fot_Eventos_EventoId",
                table: "fot_Grupos",
                column: "EventoId",
                principalTable: "fot_Eventos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_fot_Participantes_fot_Grupos_GrupoId",
                table: "fot_Participantes",
                column: "GrupoId",
                principalTable: "fot_Grupos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_fot_CodigosAcceso_fot_Participantes_ParticipanteId",
                table: "fot_CodigosAcceso",
                column: "ParticipanteId",
                principalTable: "fot_Participantes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_fot_Fotos_fot_Grupos_GrupoId",
                table: "fot_Fotos",
                column: "GrupoId",
                principalTable: "fot_Grupos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_fot_Fotos_fot_Participantes_ParticipanteId",
                table: "fot_Fotos",
                column: "ParticipanteId",
                principalTable: "fot_Participantes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_fot_Pedidos_fot_Participantes_ParticipanteId",
                table: "fot_Pedidos",
                column: "ParticipanteId",
                principalTable: "fot_Participantes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_fot_CodigosAcceso_fot_Participantes_ParticipanteId",
                table: "fot_CodigosAcceso");

            migrationBuilder.DropForeignKey(
                name: "FK_fot_Fotos_fot_Participantes_ParticipanteId",
                table: "fot_Fotos");

            migrationBuilder.DropForeignKey(
                name: "FK_fot_Fotos_fot_Grupos_GrupoId",
                table: "fot_Fotos");

            migrationBuilder.DropForeignKey(
                name: "FK_fot_Pedidos_fot_Participantes_ParticipanteId",
                table: "fot_Pedidos");

            migrationBuilder.DropForeignKey(
                name: "FK_fot_Participantes_fot_Grupos_GrupoId",
                table: "fot_Participantes");

            migrationBuilder.DropForeignKey(
                name: "FK_fot_Grupos_fot_Eventos_EventoId",
                table: "fot_Grupos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_fot_Participantes",
                table: "fot_Participantes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_fot_Grupos",
                table: "fot_Grupos");

            migrationBuilder.RenameTable(
                name: "fot_Grupos",
                newName: "fot_Cursos");

            migrationBuilder.RenameTable(
                name: "fot_Participantes",
                newName: "fot_Albumes");

            migrationBuilder.RenameColumn(
                name: "GrupoId",
                table: "fot_Albumes",
                newName: "CursoId");

            migrationBuilder.RenameColumn(
                name: "Nombre",
                table: "fot_Albumes",
                newName: "NombreAlumno");

            migrationBuilder.RenameColumn(
                name: "GrupoId",
                table: "fot_Fotos",
                newName: "CursoId");

            migrationBuilder.RenameColumn(
                name: "ParticipanteId",
                table: "fot_Fotos",
                newName: "AlbumId");

            migrationBuilder.RenameColumn(
                name: "ParticipanteId",
                table: "fot_CodigosAcceso",
                newName: "AlbumId");

            migrationBuilder.RenameColumn(
                name: "ParticipanteId",
                table: "fot_Pedidos",
                newName: "AlbumId");

            migrationBuilder.RenameColumn(
                name: "LugarOrganizacion",
                table: "fot_Eventos",
                newName: "Colegio");

            migrationBuilder.RenameIndex(
                name: "IX_fot_Grupos_EventoId",
                table: "fot_Cursos",
                newName: "IX_fot_Cursos_EventoId");

            migrationBuilder.RenameIndex(
                name: "IX_fot_Grupos_TenantId",
                table: "fot_Cursos",
                newName: "IX_fot_Cursos_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_fot_Participantes_GrupoId",
                table: "fot_Albumes",
                newName: "IX_fot_Albumes_CursoId");

            migrationBuilder.RenameIndex(
                name: "IX_fot_Participantes_TenantId",
                table: "fot_Albumes",
                newName: "IX_fot_Albumes_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_fot_Fotos_GrupoId",
                table: "fot_Fotos",
                newName: "IX_fot_Fotos_CursoId");

            migrationBuilder.RenameIndex(
                name: "IX_fot_Fotos_ParticipanteId",
                table: "fot_Fotos",
                newName: "IX_fot_Fotos_AlbumId");

            migrationBuilder.RenameIndex(
                name: "IX_fot_CodigosAcceso_ParticipanteId",
                table: "fot_CodigosAcceso",
                newName: "IX_fot_CodigosAcceso_AlbumId");

            migrationBuilder.RenameIndex(
                name: "IX_fot_Pedidos_ParticipanteId",
                table: "fot_Pedidos",
                newName: "IX_fot_Pedidos_AlbumId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_fot_Cursos",
                table: "fot_Cursos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_fot_Albumes",
                table: "fot_Albumes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_fot_Cursos_fot_Eventos_EventoId",
                table: "fot_Cursos",
                column: "EventoId",
                principalTable: "fot_Eventos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_fot_Albumes_fot_Cursos_CursoId",
                table: "fot_Albumes",
                column: "CursoId",
                principalTable: "fot_Cursos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_fot_CodigosAcceso_fot_Albumes_AlbumId",
                table: "fot_CodigosAcceso",
                column: "AlbumId",
                principalTable: "fot_Albumes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_fot_Fotos_fot_Albumes_AlbumId",
                table: "fot_Fotos",
                column: "AlbumId",
                principalTable: "fot_Albumes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_fot_Fotos_fot_Cursos_CursoId",
                table: "fot_Fotos",
                column: "CursoId",
                principalTable: "fot_Cursos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_fot_Pedidos_fot_Albumes_AlbumId",
                table: "fot_Pedidos",
                column: "AlbumId",
                principalTable: "fot_Albumes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
