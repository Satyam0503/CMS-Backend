using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codeji.CMS.DTO.RolePermissions
{
    public class AllModuleDetailsResponseModel
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public string ModuleConstant { get; set; }
        public bool IsAccessible { get; set; }

    }
}
