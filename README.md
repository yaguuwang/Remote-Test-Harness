# Remote Test Harness

The software application is built using more than one package, each implementing a specific requirement of the system. These applications may be varying from containing small packages for a small application to thousands of packages for a large single application. Testing is required to ensure the quality of the application as the developers can find out about different bugs about the code they developed, so that they can be able to resolve the issues. Testing process can be automated as the number of tests to be performed might be increasing over the development period of the application and the human assistance for these tests might be difficult to provide.

Test Harness is very useful when there are large systems that needs testing continuously. It provides the environment to run the tests automatically and provides support for continuous integration. Test Harness Server provides automated testing which can perform the tests automatically when Clients provide the test requests and the code that needs to be tested. It does not have any idea about the tests it had to perform, so the test drivers need to be provided for the code that needs to be tested. It performs all the tests and logs the result for each test and sends the results back to the client.

The process it does the work is Clients provides the Test Request which contains the packages that needs to be tested, the Test Harness sends the package details to the Build Server to build the executables required for the testing purpose of the Test Harness. Build Server responds with the executables when they are ready. The Test Harness performs the tests on isolated environment. Then it stores the results in the Repository Server which can be queried by the Client and other users any point later to the test execution. Most of the simple testing systems just produce a report. Test Harness Server maintains a database of all test results, and make it possible to browse the results of all runs, and then drill down to the diagnostic output for a particular test. Additionally, there will be a Query Logger where we can get the information related to specific tests rather than the entire Log.