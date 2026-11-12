namespace EventManagement.Model.Domain;

public enum DepartmentType { Auxiliary = 1, ITMC = 2, College = 3, TVET = 4, Library = 5 }

// Flags so an event can target several groups. A school student has exactly one level.
[Flags]
public enum YearLevel
{
    None = 0,
    Elementary = 1,
    JHS = 2,
    SHS = 4,
    College = 8,
    Outsider = 16,      // people who do not study / work in the school
    All = 31
}

// Who is asking. Derived from the email address (or Guest for outsiders), never typed by the user.
public enum RequesterRole { Faculty = 1, Student = 2, Administrator = 3, Guest = 4 }

public enum EventStatus { Pending = 1, Approved = 2, Rejected = 3, Cancelled = 4 }

// User = student / faculty / staff. Admin = one office. SuperAdmin = admin@ustangelicum.edu.ph
public enum UserRole { User = 1, Admin = 2, SuperAdmin = 3 }
