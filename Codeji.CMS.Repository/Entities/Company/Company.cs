using System;
using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.Repository.Entities.Company
{
    // Company documents evolve independently across tenants. Retired branding fields
    // (for example AppTheme) must not prevent authentication or tenant reads.
    [BsonIgnoreExtraElements]
    public class Company : Codeji.CMS.GenericRepository.Interfaces.ISupportAuditing, Codeji.CMS.GenericRepository.Interfaces.ISupportSoftDelete
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string CompanyId { get; set; }
        public bool Status { get; set; }
        public required string CompanyName { get; set; }
        public string CareerSlug { get; set; } = string.Empty;
        public bool CareerPortalEnabled { get; set; } = true;
        /// <summary>Company-wide master switch for former-employee self-service access.</summary>
        public bool FormerEmployeePortalEnabled { get; set; } = true;
        // Company-owned visual identity for both authenticated and public surfaces.
        public string AppTheme { get; set; } = "glass";
        public string PublicCompanyCode { get; set; } = string.Empty;
        public string EmployeeIdPrefix { get; set; } = string.Empty;
        public bool AutoGenerateEmployeeId { get; set; } = true;
        public bool PublishJobsToMasterPortal { get; set; } = false;
        public string? ExternalCareerUrl { get; set; }
        public string? CompanyLogo { get; set; }
        public string PrimaryContact { get; set; }
        public required string DefaultLanguage { get; set; }
        public List<string> ApplicationLanguage { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string Address { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsDeleted { get; set; }
        /// <summary>Tenant defaults for employee notification delivery. Personal preferences may opt out.</summary>
        public Dictionary<EnumsHelper.NotificationPreferenceType, bool> NotificationPreferences { get; set; } = new();
        public CompanyNotificationConfiguration NotificationConfiguration { get; set; } = new();
    }

    public sealed class CompanyNotificationConfiguration
    {
        public string Position { get; set; } = "top-right";
        public int AutoClose { get; set; } = 3500;
        public int Limit { get; set; } = 2;
        public bool NewestOnTop { get; set; } = true;
        public bool PauseOnHover { get; set; } = true;
        public bool ShowCloseButton { get; set; } = true;
        public bool CloseOnClick { get; set; }
        public string Transition { get; set; } = "slide";
        public string Theme { get; set; } = "system";
    }
}

