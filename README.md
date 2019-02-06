# MTOOS

Mutation Testing for Object Oriented Software.

Visual Studio Extension which applies the *Mutation Testing* technique on an existing pair of source code project (in C#) and a unit test suite (NUnit). 

The general purpose of this tool is to be an alternative for the classical unit test code coverage tools, which function by the idea that if a pice of code is executed during the execution of a unit test, that code area is considered to be tested (covered). That is not always the case, one can obtain a high unit test code coverage but not testing much about the code. This tool modifies key areas in your code and see if those modifications (mutations) are cought by the test suite.

The tool generates mutants (modified versions of the source code), then run the unit tests (for that modified/mutated area in the code) over those mutants. 
   - If at least one unit test is failing when the unit tests are executed over a mutant, that mutant is said to be *KILLED*, meaning that area in the source code is properly tested by at least one unit test. 
   - If no unit test is failing, that mutant is said to be *LIVE*, meaning that area in the code is not properly covered in a unit test.
   - in this tool, the mutants are generated at class level, for each class, a mutant is generated.
   - in the case of a *LIVE* mutant, using this tool, the developer can compare the mutant with the original source code and see exactly the line of code that is not covered in the unit tests.
    
Examples of mutations performed by this tool:
1. Modify boundary operators
   - ex: replace < with >, <= with >
2. Modify equality operators
   - ex: replace == with !=
3. Modify if conditions
   - ex: replace if(a == 4 and someComplexCondition) with if(true) or if(false)
4. Modify math operators
   - ex: replace + with -, * with /
5. Modify assignment expressions and variable declarations
   - ex: replace a = ComputeSomething() with a = someRandomValueOfThatType (even complex/custom types) or with null
6. Modify return expressions
   - ex: replace return a; with return somethingRandomOfThatType; (even complex/custom types) or null
7. Remove void method calls
   - ex: remove calls like CallAVoidMethodThatAffectsSomeState()
8. Modify *this* statements
   - ex: replace this.a = 4 with this.a = someRandomValueOfThatType (even complex/custom types)
    
The tool can be used to evaluate the quality of the unit test suite by computing the *MUTATION SCORE* which is equal to *NrOfKilledMutans/TotalNrOfMutans*. A mutation score of 1 means that all the mutants were killed by the unit test suite and the source code is properly tested based on the mutation testing performed by this tool.
