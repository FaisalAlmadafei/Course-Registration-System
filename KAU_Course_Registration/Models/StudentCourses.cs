namespace KAU_Course_Registration.Controllers;

public class StudentCourses
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int Id { get; set; }
    public string Email { get; set; }
    public List<Course> Courses { get; set; }
}