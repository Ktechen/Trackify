using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrackLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromPartId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromPort = table.Column<string>(type: "TEXT", nullable: false),
                    ToPartId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToPort = table.Column<string>(type: "TEXT", nullable: false),
                    DateCreated = table.Column<long>(type: "INTEGER", nullable: false),
                    DateUpdated = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackParts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SpecKey = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PlanOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    Direction = table.Column<string>(type: "TEXT", nullable: false),
                    AccelFn = table.Column<string>(type: "TEXT", nullable: false),
                    BrakeFn = table.Column<string>(type: "TEXT", nullable: false),
                    Sensor = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    SlowTarget = table.Column<int>(type: "INTEGER", nullable: false),
                    DateCreated = table.Column<long>(type: "INTEGER", nullable: false),
                    DateUpdated = table.Column<long>(type: "INTEGER", nullable: false),
                    Switch = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackParts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    MaxSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    Direction = table.Column<string>(type: "TEXT", nullable: false),
                    AccelFn = table.Column<string>(type: "TEXT", nullable: false),
                    BrakeFn = table.Column<string>(type: "TEXT", nullable: false),
                    Sensor = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    SlowTarget = table.Column<int>(type: "INTEGER", nullable: false),
                    StrandId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Curve = table.Column<string>(type: "TEXT", nullable: true),
                    BranchStrandId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BranchDirection = table.Column<string>(type: "TEXT", nullable: true),
                    Route = table.Column<string>(type: "TEXT", nullable: false),
                    BranchLinkSegmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DateCreated = table.Column<long>(type: "INTEGER", nullable: false),
                    DateUpdated = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackSegments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Hub = table.Column<string>(type: "TEXT", nullable: false),
                    BleAddress = table.Column<string>(type: "TEXT", nullable: false),
                    HubId = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: false),
                    PortA = table.Column<string>(type: "TEXT", nullable: false),
                    PortB = table.Column<string>(type: "TEXT", nullable: false),
                    Speed = table.Column<int>(type: "INTEGER", nullable: false),
                    AccelFn = table.Column<string>(type: "TEXT", nullable: false),
                    AccelExpression = table.Column<string>(type: "TEXT", nullable: false),
                    BrakeFn = table.Column<string>(type: "TEXT", nullable: false),
                    BrakeExpression = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateCreated = table.Column<long>(type: "INTEGER", nullable: false),
                    DateUpdated = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trains", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackLinks_FromPartId",
                table: "TrackLinks",
                column: "FromPartId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackLinks_ToPartId",
                table: "TrackLinks",
                column: "ToPartId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackParts_PlanOrder",
                table: "TrackParts",
                column: "PlanOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackLinks");

            migrationBuilder.DropTable(
                name: "TrackParts");

            migrationBuilder.DropTable(
                name: "TrackSegments");

            migrationBuilder.DropTable(
                name: "Trains");
        }
    }
}
