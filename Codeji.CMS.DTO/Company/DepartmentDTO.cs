using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codeji.CMS.DTO.RequestModels.Company
{
    public class DepartmentDTO
    {
        public string? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public bool IsActive {get;set;}
        public List<MultilingualModel> Titles {get;set;}

    }
}
