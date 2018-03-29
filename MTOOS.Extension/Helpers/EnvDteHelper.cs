using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTOOS.Extension.Helpers
{
    public class EnvDteHelper
    {
        public List<string> GetEnvDteProjectReferences(EnvDTE.Project project)
        {
            var vsproject = project.Object as VSLangProj.VSProject;
            var projectReferences = new List<string>();

            foreach (VSLangProj.Reference reference in vsproject.References)
            {
                projectReferences.Add(reference.Path);
            }

            return projectReferences;
        }
    }
}
