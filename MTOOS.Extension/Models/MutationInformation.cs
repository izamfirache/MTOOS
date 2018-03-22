using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.Models
{
    public class MutationInformation
    {
        public string ClassName { get; set; }
        public string MutantName { get; set; }
        public string OriginalProgramCode { get; set; }
        public string MutatedCode { get; set; }
    }
}
