using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropTrackSegments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackSegments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrackSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccelFn = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    BrakeFn = table.Column<string>(type: "TEXT", nullable: false),
                    BranchDirection = table.Column<string>(type: "TEXT", nullable: true),
                    BranchLinkSegmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BranchStrandId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Curve = table.Column<string>(type: "TEXT", nullable: true),
                    DateCreated = table.Column<long>(type: "INTEGER", nullable: false),
                    DateUpdated = table.Column<long>(type: "INTEGER", nullable: false),
                    Direction = table.Column<string>(type: "TEXT", nullable: false),
                    MaxSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Route = table.Column<string>(type: "TEXT", nullable: false),
                    Sensor = table.Column<string>(type: "TEXT", nullable: false),
                    SlowTarget = table.Column<int>(type: "INTEGER", nullable: false),
                    StrandId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackSegments", x => x.Id);
                });
        }
    }
}
