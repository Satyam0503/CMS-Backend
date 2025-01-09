using System;
using System.Net.NetworkInformation;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Utility.Constraints
{


    public class EnumsBindList
    {
        public string Name { get; set; }
        public int Value { get; set; }

    }
    public static class StaticData
    {
        public static readonly List<EnumsBindList> StatusList = Enum.GetValues(typeof(StatusEnums))
        .Cast<EnumsBindList>().ToList();
        public static readonly List<EnumsBindList> ActivityTypeList = Enum.GetValues(typeof(ActivityTypeEnums))
       .Cast<EnumsBindList>().ToList();


    }
}

