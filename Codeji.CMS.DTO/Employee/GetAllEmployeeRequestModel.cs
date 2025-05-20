using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codeji.CMS.DTO.Employee
{
    public class GetAllEmployeeRequestModel
    {
        public string Name { get; set; }
        public List <string> DepartmentId { get; set;}
    }
}
