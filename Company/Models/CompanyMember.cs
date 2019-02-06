using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Company.Models
{
    public class CompanyMember
    {

        public string ID { get; set; }
        public string Name { get; set; }
        public List<Performance> Performances { get; set; }

        public int Salary { get; set; }
    }
}
