using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Codeji.CMS.Repository.Entities.Employees;

/// <summary>
/// Policy coverage is a calendar date, not an instant. Persist UTC midnight without
/// converting through the host timezone. Earlier India-hosted versions stored local
/// midnight as 18:30 UTC on the preceding day; recover that exact legacy encoding.
/// Approval timestamps continue to use the normal UTC timestamp serializer.
/// </summary>
public sealed class PayrollPolicyCalendarDateSerializer : SerializerBase<DateTime>
{
    public override DateTime Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var value = DateTimeOffset.FromUnixTimeMilliseconds(context.Reader.ReadDateTime()).UtcDateTime;
        if (value.TimeOfDay == new TimeSpan(18, 30, 0))
            value = value.AddMinutes(330);
        return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateTime value) =>
        context.Writer.WriteDateTime(new DateTimeOffset(DateTime.SpecifyKind(value.Date, DateTimeKind.Utc)).ToUnixTimeMilliseconds());
}

public sealed class NullablePayrollPolicyCalendarDateSerializer : SerializerBase<DateTime?>
{
    private static readonly PayrollPolicyCalendarDateSerializer CalendarDate = new();

    public override DateTime? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.GetCurrentBsonType() == BsonType.Null)
        {
            context.Reader.ReadNull();
            return null;
        }
        return CalendarDate.Deserialize(context, args);
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateTime? value)
    {
        if (value is null) context.Writer.WriteNull();
        else CalendarDate.Serialize(context, args, value.Value);
    }
}
