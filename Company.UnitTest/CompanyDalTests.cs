using Company.DAL;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Company.UnitTest
{
    public class CompanyDalTests
    {
        [Test]
        public void CreateSimulatedCompanyTest()
        {
            var companyDal = new CompanyDal();
            Assert.AreEqual(true, companyDal.SimulatedCompany != null);
            Assert.AreEqual(true, companyDal.SimulatedCompany.Teams.Count != 0);
        }

        [Test]
        public void PopulateCompanyMembersListTest()
        {
            var companyDal = new CompanyDal();
            Assert.AreEqual(true, companyDal.CompanyMembers != null);
            //Assert.AreEqual(true, companyDal.CompanyMembers.Count != 0);
        }
    }
}