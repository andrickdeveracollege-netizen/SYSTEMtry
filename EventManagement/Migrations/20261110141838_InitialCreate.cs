using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EventManagement.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Equipments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Department = table.Column<int>(type: "int", nullable: false),
                    IsOther = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequesterName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RequesterEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequesterPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Organization = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    TargetAudience = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsGuest = table.Column<bool>(type: "bit", nullable: false),
                    IsInstitutional = table.Column<bool>(type: "bit", nullable: false),
                    RequiresRegistration = table.Column<bool>(type: "bit", nullable: false),
                    ManageCode = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Facilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Department = table.Column<int>(type: "int", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: true),
                    IsOther = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    AdminDepartment = table.Column<int>(type: "int", nullable: true),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DepartmentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampusEventId = table.Column<int>(type: "int", nullable: false),
                    Department = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EndorsementStoredName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EndorsementOriginalName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentRequests_Events_CampusEventId",
                        column: x => x.CampusEventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Registrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampusEventId = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    YearLevel = table.Column<int>(type: "int", nullable: false),
                    Confirmed = table.Column<bool>(type: "bit", nullable: false),
                    Cancelled = table.Column<bool>(type: "bit", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Registrations_Events_CampusEventId",
                        column: x => x.CampusEventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventEquipments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentRequestId = table.Column<int>(type: "int", nullable: false),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventEquipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventEquipments_DepartmentRequests_DepartmentRequestId",
                        column: x => x.DepartmentRequestId,
                        principalTable: "DepartmentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventEquipments_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventFacilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentRequestId = table.Column<int>(type: "int", nullable: false),
                    FacilityId = table.Column<int>(type: "int", nullable: false),
                    OtherName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventFacilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventFacilities_DepartmentRequests_DepartmentRequestId",
                        column: x => x.DepartmentRequestId,
                        principalTable: "DepartmentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventFacilities_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Equipments",
                columns: new[] { "Id", "Department", "IsOther", "Name" },
                values: new object[,]
                {
                    { 1, 1, false, "Speaker" },
                    { 2, 1, false, "Microphone" },
                    { 3, 1, false, "Table" },
                    { 4, 1, false, "Chair" },
                    { 5, 1, false, "Podium" },
                    { 6, 1, true, "Others" },
                    { 7, 2, false, "Computer" },
                    { 8, 2, false, "Laptop" },
                    { 9, 2, false, "HDMI Cable" },
                    { 10, 2, false, "Wi-Fi Access" },
                    { 11, 2, false, "Projector" },
                    { 12, 2, true, "Others" }
                });

            migrationBuilder.InsertData(
                table: "Facilities",
                columns: new[] { "Id", "Capacity", "Department", "IsOther", "Name" },
                values: new object[,]
                {
                    { 1, 2000, 1, false, "Field" },
                    { 2, 1000, 1, false, "Gym" },
                    { 3, 40, 1, false, "Dance Room" },
                    { 4, 150, 1, false, "Blessed Diana Hall" },
                    { 5, 180, 1, false, "Auditorium" },
                    { 6, 800, 1, false, "Elem Grounds" },
                    { 7, null, 1, true, "Others" },
                    { 8, 40, 2, false, "Comlab 1" },
                    { 9, 40, 2, false, "Comlab 2" },
                    { 10, 40, 2, false, "Comlab 3" },
                    { 11, 40, 2, false, "Comlab 4" },
                    { 12, 40, 2, false, "Comlab 5" },
                    { 13, null, 2, true, "Others" },
                    { 14, 100, 3, false, "AULA MAYOR" },
                    { 15, 20, 3, false, "Deans Conference" },
                    { 16, 45, 3, false, "UNO" },
                    { 17, 45, 3, false, "DOS" },
                    { 18, 45, 3, false, "TRES" },
                    { 19, 45, 3, false, "QUATRO" },
                    { 20, 45, 3, false, "CINCO" },
                    { 21, 45, 3, false, "SEIS" },
                    { 22, 45, 3, false, "CARITAS" },
                    { 23, 45, 3, false, "FORTITUDO" },
                    { 24, 45, 3, false, "JUSTITIA" },
                    { 25, null, 3, true, "Others" },
                    { 26, 10, 4, false, "Single Room" },
                    { 27, 20, 4, false, "Double Room" },
                    { 28, 45, 4, false, "Classrooms" },
                    { 29, 30, 4, false, "FBS Lab" },
                    { 30, 30, 4, false, "BBP Lab" },
                    { 31, 30, 4, false, "Cookery Lab" },
                    { 32, 50, 4, false, "Ange Cafe" },
                    { 33, null, 4, true, "Others" },
                    { 34, 100, 5, false, "MAIN LIB" },
                    { 35, 60, 5, false, "LOWER LIB" },
                    { 36, 40, 5, false, "IMC" },
                    { 37, null, 5, true, "Others" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentRequests_CampusEventId_Department",
                table: "DepartmentRequests",
                columns: new[] { "CampusEventId", "Department" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventEquipments_DepartmentRequestId",
                table: "EventEquipments",
                column: "DepartmentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_EventEquipments_EquipmentId",
                table: "EventEquipments",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EventFacilities_DepartmentRequestId_FacilityId",
                table: "EventFacilities",
                columns: new[] { "DepartmentRequestId", "FacilityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventFacilities_FacilityId",
                table: "EventFacilities",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_StartTime_EndTime",
                table: "Events",
                columns: new[] { "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_Department_Name",
                table: "Facilities",
                columns: new[] { "Department", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_CampusEventId_Email",
                table: "Registrations",
                columns: new[] { "CampusEventId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventEquipments");

            migrationBuilder.DropTable(
                name: "EventFacilities");

            migrationBuilder.DropTable(
                name: "Registrations");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Equipments");

            migrationBuilder.DropTable(
                name: "DepartmentRequests");

            migrationBuilder.DropTable(
                name: "Facilities");

            migrationBuilder.DropTable(
                name: "Events");
        }
    }
}
