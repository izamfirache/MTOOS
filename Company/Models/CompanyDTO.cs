using Company.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Company.Models
{
    public class CompanyDTO
    {
        public string ID { get; set; }
        public string Name { get; set; }

        public Director Director { get; set; }
        public List<Team> Teams { get; set; }

        public List<TaxYearInfo> TaxYears { get; set; }
    }
}
