using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AccessControl.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class InicialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "localidades",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    es_sistema_aislado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_localidades", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    rol = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "empleados",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    apellido = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    cedula = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cargo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    localidad_id = table.Column<int>(type: "integer", nullable: false),
                    foto_carnet_path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    embedding_json = table.Column<string>(type: "jsonb", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_empleados", x => x.id);
                    table.ForeignKey(
                        name: "fk_empleados_localidades_localidad_id",
                        column: x => x.localidad_id,
                        principalTable: "localidades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "terminales",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    localidad_id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ubicacion_descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_terminales", x => x.id);
                    table.ForeignKey(
                        name: "fk_terminales_localidades_localidad_id",
                        column: x => x.localidad_id,
                        principalTable: "localidades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "marcaciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empleado_id = table.Column<int>(type: "integer", nullable: true),
                    terminal_id = table.Column<int>(type: "integer", nullable: false),
                    localidad_id = table.Column<int>(type: "integer", nullable: false),
                    resultado = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    score_similitud = table.Column<double>(type: "double precision", nullable: false),
                    captura_path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    timestamp_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_marcaciones", x => x.id);
                    table.ForeignKey(
                        name: "fk_marcaciones_empleados_empleado_id",
                        column: x => x.empleado_id,
                        principalTable: "empleados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_marcaciones_localidades_localidad_id",
                        column: x => x.localidad_id,
                        principalTable: "localidades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_marcaciones_terminales_terminal_id",
                        column: x => x.terminal_id,
                        principalTable: "terminales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_empleados_cedula",
                table: "empleados",
                column: "cedula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_empleados_codigo",
                table: "empleados",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_empleados_localidad_id",
                table: "empleados",
                column: "localidad_id");

            migrationBuilder.CreateIndex(
                name: "ix_localidades_nombre",
                table: "localidades",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_marcaciones_empleado_id",
                table: "marcaciones",
                column: "empleado_id");

            migrationBuilder.CreateIndex(
                name: "ix_marcaciones_localidad_id",
                table: "marcaciones",
                column: "localidad_id");

            migrationBuilder.CreateIndex(
                name: "ix_marcaciones_terminal_id",
                table: "marcaciones",
                column: "terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_marcaciones_timestamp_utc",
                table: "marcaciones",
                column: "timestamp_utc");

            migrationBuilder.CreateIndex(
                name: "ix_terminales_localidad_id",
                table: "terminales",
                column: "localidad_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "marcaciones");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "empleados");

            migrationBuilder.DropTable(
                name: "terminales");

            migrationBuilder.DropTable(
                name: "localidades");
        }
    }
}
