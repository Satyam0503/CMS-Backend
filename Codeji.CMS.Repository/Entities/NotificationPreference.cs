using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using static Codeji.CMS.Utility.Enums.EnumsHelper;


namespace Codeji.CMS.Repository.Entities;

public class NotificationPreference : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string UserId { get; set; }

    [BsonDictionaryOptions(DictionaryRepresentation.Document)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Dictionary<NotificationPreferenceType, bool> Preferences { get; set; } = new();
}
