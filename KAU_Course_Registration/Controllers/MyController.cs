using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using KAU_Course_Registration.Controllers;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Extensions;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Protocol;
using JsonSerializer = System.Text.Json.JsonSerializer;


namespace TTest.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MyController : ControllerBase
{
    private readonly IConfiguration config;

    public MyController(IConfiguration config)
    {
        this.config = config;
    }
    //------------------------------------HTTP method for login and then generate a token for the user if found in DataBase--------------------------------------------------

    [HttpGet]
    [Route("LogIn")]
    public async Task<IActionResult> LogIn(int UserID, string UserPass)
    {
        IActionResult response = Unauthorized();
        var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
        try
        {
            var res = await conn.QueryFirstAsync<Members>(
                "select * from [KAU_Unv].[dbo].[MEMBERS]  where ID = @ID and Password = @Password",
                new { ID = UserID, Password = UserPass });

            if (res != null)
            {
                var token = Generate(res);
                response = Ok(new { token });

                return Ok(token);
            }
            else
            {
                return BadRequest("User not found in the database");
            }
        }
        catch (InvalidOperationException ex)
        {
            // Catch the exception that is thrown when no results are found
            return BadRequest("Error returning a user from the database: " + ex.Message);
        }
    }


    //------------------------------------Http method to add courses for a student ---------------------------------------------------

    [Authorize(Roles = "admin")]
    [HttpPost]
    [Route("AddCourses")]
    public async Task<IActionResult> AddCourse(int studentID, int courseID)
    {
        var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
        await conn.ExecuteAsync(
            "INSERT INTO [KAU_Unv].[dbo].[join_table] (student_id , course_id) values (@student_id , @course_id)",
            new { student_id = studentID, course_id = courseID });

        var cou = await conn.QueryAsync<StudentCourse>(
            @"SELECT  s.Fname as FirstName, s.Lname as LastName, s.ID as Id, s.Email, c.Code as CourseCode, c.Name as CourseName
                    FROM [KAU_Unv].[dbo].[MEMBERS] s 
                        INNER JOIN [KAU_Unv].[dbo].[join_table] sc on s.ID = sc.student_id
                        INNER JOIN [KAU_Unv].[dbo].[Courses] c on c.C_ID = sc.course_id
                        where s.ID = @studentID
        ", new { studentID = studentID });

        if (!cou.Any())
        {
            return BadRequest("Error gathering student courses.");
        }

        var result = new StudentCourses
        {
            FirstName = cou.First().FirstName,
            LastName = cou.First().LastName,
            Id = cou.First().Id,
            Email = cou.First().Email,
            Courses = cou.Select(x => new Course { CourseCode = x.CourseCode, CourseName = x.CourseName }).ToList()
        };
        return Ok(result);
    }


    //------------------------------------Http method to get users in the DataBase---------------------------------------------------
    [Authorize(Roles = "admin")]
    [HttpGet]
    [Route("getUsers")]
    public async Task<ActionResult> GetUsers()
    {
        var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
        var stu = await conn.QueryAsync<Members>("select * from  [KAU_Unv].[dbo].[MEMBERS]");
        return Ok(stu);
    }

    //-----------------------------------Http method to get available courses in DataBase---------------------------------------------

    [HttpGet]
    [Route("GetCourses")]
    public async Task<ActionResult> GetCourses()
    {
        var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
        var cou = await conn.QueryAsync<Courses>("select * from  [KAU_Unv].[dbo].[Courses]");
        return Ok(cou);
    }

    //-----------------------------------Http method that a student can use to see registered courses -------------------------------

    [Authorize(Roles = "student")]
    [HttpGet]
    [Route("Get_My_Courses")]
    public async Task<ActionResult> Get_Student_Courses()
    {
        string MYid = getID();
        int myidd = int.Parse(MYid);
        Console.WriteLine(myidd);
        var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
        var cou = await conn.QueryAsync<StudentCourse>(
            @"SELECT  s.Fname as FirstName, s.Lname as LastName, s.ID as Id, s.Email, c.Code as CourseCode, c.Name as CourseName
                    FROM [KAU_Unv].[dbo].[MEMBERS] s 
                        INNER JOIN [KAU_Unv].[dbo].[join_table] sc on s.ID = sc.student_id
                        INNER JOIN [KAU_Unv].[dbo].[Courses] c on c.C_ID = sc.course_id
                        where s.ID = @myidd
        ", new { myidd });
        if (!cou.Any())
        {
            return BadRequest("Error gathering student courses.");
        }

        var result = new StudentCourses
        {
            FirstName = cou.First().FirstName,
            LastName = cou.First().LastName,
            Id = cou.First().Id,
            Email = cou.First().Email,
            Courses = cou.Select(x => new Course { CourseCode = x.CourseCode, CourseName = x.CourseName }).ToList()
        };

        return Ok(result);
    }

    //-----------------------------------Http method that allow only admin to delete a course for a student-------------------------------

    [Authorize(Roles = "admin")]
    [HttpDelete]
    [Route("DeleteCourses")]
    public async Task<IActionResult> DeletCourse(int studentID, int courseID)
    {
        var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
        await conn.ExecuteAsync(
            "DELETE FROM  [KAU_Unv].[dbo].[join_table] WHERE student_id = @student_id and course_id = @course_id;",
            new { student_id = studentID, course_id = courseID });

        var cou = await conn.QueryAsync<StudentCourse>(
            @"SELECT  s.Fname as FirstName, s.Lname as LastName, s.ID as Id, s.Email, c.Code as CourseCode, c.Name as CourseName
                    FROM [KAU_Unv].[dbo].[MEMBERS] s 
                        INNER JOIN [KAU_Unv].[dbo].[join_table] sc on s.ID = sc.student_id
                        INNER JOIN [KAU_Unv].[dbo].[Courses] c on c.C_ID = sc.course_id
                        where s.ID = @studentID
        ", new { studentID = studentID });

        if (!cou.Any())
        {
            return BadRequest("Error gathering student courses.");
        }

        var result = new StudentCourses
        {
            FirstName = cou.First().FirstName,
            LastName = cou.First().LastName,
            Id = cou.First().Id,
            Email = cou.First().Email,
            Courses = cou.Select(x => new Course { CourseCode = x.CourseCode, CourseName = x.CourseName }).ToList()
        };

        return Ok(result);
    }

    //----------------------------------- Method to generate and return tokens---------------------------------------------------
    private string Generate(Members user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
        };

        var token = new JwtSecurityToken(config["Jwt:Issuer"],
            config["Jwt:Audience"],
            claims,
            expires: DateTime.Now.AddMinutes(60),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    //------------------------------Method to get user ID from token to use it----------------------------------------------------------
    private string getID()
    {
        var idenity = HttpContext.User.Identity as ClaimsIdentity;
        if (idenity != null)
        {
            var userc = idenity.Claims;
            string myid = userc.FirstOrDefault(o => o.Type == ClaimTypes.NameIdentifier)?.Value;
            return (myid);
        }

        return null;
    }
    
    
    //-------------------------------------------------------------------------------------------------
}