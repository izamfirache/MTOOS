using Company.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Company.BLL
{
    public class CompanyHR
    {
        public CompanyDal _companyDal;
        public CompanyHR()
        {
            _companyDal = new CompanyDal();
        }

        public bool AddNewEmployee(string employeeID, string employeeName, string teamName)
        {
            if (_companyDal.AddNewEmployee(employeeID, employeeName, teamName))
            {
                return true;
            }

            return false;
        }

        public bool FireEmployee(string employeeID)
        {
            if (_companyDal.RemoveEmployee(employeeID))
            {
                return true;
            }

            return false;
        }
    }
}
