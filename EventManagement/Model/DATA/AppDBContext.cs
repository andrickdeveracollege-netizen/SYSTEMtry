using EventManagement.Model.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Model.Data;

public class AppDBContext : DbContext
{
    public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<CampusEvent> Events => Set<CampusEvent>();
    public DbSet<DepartmentRequest> DepartmentRequests => Set<DepartmentRequest>();
    public DbSet<EventFacility> EventFacilities => Set<EventFacility>();
    public DbSet<EventRegistration> Registrations => Set<EventRegistration>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<EventEquipment> EventEquipments => Set<EventEquipment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        });

        b.Entity<Facility>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.Department, x.Name }).IsUnique();
        });

        b.Entity<CampusEvent>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.RequesterName).HasMaxLength(150).IsRequired();
            e.Property(x => x.RequesterEmail).HasMaxLength(200).IsRequired();
            e.Property(x => x.Organization).HasMaxLength(150).IsRequired();
            e.Property(x => x.Purpose).HasMaxLength(2000).IsRequired();
            e.Property(x => x.RequesterPhone).HasMaxLength(30);
            e.Property(x => x.ManageCode).HasMaxLength(12).IsRequired();
            e.HasIndex(x => new { x.StartTime, x.EndTime });
        });

        b.Entity<DepartmentRequest>(e =>
        {
            e.Property(x => x.RejectionReason).HasMaxLength(500);
            e.Property(x => x.EndorsementStoredName).HasMaxLength(100);
            e.Property(x => x.EndorsementOriginalName).HasMaxLength(260);
            e.HasOne(x => x.CampusEvent).WithMany(c => c.DepartmentRequests)
             .HasForeignKey(x => x.CampusEventId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CampusEventId, x.Department }).IsUnique();
        });

        b.Entity<EventFacility>(e =>
        {
            e.Property(x => x.OtherName).HasMaxLength(100);
            e.HasOne(x => x.DepartmentRequest).WithMany(d => d.Facilities)
             .HasForeignKey(x => x.DepartmentRequestId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Facility).WithMany(f => f.EventFacilities)
             .HasForeignKey(x => x.FacilityId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.DepartmentRequestId, x.FacilityId }).IsUnique();
        });

        b.Entity<EventRegistration>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            e.HasOne(x => x.CampusEvent).WithMany(c => c.Registrations)
             .HasForeignKey(x => x.CampusEventId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CampusEventId, x.Email }).IsUnique();
        });

        b.Entity<Equipment>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });

        b.Entity<EventEquipment>(e =>
        {
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasOne(x => x.DepartmentRequest).WithMany(d => d.EquipmentItems)
             .HasForeignKey(x => x.DepartmentRequestId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Equipment).WithMany()
             .HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Facility>().HasData(SeedFacilities());
        b.Entity<Equipment>().HasData(SeedEquipment());
    }

    // Capacities are placeholders: change the numbers to the real room sizes.
    // Keep the order, because the Ids come from it.
    private static List<Facility> SeedFacilities()
    {
        var list = new List<Facility>();
        int id = 1;
        void Add(DepartmentType d, string name, int? capacity, bool other = false) =>
            list.Add(new Facility { Id = id++, Name = name, Department = d, Capacity = capacity, IsOther = other });

        // Auxiliary (big events: Field, Gym)
        Add(DepartmentType.Auxiliary, "Field", 2000);
        Add(DepartmentType.Auxiliary, "Gym", 1000);
        Add(DepartmentType.Auxiliary, "Dance Room", 40);
        Add(DepartmentType.Auxiliary, "Blessed Diana Hall", 150);
        Add(DepartmentType.Auxiliary, "Auditorium", 180);
        Add(DepartmentType.Auxiliary, "Elem Grounds", 800);
        Add(DepartmentType.Auxiliary, "Others", null, other: true);

        // ITMC
        foreach (var n in new[] { "Comlab 1", "Comlab 2", "Comlab 3", "Comlab 4", "Comlab 5" })
            Add(DepartmentType.ITMC, n, 40);
        Add(DepartmentType.ITMC, "Others", null, other: true);

        // College (Ms. Judith)
        Add(DepartmentType.College, "AULA MAYOR", 100);
        Add(DepartmentType.College, "Deans Conference", 20);
        foreach (var n in new[] { "UNO", "DOS", "TRES", "QUATRO", "CINCO", "SEIS", "CARITAS", "FORTITUDO", "JUSTITIA" })
            Add(DepartmentType.College, n, 45);
        Add(DepartmentType.College, "Others", null, other: true);

        // TVET (Ms. Chavez)
        Add(DepartmentType.TVET, "Single Room", 10);
        Add(DepartmentType.TVET, "Double Room", 20);
        Add(DepartmentType.TVET, "Classrooms", 45);
        Add(DepartmentType.TVET, "FBS Lab", 30);
        Add(DepartmentType.TVET, "BBP Lab", 30);
        Add(DepartmentType.TVET, "Cookery Lab", 30);
        Add(DepartmentType.TVET, "Ange Cafe", 50);
        Add(DepartmentType.TVET, "Others", null, other: true);

        // Library
        Add(DepartmentType.Library, "MAIN LIB", 100);
        Add(DepartmentType.Library, "LOWER LIB", 60);
        Add(DepartmentType.Library, "IMC", 40);
        Add(DepartmentType.Library, "Others", null, other: true);
        return list;
    }

    // Keep the existing Ids stable once migrated.
    private static List<Equipment> SeedEquipment()
    {
        var list = new List<Equipment>();
        int id = 1;
        void Add(DepartmentType d, string name, bool other = false) =>
            list.Add(new Equipment { Id = id++, Name = name, Department = d, IsOther = other });

        Add(DepartmentType.Auxiliary, "Speaker");
        Add(DepartmentType.Auxiliary, "Microphone");
        Add(DepartmentType.Auxiliary, "Table");
        Add(DepartmentType.Auxiliary, "Chair");
        Add(DepartmentType.Auxiliary, "Podium");
        Add(DepartmentType.Auxiliary, "Others", other: true);

        Add(DepartmentType.ITMC, "Computer");
        Add(DepartmentType.ITMC, "Laptop");
        Add(DepartmentType.ITMC, "HDMI Cable");
        Add(DepartmentType.ITMC, "Wi-Fi Access");
        Add(DepartmentType.ITMC, "Projector");
        Add(DepartmentType.ITMC, "Others", other: true);
        return list;
    }
}