using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public class SeedDefaultSkills : IMigration
{
    public string Id => $"2025-01-09-{typeof(SeedDefaultSkills).Name}";

    // Common industry skills (English) used as suggestion defaults.
    // Grouped by area for readability; insertion order doesn't matter.
    private static readonly string[] DefaultSkills =
    [
        // Programming languages
        "C#", "Java", "JavaScript", "TypeScript", "Python", "Go", "Rust", "C++", "C",
        "PHP", "Ruby", "Kotlin", "Swift", "Scala", "R", "Dart", "Objective-C",

        // Frontend
        "HTML", "CSS", "Sass", "Tailwind CSS", "Bootstrap", "Material UI",
        "React", "Next.js", "Angular", "Vue.js", "Svelte", "Redux", "RxJS",
        "jQuery", "Webpack", "Vite",

        // Backend
        "Node.js", "Express.js", "NestJS", "ASP.NET Core", ".NET", "Entity Framework",
        "Spring Boot", "Django", "Flask", "FastAPI", "Laravel", "Ruby on Rails",
        "GraphQL", "REST APIs", "gRPC", "WebSockets", "SignalR", "Microservices",

        // Mobile
        "Android", "iOS", "React Native", "Flutter", "Xamarin",

        // Databases
        "MongoDB", "MySQL", "PostgreSQL", "SQL Server", "Oracle", "SQLite",
        "Redis", "Elasticsearch", "DynamoDB", "Cassandra", "Firebase",

        // Cloud / DevOps
        "AWS", "Azure", "Google Cloud Platform", "Docker", "Kubernetes",
        "Terraform", "Ansible", "Jenkins", "GitHub Actions", "Azure DevOps", "GitLab CI",
        "Linux", "Bash", "PowerShell", "Nginx", "Apache",

        // Version control / collaboration
        "Git", "GitHub", "GitLab", "Bitbucket", "Jira", "Confluence",

        // Testing
        "Unit Testing", "Integration Testing", "End-to-End Testing",
        "Jest", "Mocha", "Cypress", "Playwright", "Selenium", "xUnit", "NUnit",

        // Data / AI
        "SQL", "NoSQL", "Pandas", "NumPy", "TensorFlow", "PyTorch",
        "Machine Learning", "Deep Learning", "Data Analysis", "Power BI", "Tableau",

        // Architecture / methodology
        "Object-Oriented Programming", "Functional Programming", "Design Patterns",
        "SOLID Principles", "Clean Architecture", "Domain-Driven Design",
        "Agile", "Scrum", "Kanban", "TDD", "BDD", "CI/CD",

        // Security
        "OAuth", "JWT", "OWASP", "Penetration Testing", "Cryptography",

        // Soft skills
        "Communication", "Teamwork", "Problem Solving", "Critical Thinking",
        "Leadership", "Time Management", "Project Management", "Mentoring",
        "Adaptability", "Stakeholder Management",
    ];

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(m => m.Id == Id).AnyAsync()) return;

        var skills = db.GetCollection<Skills>(typeof(Skills).Name);

        // Skip skills that already exist by Name (case-insensitive match).
        var existingNames = (await skills
            .Find(FilterDefinition<Skills>.Empty)
            .Project(s => s.Name)
            .ToListAsync())
            .Select(n => n?.Trim().ToLowerInvariant())
            .Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet();

        var newSkills = DefaultSkills
            .Where(name => !existingNames.Contains(name.Trim().ToLowerInvariant()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => new Skills { Name = name })
            .ToList();

        if (newSkills.Count > 0)
            await skills.InsertManyAsync(newSkills);

        await migrations.InsertOneAsync(new Migration
        {
            Id = Id,
            ExecutedAt = DateTime.UtcNow,
        });
    }
}
