using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.Models
{
    public class UnitTestMutationResult
    {
        public List<GeneratedMutant> GeneratedUnitTestMutants { get; set; }
        public string OutputPath { get; set; }
    }
}
