using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Company.Models
{
    public class Team
    {
        public string ID { get; set; }
        public string Name { get; set; }

        public Employee Leader { get; set; }
        public List<Employee> Employees { get; set; }
    }
}
