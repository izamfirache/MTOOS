using Company.DAL;
using Company.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Company.BLL
{
    public class CompanySales
    {
        private CompanyDal _companyDal;
        private TaxYearInfo _mostProfitableyear;
        private int MaxLimitForTaxes;
        public CompanySales()
        {
            _companyDal = new CompanyDal();
            MaxLimitForTaxes = 2000;
        }

        public int ComputeProfitForYear(int year)
        {
            var taxYear = _companyDal.GetTaxYear(year);
            var profitWithoutTaxes = taxYear.Incomes - taxYear.SpentMoney;
            var profitWithTaxes = profitWithoutTaxes / taxYear.TaxPercentage;

            return profitWithTaxes;
        }

        public int GetTotalTaxesSumForAllYears()
        {
            var mostProfitable = new TaxYearInfo();
            var taxYears = _companyDal.GetTaxYears();
            int taxesSum = 0;
            int max = 0;
            foreach (TaxYearInfo year in taxYears)
            {
                if (IsValidYear(year.Year))
                {
                    var profitWithoutTaxes = year.Incomes - year.SpentMoney;
                    var profitWithTaxes = profitWithoutTaxes / year.TaxPercentage;
                    if (profitWithoutTaxes != 0 && profitWithoutTaxes > max)
                    {
                        max = profitWithoutTaxes;
                        _mostProfitableyear = year;
                    }

                    taxesSum = taxesSum + profitWithTaxes;
                }
            }

            if (taxesSum < MaxLimitForTaxes || taxesSum == MaxLimitForTaxes)
            {
                return taxesSum;
            }
            else
            {
                return 2000;
            }
        }

        private bool IsValidYear(int year)
        {
            if (year == 2000 || year == 2001 || year == 2002)
                return true;
            else
                return false;
        }
    }
}