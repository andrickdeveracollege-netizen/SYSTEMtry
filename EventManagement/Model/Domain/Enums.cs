namespace EventManagement.Model.Domain;

public enum DepartmentType { Auxiliary = 1, ITMC = 2, College = 3, TVET = 4, Library = 5 }

// Flags so an event can target several levels. A registrant picks exactly one value.
[Flags]
public enum YearLevel
{
    None = 0,
    Elementary = 1,
    JHS = 2,
    SHS = 4,
    College = 8,
    All = 15
}

public enum RequesterRole { Faculty = 1, Student = 2 }
public enum EventStatus { Pending = 1, Approved = 2, Rejected = 3, Cancelled = 4 }
public enum UserRole { User = 1, Admin = 2 }