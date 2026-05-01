using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttendanceStatus
{
  P = 1,
  A = 2,
  H = 3,
  L = 4
}