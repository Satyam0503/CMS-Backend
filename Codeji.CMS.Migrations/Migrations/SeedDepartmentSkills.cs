using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public class SeedDepartmentSkills : IMigration
{
    public string Id => $"2025-01-10-{typeof(SeedDepartmentSkills).Name}";

    // HR / People Operations
    private static readonly string[] HrSkills =
    [
        "Talent Acquisition", "Recruitment", "Sourcing", "Technical Recruiting",
        "Employee Onboarding", "Employee Offboarding", "Background Verification",
        "Performance Management", "Performance Appraisal", "Goal Setting (OKRs / KPIs)",
        "Compensation & Benefits", "Payroll Management", "Benefits Administration",
        "Employee Relations", "Conflict Resolution", "Grievance Handling",
        "Employee Engagement", "Employee Retention", "Exit Interviews",
        "Training & Development", "Learning & Development",
        "Workforce Planning", "Succession Planning", "Organizational Development",
        "HR Policies", "HR Analytics", "HR Operations",
        "Diversity, Equity & Inclusion", "Culture Building",
        "Labor Law", "Statutory Compliance", "POSH Compliance", "EEO Compliance",
        "HRIS", "HRMS", "ATS (Applicant Tracking Systems)",
        "Workday", "SAP SuccessFactors", "BambooHR", "Greenhouse", "Lever",
        "Zoho People", "Keka", "Darwinbox",
    ];

    // QA / Software Testing
    private static readonly string[] QaSkills =
    [
        "Manual Testing", "Automation Testing", "Test Automation Framework",
        "Test Case Design", "Test Plan", "Test Strategy",
        "Functional Testing", "Non-Functional Testing",
        "Regression Testing", "Smoke Testing", "Sanity Testing",
        "Exploratory Testing", "Risk-Based Testing", "Agile Testing",
        "API Testing", "UI Testing", "Database Testing",
        "Performance Testing", "Load Testing", "Stress Testing",
        "Mobile Testing", "Cross-Browser Testing", "Accessibility Testing", "Usability Testing",
        "Postman", "SoapUI", "JMeter", "LoadRunner", "Gatling",
        "TestNG", "Cucumber", "SpecFlow", "Appium", "Robot Framework",
        "Defect Tracking", "Bug Triage", "Test Reporting",
        "STLC (Software Testing Life Cycle)", "ISTQB", "Quality Assurance", "Quality Control",
        "Test Driven Development", "Behavior Driven Development",
        "Continuous Testing", "Test Data Management",
    ];

    // Finance / Accounting
    private static readonly string[] FinanceSkills =
    [
        "Financial Analysis", "Financial Modeling", "Financial Reporting",
        "Accounting", "Bookkeeping", "General Ledger", "Journal Entries",
        "Accounts Payable", "Accounts Receivable", "Bank Reconciliation",
        "Cost Accounting", "Management Accounting", "Cash Flow Management",
        "Working Capital Management", "Treasury Management",
        "Budgeting", "Forecasting", "Variance Analysis",
        "P&L Analysis", "Balance Sheet Analysis", "Financial Statements",
        "GAAP", "IFRS", "Indian Accounting Standards (Ind AS)",
        "Auditing", "Internal Audit", "External Audit", "Statutory Audit",
        "Taxation", "Direct Tax", "Indirect Tax", "GST", "TDS", "Income Tax",
        "Tax Planning", "Tax Compliance", "Transfer Pricing",
        "Risk Management", "Compliance", "KYC", "AML",
        "Investment Analysis", "Portfolio Management", "Equity Research",
        "Valuation", "DCF", "Mergers & Acquisitions", "Corporate Finance",
        "ERP Systems", "SAP FICO", "Oracle Financials", "Microsoft Dynamics 365 Finance",
        "QuickBooks", "Tally ERP", "Xero", "Zoho Books",
        "Advanced Excel", "Financial Modeling in Excel",
        "CA (Chartered Accountant)", "CPA", "CFA", "CMA", "ACCA",
    ];

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(m => m.Id == Id).AnyAsync()) return;

        var skills = db.GetCollection<Skills>(typeof(Skills).Name);

        var existingNames = (await skills
            .Find(FilterDefinition<Skills>.Empty)
            .Project(s => s.Name)
            .ToListAsync())
            .Select(n => n?.Trim().ToLowerInvariant())
            .Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet();

        var allDepartmentSkills = HrSkills
            .Concat(QaSkills)
            .Concat(FinanceSkills)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var newSkills = allDepartmentSkills
            .Where(name => !existingNames.Contains(name.Trim().ToLowerInvariant()))
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
