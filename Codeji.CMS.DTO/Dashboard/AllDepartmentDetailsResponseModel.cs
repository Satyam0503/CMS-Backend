using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codeji.CMS.DTO.Dashboard
{
    public class AllDepartmentDetailsResponseModel
    {
        public List<MultilingualModel> Titles { get; set; }
        public string DepartmentId { get; set; }
        public int EmployeeCount { get; set; }
    }
}
