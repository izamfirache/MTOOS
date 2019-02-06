using Company.BLL;
using Company.DAL;
using Company.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Company.BLL
{
    public class CompanyBll
    {
        private CompanyDal _companyDal;
        private List<int> _computetSalaries;
        private int MaxLimitForSalary;
        public CompanyBll()
        {
            _companyDal = new CompanyDal();
            InitializeSalariesList();
            MaxLimitForSalary = 5500;
        }

        public void ComputeSalariesBasedOnPerformances()
        {
            var companyMembers = _companyDal.GetCompanyMembers();
            foreach(CompanyMember cm in companyMembers)
            {
                var salary = 3000;
                var memberPerformances = _companyDal.GetPerformancesForCompanyMember(cm.ID);
                foreach(Performance p in memberPerformances)
                {
                    salary = salary + p.Points;

                    if (salary >= MaxLimitForSalary)
                    {
                        break;
                    }
                }

                _computetSalaries.Add(salary);
                _companyDal.SetCompanyMemberSalary(cm.ID, salary);

                SetupSomeGlobalStuff();
            }
        }

        private void SetupSomeGlobalStuff()
        {
            throw new NotImplementedException();
        }

        public int GetTotalForSalaries()
        {
            var total = 0;
            foreach(int salary in _computetSalaries)
            {
                total = total + salary;
            }

            return total;
        }

        public CompanyMember GetCompanyMemberInfo(string memberID)
        {
            return _companyDal.GetCompanyMember(memberID);
        }

        private void InitializeSalariesList()
        {
            _computetSalaries = new List<int>();
        }
    }
}