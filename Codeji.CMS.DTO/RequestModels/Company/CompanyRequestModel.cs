using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codeji.CMS.DTO.RequestModels.Company
{
    public class CompanyRequestModel
    {
        public required string CompanyName { get; set; }
      
        public required string FirstName { get; set; }
       
        public required string LastName { get; set; }
        
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
