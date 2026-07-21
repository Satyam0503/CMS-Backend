using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

public class AttendanceStatusCodeSerializer : SerializerBase<string>
{
    private static readonly Dictionary<int,string> Legacy = new() { [1]="P", [2]="A", [3]="HD", [4]="L", [5]="SL", [6]="CL", [7]="EL", [8]="WFH", [9]="ED", [10]="LHD", [11]="WFH+WFO", [12]="COMP-OFF", [13]="CL-HALF", [14]="SL-HALF", [15]="WFH-HD" };
    public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => context.Reader.GetCurrentBsonType() switch
    {
        BsonType.String => context.Reader.ReadString(),
        BsonType.Int32 => Legacy.GetValueOrDefault(context.Reader.ReadInt32(), "P"),
        _ => throw new FormatException("Unsupported attendance status format.")
    };
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value) => context.Writer.WriteString(value);
}
