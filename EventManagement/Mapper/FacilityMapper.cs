using EventManagement.Model.Domain;
using EventManagement.Model.DTO;

namespace EventManagement.Mapper;

public static class FacilityMapper
{
    public static string OfficeName(DepartmentType d) => d switch
    {
        DepartmentType.Auxiliary => "Auxiliary",
        DepartmentType.ITMC => "ITMC (Technical)",
        DepartmentType.College => "College (Ms. Judith)",
        DepartmentType.TVET => "TVET (Ms. Chavez)",
        _ => d.ToString()
    };

    public static FacilityDto ToDto(this Facility f) => new(
        f.Id, f.Name, f.Department, OfficeName(f.Department), f.Department == DepartmentType.College);
}