using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Company;

public class LeaveSettings : BaseClass
{
    [BsonId]
    public string Id { get; set; }
    [Range(1, 12)]
    public int FinancialYearStartMonth { get; set; }
}
