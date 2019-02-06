using Company.BLL;
using Company.DAL;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Company.UnitTest
{
    public class CompanyBllTests
    {
        [Test]
        public void ComputeSalariesBasedOnPerformanceTest()
        {
            var companyBll = new CompanyBll();
            companyBll.ComputeSalariesBasedOnPerformances();
            var result = companyBll.GetTotalForSalaries();

            Assert.AreEqual(32218, result);
        }

        [Test]
        public void GetCompanyMemberInfoTest()
        {
            var companyBll = new CompanyBll();
            var memberInfo = companyBll.GetCompanyMemberInfo("112");

            Assert.AreEqual(memberInfo.ID, "112");
            Assert.AreEqual(memberInfo.Name, "Emp1");
        }
    }
}