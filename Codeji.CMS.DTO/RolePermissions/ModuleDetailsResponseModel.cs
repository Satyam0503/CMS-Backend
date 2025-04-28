using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codeji.CMS.DTO.RolePermissions
{
    public class AllModuleDetailsResponseModel
    {
        //public string _id { get; set; }
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public bool Current_Status { get; set; }

    }
}
