using Company.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Company.DAL
{
    public class CompanyDal
    {
        public CompanyDTO SimulatedCompany;
        public List<CompanyMember> CompanyMembers;
        public CompanyDal()
        {
            CreateSimulatedCompany();
            CompanyMembers = PopulateCompanyMembersList();
        }

        private List<CompanyMember> PopulateCompanyMembersList()
        {
            List<CompanyMember> members = new List<CompanyMember>();
            foreach (Team team in SimulatedCompany.Teams)
            {
                members.AddRange(team.Employees);
                members.Add(team.Leader);
            }
            members.Add(SimulatedCompany.Director);

            return members;
        }

        public Director GetCompanyDirector()
        {
            return SimulatedCompany.Director;
        }

        public List<Team> GetCompanyTeams()
        {
            return SimulatedCompany.Teams;
        }

        public Team GetTeam(string teamID)
        {
            return SimulatedCompany.Teams.Where(team => team.ID == teamID).FirstOrDefault();
        }

        public Employee GetEmployee(string teamID, string employeeID)
        {
            var team = SimulatedCompany.Teams.Where(t => t.ID == teamID).FirstOrDefault();
            return team.Employees.Where(e => e.ID == employeeID).FirstOrDefault();
        }

        public List<CompanyMember> GetCompanyMembers()
        {
            List<CompanyMember> members = new List<CompanyMember>();
            foreach (Team team in SimulatedCompany.Teams)
            {
                members.AddRange(team.Employees);
            }
            members.Add(SimulatedCompany.Director);

            return members;
        }

        public CompanyMember GetCompanyMember(string memberID)
        {
            List<CompanyMember> members = new List<CompanyMember>();
            foreach (Team team in SimulatedCompany.Teams)
            {
                members.AddRange(team.Employees);
            }
            members.Add(SimulatedCompany.Director);

            return members.Where(m => m.ID == memberID).FirstOrDefault();
        }

        public List<Performance> GetPerformancesForCompanyMember(string memberID)
        {
            List<CompanyMember> members = new List<CompanyMember>();
            foreach(Team team in SimulatedCompany.Teams)
            {
                members.AddRange(team.Employees);
            }
            members.Add(SimulatedCompany.Director);

            return members.Where(m => m.ID == memberID).FirstOrDefault().Performances;
        }

        public void SetCompanyMemberSalary(string memberID, int salary)
        {
            List<CompanyMember> members = new List<CompanyMember>();
            foreach (Team team in SimulatedCompany.Teams)
            {
                members.AddRange(team.Employees);
            }
            members.Add(SimulatedCompany.Director);

            var member = members.Where(m => m.ID == memberID).FirstOrDefault();
            member.Salary = salary;
        }

        public TaxYearInfo GetTaxYear(int year)
        {
            return SimulatedCompany.TaxYears.Where(ty => ty.Year == year).FirstOrDefault();
        }

        public List<TaxYearInfo> GetTaxYears()
        {
            return SimulatedCompany.TaxYears;
        }

        public bool AddNewEmployee(string employeeID, string employeeName, string teamName)
        {
            var team = SimulatedCompany.Teams.Where(t => t.Name == teamName).FirstOrDefault();

            var employee = new Employee()
            {
                ID = employeeID,
                Name = employeeName,
                Performances = new List<Performance>()
            };
            team.Employees.Add(employee);

            CompanyMembers.Add(employee);

            return true;
        }

        public bool CheckIfEmployeeExists(string employeeID)
        {
            return CompanyMembers.Any(m => m.ID == employeeID);
        }

        public bool RemoveEmployee(string employeeID)
        {
            var emp = CompanyMembers.Where(e => e.ID == employeeID).FirstOrDefault();
            return CompanyMembers.Remove(emp);
        }

        private void CreateSimulatedCompany()
        {
            SimulatedCompany = new CompanyDTO()
            {
                ID = "abc111",
                Name = "MyCompany",
                Director = new Director()
                {
                    ID = "0001",
                    Name = "John Doe",
                    Performances = new List<Performance>()
                    {
                        new Performance(){Points = 20, Description = "profit"},
                        new Performance(){Points = 3, Description = "tasks"},
                        new Performance(){Points = 40, Description = "profit"}
                    }
                },
                Teams = new List<Team>()
                {
                    new Team()
                    {
                        ID = "team0001",
                        Name = "Avengers",
                        Leader = new Employee()
                        {
                            ID = "111",
                            Name = "Emp Leader",
                            Performances = new List<Performance>()
                            {
                                new Performance(){Points = 43, Description = "profit"},
                                new Performance(){Points = 32, Description = "tasks"},
                                new Performance(){Points = 4, Description = "profit"}
                            }
                        },
                        Employees = new List<Employee>()
                        {
                            new Employee()
                            {
                                ID = "112",
                                Name = "Emp1",
                                Performances = new List<Performance>()
                                {
                                    new Performance(){Points =4, Description = "profit"},
                                    new Performance(){Points = 32, Description = "tasks"},
                                    new Performance(){Points = 41, Description = "profit"}
                                }
                            },
                            new Employee()
                            {
                                ID = "112",
                                Name = "Emp2",
                                Performances = new List<Performance>()
                                {
                                    new Performance(){Points = 4, Description = "profit"},
                                    new Performance(){Points = 132, Description = "tasks"},
                                    new Performance(){Points = 4, Description = "profit"}
                                }
                            },
                            new Employee()
                            {
                                ID = "113",
                                Name = "Emp3",
                                Performances = new List<Performance>()
                                {
                                    new Performance(){Points = 43, Description = "profit"},
                                    new Performance(){Points = 32, Description = "tasks"},
                                    new Performance(){Points = 43, Description = "profit"}
                                }
                            }
                        }
                    },
                    new Team()
                    {
                        ID = "team0002",
                        Name = "Warriors",
                        Leader = new Employee()
                        {
                            ID = "115",
                            Name = "Emp Leader3",
                            Performances = new List<Performance>()
                            {
                                new Performance(){Points = 43, Description = "profit"},
                                new Performance(){Points = 32, Description = "tasks"},
                                new Performance(){Points = 4, Description = "profit"}
                            }
                        },
                        Employees = new List<Employee>()
                        {
                            new Employee()
                            {
                                ID = "116",
                                Name = "Emp6",
                                Performances = new List<Performance>()
                                {
                                    new Performance(){Points =4, Description = "profit"},
                                    new Performance(){Points = 32, Description = "tasks"},
                                    new Performance(){Points = 41, Description = "profit"}
                                }
                            },
                            new Employee()
                            {
                                ID = "117",
                                Name = "Emp7",
                                Performances = new List<Performance>()
                                {
                                    new Performance(){Points = 466, Description = "profit"},
                                    new Performance(){Points = 132, Description = "tasks"},
                                    new Performance(){Points = 41, Description = "profit"}
                                }
                            },
                            new Employee()
                            {
                                ID = "118",
                                Name = "Emp8",
                                Performances = new List<Performance>()
                                {
                                    new Performance(){Points = 1, Description = "profit"},
                                    new Performance(){Points = 32, Description = "tasks"},
                                    new Performance(){Points = 12, Description = "profit"}
                                }
                            }
                        }
                    },
                    new Team()
                    {
                        ID = "team0003",
                        Name = "Challengers",
                        Leader = new Employee()
                        {
                            ID = "121",
                            Name = "Emp21",
                            Performances = new List<Performance>()
                            {
                                new Performance(){Points = 3, Description = "profit"},
                                new Performance(){Points = 232, Description = "tasks"},
                                new Performance(){Points = 4, Description = "profit"}
                            }
                        },
                        Employees = new List<Employee>()
                        {
                            new Employee()
                            {
                                ID = "122",
                                Name = "Emp22",
                                Performances = new List<Performance>()
                                {
                                    new Performance(){Points =4, Description = "profit"},
                                    new Performance(){Points = 32, Description = "tasks"},
                                    new Performance(){Points = 41, Description = "profit"}
                                }
                            },
                            new Employee()
                            {
                                ID = "132",
                                Name = "Emp32",
                                Performances = new List<Performance>()
                                {
                                    new Performance(){Points = 41, Description = "profit"},
                                    new Performance(){Points = 132, Description = "tasks"},
                                    new Performance(){Points = 14, Description = "profit"}
                                }
                            },
                            new Employee()
                            {
                                ID = "124",
                                Name = "Emp24",
                                Performances = new List<Performance>()
                                {
                                    new Performance(){Points = 413, Description = "profit"},
                                    new Performance(){Points = 32, Description = "tasks"},
                                    new Performance(){Points = 413, Description = "profit"}
                                }
                            }
                        }
                    }
                },
                TaxYears = new List<TaxYearInfo>()
                {
                    new TaxYearInfo()
                    {
                        Year = 2000,
                        SpentMoney = 100000000,
                        Incomes = 100009000,
                        TaxPercentage = 3
                    },
                    new TaxYearInfo()
                    {
                        Year = 2001,
                        SpentMoney = 200000000,
                        Incomes = 200099000,
                        TaxPercentage = 4
                    },
                    new TaxYearInfo()
                    {
                        Year = 2002,
                        SpentMoney = 150000000,
                        Incomes = 150099000,
                        TaxPercentage = 7
                    }
                }
            };
        }
    }
}