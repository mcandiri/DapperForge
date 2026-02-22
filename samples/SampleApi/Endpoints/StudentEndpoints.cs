using DapperForge;
using SampleApi.Models;

namespace SampleApi.Endpoints;

public static class StudentEndpoints
{
    public static void MapStudentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/students").WithTags("Students");

        // Convention-based: Resolves to "Get_Students"
        group.MapGet("/", async (IForgeConnection forge) =>
        {
            var students = await forge.GetAsync<Student>();
            return Results.Ok(students);
        });

        // Convention-based: Resolves to "Get_Students" with @Id param
        group.MapGet("/{id:int}", async (int id, IForgeConnection forge) =>
        {
            var student = await forge.GetSingleAsync<Student>(new { Id = id });
            return student is not null ? Results.Ok(student) : Results.NotFound();
        });

        // Convention-based: Resolves to "Save_Students"
        group.MapPost("/", async (Student student, IForgeConnection forge) =>
        {
            await forge.SaveAsync(student);
            return Results.Created($"/api/students/{student.Id}", student);
        });

        // Convention-based: Resolves to "Save_Students"
        group.MapPut("/{id:int}", async (int id, Student student, IForgeConnection forge) =>
        {
            student.Id = id;
            await forge.SaveAsync(student);
            return Results.NoContent();
        });

        // Convention-based: Resolves to "Remove_Students"
        group.MapDelete("/{id:int}", async (int id, IForgeConnection forge) =>
        {
            await forge.RemoveAsync<Student>(new { Id = id });
            return Results.NoContent();
        });

        // Direct SP call: "rpt_ActiveStudents"
        group.MapGet("/active", async (IForgeConnection forge) =>
        {
            var active = await forge.ExecuteSpAsync<Student>("rpt_ActiveStudents", new { IsActive = true });
            return Results.Ok(active);
        });

        // Scalar SP call: "sel_StudentCount"
        group.MapGet("/count", async (IForgeConnection forge) =>
        {
            var count = await forge.ExecuteSpScalarAsync<int>("sel_StudentCount");
            return Results.Ok(new { Count = count });
        });

        // Transaction example
        group.MapPost("/bulk", async (Student[] students, IForgeConnection forge) =>
        {
            await forge.InTransactionAsync(async tx =>
            {
                foreach (var student in students)
                {
                    await tx.SaveAsync(student);
                }
            });
            return Results.Ok(new { Saved = students.Length });
        });
    }
}
