using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetCare.Web.Datos.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hogares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    CreadoUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hogares", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cuidadores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HogarId = table.Column<int>(type: "INTEGER", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    EmailNormalizado = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    CreadoUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuidadores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cuidadores_Hogares_HogarId",
                        column: x => x.HogarId,
                        principalTable: "Hogares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mascotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HogarId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    NombreNormalizado = table.Column<string>(type: "TEXT", nullable: false),
                    CreadaDia = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    MaxDiasEntreBanos = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mascotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mascotas_Hogares_HogarId",
                        column: x => x.HogarId,
                        principalTable: "Hogares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Deberes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MascotaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false),
                    Subtipo = table.Column<string>(type: "TEXT", nullable: true),
                    Modalidad = table.Column<string>(type: "TEXT", nullable: true),
                    HoraInicio = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    IntervaloHoras = table.Column<int>(type: "INTEGER", nullable: true),
                    Recurrencia = table.Column<string>(type: "TEXT", nullable: true),
                    HastaDia = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Dia = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Hora = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    CreadoUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreadoDia = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DadoDeBajaUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deberes", x => x.Id);
                    table.CheckConstraint("CK_Deberes_IntervaloHoras", "IntervaloHoras BETWEEN 1 AND 23");
                    table.ForeignKey(
                        name: "FK_Deberes_Mascotas_MascotaId",
                        column: x => x.MascotaId,
                        principalTable: "Mascotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Instancias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeberId = table.Column<int>(type: "INTEGER", nullable: false),
                    Dia = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Hora = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    ProgramadaUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    GeneradaUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GeneradaVencida = table.Column<bool>(type: "INTEGER", nullable: false),
                    EpisodioVencimiento = table.Column<int>(type: "INTEGER", nullable: false),
                    ReactivadaUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instancias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Instancias_Deberes_DeberId",
                        column: x => x.DeberId,
                        principalTable: "Deberes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false),
                    Clave = table.Column<string>(type: "TEXT", nullable: false),
                    InstanciaId = table.Column<int>(type: "INTEGER", nullable: true),
                    DeberId = table.Column<int>(type: "INTEGER", nullable: true),
                    MascotaId = table.Column<int>(type: "INTEGER", nullable: true),
                    CondicionUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    Intentos = table.Column<int>(type: "INTEGER", nullable: false),
                    UltimoIntentoUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AceptadaUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificaciones_Deberes_DeberId",
                        column: x => x.DeberId,
                        principalTable: "Deberes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notificaciones_Instancias_InstanciaId",
                        column: x => x.InstanciaId,
                        principalTable: "Instancias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notificaciones_Mascotas_MascotaId",
                        column: x => x.MascotaId,
                        principalTable: "Mascotas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RegistrosCumplimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InstanciaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Dia = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Hora = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    RegistradoUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosCumplimiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosCumplimiento_Instancias_InstanciaId",
                        column: x => x.InstanciaId,
                        principalTable: "Instancias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cuidadores_EmailNormalizado",
                table: "Cuidadores",
                column: "EmailNormalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cuidadores_HogarId",
                table: "Cuidadores",
                column: "HogarId");

            migrationBuilder.CreateIndex(
                name: "IX_Deberes_MascotaId",
                table: "Deberes",
                column: "MascotaId");

            migrationBuilder.CreateIndex(
                name: "IX_Instancias_DeberId_ProgramadaUtc",
                table: "Instancias",
                columns: new[] { "DeberId", "ProgramadaUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Instancias_Estado_Dia",
                table: "Instancias",
                columns: new[] { "Estado", "Dia" });

            migrationBuilder.CreateIndex(
                name: "IX_Mascotas_HogarId_NombreNormalizado",
                table: "Mascotas",
                columns: new[] { "HogarId", "NombreNormalizado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_Clave",
                table: "Notificaciones",
                column: "Clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_DeberId",
                table: "Notificaciones",
                column: "DeberId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_Estado",
                table: "Notificaciones",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_InstanciaId",
                table: "Notificaciones",
                column: "InstanciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_MascotaId",
                table: "Notificaciones",
                column: "MascotaId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosCumplimiento_Dia",
                table: "RegistrosCumplimiento",
                column: "Dia");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosCumplimiento_InstanciaId",
                table: "RegistrosCumplimiento",
                column: "InstanciaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cuidadores");

            migrationBuilder.DropTable(
                name: "Notificaciones");

            migrationBuilder.DropTable(
                name: "RegistrosCumplimiento");

            migrationBuilder.DropTable(
                name: "Instancias");

            migrationBuilder.DropTable(
                name: "Deberes");

            migrationBuilder.DropTable(
                name: "Mascotas");

            migrationBuilder.DropTable(
                name: "Hogares");
        }
    }
}
