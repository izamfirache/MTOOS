using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.Models
{
    public class ClassConstructor
    {
        public ClassConstructor()
        {
            Parameters = new List<MethodParameter>();
        }

        public List<MethodParameter> Parameters { get; set; }
    }
}
