using Codeji.CMS.DTO;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Utility.Constraints;

namespace Codeji.CMS.Services.Companies;

// Default lookup data inserted for every newly created company.
// Labels are translated into each language listed in Company.ApplicationLanguage;
// missing translations fall back to the English label.
public static class DefaultCompanySeeds
{
    private static readonly string[] DefaultDepartments =
    [
        "Engineering",
        "Human Resources",
        "Finance & Accounts",
        "Sales",
        "Marketing",
        "Operations",
        "Customer Support",
        "Administration",
    ];

    private static readonly (string Title, string Department)[] DefaultJobTitles =
    [
        ("Software Engineer", "Engineering"), ("Senior Software Engineer", "Engineering"),
        ("Team Lead", "Engineering"), ("Project Manager", "Operations"),
        ("Business Analyst", "Operations"), ("Quality Analyst", "Engineering"),
        ("UI/UX Designer", "Engineering"), ("HR Executive", "Human Resources"),
        ("HR Manager", "Human Resources"), ("Accountant", "Finance & Accounts"),
        ("Sales Executive", "Sales"), ("Marketing Executive", "Marketing"),
    ];

    // EN label -> { language code -> localized label }
    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        // ---------- Departments ----------
        ["Engineering"] = new()
        {
            ["zh"] = "工程部", ["es"] = "Ingeniería", ["ja"] = "エンジニアリング",
            ["de"] = "Entwicklung", ["fr"] = "Ingénierie",
        },
        ["Human Resources"] = new()
        {
            ["zh"] = "人力资源", ["es"] = "Recursos Humanos", ["ja"] = "人事",
            ["de"] = "Personalwesen", ["fr"] = "Ressources Humaines",
        },
        ["Finance & Accounts"] = new()
        {
            ["zh"] = "财务与会计", ["es"] = "Finanzas y Contabilidad", ["ja"] = "財務・経理",
            ["de"] = "Finanzen & Buchhaltung", ["fr"] = "Finance et Comptabilité",
        },
        ["Sales"] = new()
        {
            ["zh"] = "销售", ["es"] = "Ventas", ["ja"] = "営業",
            ["de"] = "Vertrieb", ["fr"] = "Ventes",
        },
        ["Marketing"] = new()
        {
            ["zh"] = "市场营销", ["es"] = "Marketing", ["ja"] = "マーケティング",
            ["de"] = "Marketing", ["fr"] = "Marketing",
        },
        ["Operations"] = new()
        {
            ["zh"] = "运营", ["es"] = "Operaciones", ["ja"] = "業務",
            ["de"] = "Betrieb", ["fr"] = "Opérations",
        },
        ["Customer Support"] = new()
        {
            ["zh"] = "客户支持", ["es"] = "Atención al Cliente", ["ja"] = "カスタマーサポート",
            ["de"] = "Kundensupport", ["fr"] = "Support Client",
        },
        ["Administration"] = new()
        {
            ["zh"] = "行政", ["es"] = "Administración", ["ja"] = "管理部",
            ["de"] = "Verwaltung", ["fr"] = "Administration",
        },

        // ---------- Job Titles ----------
        ["Software Engineer"] = new()
        {
            ["zh"] = "软件工程师", ["es"] = "Ingeniero de Software", ["ja"] = "ソフトウェアエンジニア",
            ["de"] = "Softwareentwickler", ["fr"] = "Ingénieur Logiciel",
        },
        ["Senior Software Engineer"] = new()
        {
            ["zh"] = "高级软件工程师", ["es"] = "Ingeniero de Software Senior", ["ja"] = "シニアソフトウェアエンジニア",
            ["de"] = "Senior Softwareentwickler", ["fr"] = "Ingénieur Logiciel Senior",
        },
        ["Team Lead"] = new()
        {
            ["zh"] = "团队负责人", ["es"] = "Líder de Equipo", ["ja"] = "チームリード",
            ["de"] = "Teamleiter", ["fr"] = "Chef d'Équipe",
        },
        ["Project Manager"] = new()
        {
            ["zh"] = "项目经理", ["es"] = "Gerente de Proyecto", ["ja"] = "プロジェクトマネージャー",
            ["de"] = "Projektmanager", ["fr"] = "Chef de Projet",
        },
        ["Business Analyst"] = new()
        {
            ["zh"] = "业务分析师", ["es"] = "Analista de Negocios", ["ja"] = "ビジネスアナリスト",
            ["de"] = "Business Analyst", ["fr"] = "Analyste d'Affaires",
        },
        ["Quality Analyst"] = new()
        {
            ["zh"] = "质量分析师", ["es"] = "Analista de Calidad", ["ja"] = "品質アナリスト",
            ["de"] = "Qualitätsanalyst", ["fr"] = "Analyste Qualité",
        },
        ["UI/UX Designer"] = new()
        {
            ["zh"] = "UI/UX 设计师", ["es"] = "Diseñador UI/UX", ["ja"] = "UI/UX デザイナー",
            ["de"] = "UI/UX-Designer", ["fr"] = "Designer UI/UX",
        },
        ["HR Executive"] = new()
        {
            ["zh"] = "人力资源专员", ["es"] = "Ejecutivo de RR.HH.", ["ja"] = "人事担当者",
            ["de"] = "HR-Sachbearbeiter", ["fr"] = "Chargé RH",
        },
        ["HR Manager"] = new()
        {
            ["zh"] = "人力资源经理", ["es"] = "Gerente de RR.HH.", ["ja"] = "人事部長",
            ["de"] = "HR-Manager", ["fr"] = "Responsable RH",
        },
        ["Accountant"] = new()
        {
            ["zh"] = "会计", ["es"] = "Contador", ["ja"] = "経理担当",
            ["de"] = "Buchhalter", ["fr"] = "Comptable",
        },
        ["Sales Executive"] = new()
        {
            ["zh"] = "销售专员", ["es"] = "Ejecutivo de Ventas", ["ja"] = "営業担当",
            ["de"] = "Vertriebsmitarbeiter", ["fr"] = "Chargé de Ventes",
        },
        ["Marketing Executive"] = new()
        {
            ["zh"] = "市场专员", ["es"] = "Ejecutivo de Marketing", ["ja"] = "マーケティング担当",
            ["de"] = "Marketing-Sachbearbeiter", ["fr"] = "Chargé Marketing",
        },
    };

    public static List<Department> BuildDepartments(string companyId, IEnumerable<string>? languages, string? createdBy = null)
    {
        var langs = NormalizeLanguages(languages);
        var now = DateTime.UtcNow;
        return DefaultDepartments
            .Select(label => new Department
            {
                CompanyId = companyId,
                IsActive = true,
                IsDeleted = false,
                CreatedBy = createdBy ?? string.Empty,
                CreatedDate = now,
                Titles = BuildTitles(label, langs),
            })
            .ToList();
    }

    public static List<JobTitles> BuildJobTitles(string companyId, IEnumerable<string>? languages, IReadOnlyDictionary<string, string> departmentIds, string? createdBy = null)
    {
        var langs = NormalizeLanguages(languages);
        var now = DateTime.UtcNow;
        return DefaultJobTitles
            .Select(item => new JobTitles
            {
                CompanyId = companyId,
                IsActive = true,
                IsDeleted = false,
                CreatedBy = createdBy ?? string.Empty,
                CreatedDate = now,
                DepartmentId = departmentIds.TryGetValue(item.Department, out var departmentId) ? departmentId : null,
                Titles = BuildTitles(item.Title, langs),
            })
            .ToList();
    }

    private static List<string> NormalizeLanguages(IEnumerable<string>? languages)
    {
        var langs = (languages ?? [])
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();
        if (langs.Count == 0) langs.Add(Languages.English);
        return langs;
    }

    private static List<MultilingualModel> BuildTitles(string englishLabel, List<string> languages)
    {
        Translations.TryGetValue(englishLabel, out var perLang);
        return languages.Select(lang => new MultilingualModel
        {
            Language = lang,
            Label = lang == Languages.English
                ? englishLabel
                : (perLang != null && perLang.TryGetValue(lang, out var translated) ? translated : englishLabel),
        }).ToList();
    }
}
