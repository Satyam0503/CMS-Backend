using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttendanceStatus
{
  P = 1,
  A = 2,
  HD = 3,
  // Kept for existing records that used the old generic Leave status.
  L = 4,
  SL = 5,
  CL = 6,
  EL = 7,
  WFH = 8,
  ED = 9,
  LHD = 10,
  [JsonStringEnumMemberName("WFH+WFO")]
  WFH_WFO = 11,
  [JsonStringEnumMemberName("COMP-OFF")]
  COMP_OFF = 12,
  [JsonStringEnumMemberName("CL-HALF")]
  CL_HALF = 13,
  [JsonStringEnumMemberName("SL-HALF")]
  SL_HALF = 14,
  [JsonStringEnumMemberName("WFH-HD")]
  WFH_HD = 15
}
