using Company.BLL;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Company.UnitTest
{
    public class CompanyHrTests
    {
        [Test]
        public void AddNewEmployeeTest()
        {
            var companyHr = new CompanyHR();
            var ID = string.Format("{0}", new Random().Next(1000, 2000));
            var result = companyHr.AddNewEmployee(ID, "johny doey", "Avengers");

            Assert.AreEqual(true, result);
            var addedEmployee = companyHr._companyDal.GetCompanyMember(ID);

            Assert.AreEqual(addedEmployee.Name, "johny doey");
            Assert.AreEqual(addedEmployee.ID, ID);
        }

        [Test]
        public void FireEmployeeTest()
        {
            var companyHr = new CompanyHR();
            var result = companyHr.FireEmployee("115");

            Assert.AreEqual(true, result);
            //Assert.AreEqual(companyHr._companyDal.CheckIfEmployeeExists("115"), false));
        }
    }
}