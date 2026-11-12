namespace EventManagement.Model.Domain;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public DepartmentType? AdminDepartment { get; set; }

    // Student / Faculty / Administrator. Set from the email format when the account is created.
    public RequesterRole Position { get; set; } = RequesterRole.Faculty;
    // Only for students: the level taken from the email (.college .elem .jhs .shs)
    public YearLevel Level { get; set; } = YearLevel.None;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}