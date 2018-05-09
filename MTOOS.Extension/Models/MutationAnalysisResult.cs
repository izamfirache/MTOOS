using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.Models
{
    public class MutationAnalysisResult
    {
        public List<GeneratedMutant> LiveMutants { get; set; }
        public List<GeneratedMutant> GeneratedMutants { get; set; }
    }
}
