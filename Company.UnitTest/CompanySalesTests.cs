using Company.BLL;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Company.UnitTest
{
    public class CompanySalesTests
    {
        [Test]
        public void ComputeProfitForYearTest()
        {
            var companySales = new CompanySales();
            var result = companySales.ComputeProfitForYear(2001);
            Assert.AreEqual(24750, result);
        }

        [Test]
        public void GetTotalTaxesSumForAllYearsTest()
        {
            var companySales = new CompanySales();
            var result = companySales.GetTotalTaxesSumForAllYears();
            Assert.AreEqual(2000, result);
        }
    }
}