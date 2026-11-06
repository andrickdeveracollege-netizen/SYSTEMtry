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
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<CampusEvent>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.RequesterName).HasMaxLength(150).IsRequired();
            e.Property(x => x.RequesterEmail).HasMaxLength(200).IsRequired();
            e.Property(x => x.Organization).HasMaxLength(150).IsRequired();
            e.Property(x => x.Purpose).HasMaxLength(2000).IsRequired();
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

    private static List<Facility> SeedFacilities()
    {
        var list = new List<Facility>();
        int id = 1;
        void Add(DepartmentType d, params string[] names)
        {
            foreach (var n in names) list.Add(new Facility { Id = id++, Name = n, Department = d });
        }

        Add(DepartmentType.Auxiliary, "Field", "Gym", "Dance Room", "Blessed Diana Hall", "Auditorium", "Elem Grounds");
        Add(DepartmentType.ITMC, "Comlab 1", "Comlab 2", "Comlab 3", "Comlab 4", "Comlab 5");
        Add(DepartmentType.College, "AULA MAYOR", "Deans Conference", "UNO", "DOS", "TRES", "QUATRO",
            "CINCO", "SEIS", "CARITAS", "FORTITUDO", "JUSTITIA");
        Add(DepartmentType.TVET, "Single Room", "Double Room", "Classrooms", "FBS Lab", "BBP Lab",
            "Cookery Lab", "Ange Cafe");
        // Appended last so the existing facility Ids stay the same. Add more Library rooms here.
        Add(DepartmentType.Library, "IMC", "Discussion Room");
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
