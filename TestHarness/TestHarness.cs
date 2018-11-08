/////////////////////////////////////////////////////////////////////
// TestHarness.cs - TestHarness Engine: creates child domains      //
// ver 1.1                                                         //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * TestHarness package provides integration testing services.  It:
 * - receives structured test requests
 * - retrieves cited files from a repository
 * - executes tests on all code that implements an ITest interface,
 *   e.g., test drivers.
 * - reports pass or fail status for each test in a test request
 * - stores test logs in the repository
 * It contains classes:
 * - TestHarness that runs all tests in child AppDomains
 * - Callback to support sending messages from a child AppDomain to
 *   the TestHarness primary AppDomain.
 * - Test and RequestInfo to support transferring test information
 *   from TestHarness to child AppDomain
 * 
 * Required Files:
 * ---------------
 * - TestHarness.cs, BlockingQueue.cs
 * - ITest.cs
 * - LoadAndTest, Logger, Messages
 *
 * Maintanence History:
 * --------------------
 * ver 1.1 : 11 Nov 2016
 * - added ability for test harness to pass a load path to
 *   LoadAndTest instance in child AppDomain
 * ver 1.0 : 16 Oct 2016
 * - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Security.Policy;    // defines evidence needed for AppDomain construction
using System.Runtime.Remoting;   // provides remote communication between AppDomains
using System.Xml;
using System.Xml.Linq;

namespace TestHarness
{
  ///////////////////////////////////////////////////////////////////
  // Callback class is used to receive messages from child AppDomain
  //
  public class Callback : MarshalByRefObject, ICallback
  {
    public void sendMessage(Message message)
    {
      RLog.flush();
      DLog.write("\n  received msg from childDomain: \"" + message.body + "\"");
    }
  }
  ///////////////////////////////////////////////////////////////////
  // Test and RequestInfo are used to pass test request information
  // to child AppDomain
  //
  [Serializable]
  class Test : ITestInfo
  {
    public string testName { get; set; }
    public List<string> files { get; set; } = new List<string>();
  }
  [Serializable]
  class RequestInfo : IRequestInfo
  {
    public List<ITestInfo> requestInfo { get; set; } = new List<ITestInfo>();
  }
  ///////////////////////////////////////////////////////////////////
  // class TestHarness

  public class TestHarness : ITestHarness
  {
    public SWTools.BlockingQueue<Message> inQ_ { get; set; } = new SWTools.BlockingQueue<Message>();
    private ICallback cb_;
    private IRepository repo_;
    private IClient client_;
    private string localDir_;
    private string repoPath_ = "../../../Repository/RepositoryStorage/";
    private string filePath_;
    //private string loaderPath_;

    public TestHarness(IRepository repo)
    {
      DLog.write("\n  creating instance of TestHarness");
      repo_ = repo;
      repoPath_ = System.IO.Path.GetFullPath(repoPath_);
      cb_ = new Callback();
    }
    //----< called by TestExecutive >--------------------------------

    public void setClient(IClient client)
    {
      client_ = client;
    }
    //----< called by clients >--------------------------------------

    public void sendTestRequest(Message testRequest)
    {
      RLog.write("\n  TestHarness received a testRequest - Req #2");
      inQ_.enQ(testRequest);
    }
    //----< not used for Project #2 >--------------------------------

    public Message sendMessage(Message msg)
    {
      return msg;
    }
    //----< make path name from author and time >--------------------

    string makeKey(string author)
    {
      DateTime now = DateTime.Now;
      string nowDateStr = now.Date.ToString("d");
      string[] dateParts = nowDateStr.Split('/');
      string key = "";
      foreach (string part in dateParts)
        key += part.Trim() + '_';
      string nowTimeStr = now.TimeOfDay.ToString();
      string[] timeParts = nowTimeStr.Split(':');
      for(int i = 0; i< timeParts.Count() - 1; ++i)
        key += timeParts[i].Trim() + '_';
      key += timeParts[timeParts.Count() - 1];
      key = author + "_" + key;
      return key;
    }
    //----< retrieve test information from testRequest >-------------

    List<ITestInfo> extractTests(Message testRequest)
    {
      DLog.write("\n  parsing test request");
      List<ITestInfo> tests = new List<ITestInfo>();
      XDocument doc = XDocument.Parse(testRequest.body);
      foreach (XElement testElem in doc.Descendants("test"))
      {
        Test test = new Test();
        string testDriverName = testElem.Element("testDriver").Value;
        test.testName = testElem.Attribute("name").Value;
        test.files.Add(testDriverName);
        foreach (XElement lib in testElem.Elements("library"))
        {
          test.files.Add(lib.Value);
        }
        tests.Add(test);
      }
      return tests;
    }
    //----< retrieve test code from testRequest >--------------------

    List<string> extractCode(List<ITestInfo> testInfos)
    {
      DLog.write("\n  retrieving code files from testInfo data structure");
      List<string> codes = new List<string>();
      foreach (ITestInfo testInfo in testInfos)
        codes.AddRange(testInfo.files);
      return codes;
    }
    //----< create local directory and load from Repository >--------

    RequestInfo processRequestAndLoadFiles(Message testRequest)
    {
      RequestInfo rqi = new RequestInfo();
      rqi.requestInfo = extractTests(testRequest);
      List<string> files = extractCode(rqi.requestInfo);

      localDir_ = makeKey(testRequest.author);            // name of temporary dir to hold test files
      filePath_ = System.IO.Path.GetFullPath(localDir_);  // LoadAndTest will use this path
      DLog.write("\n  creating local test directory \"" + localDir_ + "\"");
      System.IO.Directory.CreateDirectory(localDir_);

      DLog.write("\n  loading code from Repository");
      foreach (string file in files)
      {
        string name = System.IO.Path.GetFileName(file);
        string src = System.IO.Path.Combine(repoPath_, file);
        if (System.IO.File.Exists(src))
        {
          string dst = System.IO.Path.Combine(localDir_, name);
          System.IO.File.Copy(src, dst, true);
          DLog.write("\n    retrieved file \"" + name + "\"");
        }
        else
        {
          DLog.write("\n    could not retrieve file \"" + name + "\"");
        }
      }
      DLog.putLine();
      return rqi;
    }
    //----< save results and logs in Repository >--------------------

    bool saveResultsAndLogs(ITestResults testResults)
    {
      string logName = testResults.testKey + ".txt";
      System.IO.StreamWriter sr = null;
      try
      {
        sr = new System.IO.StreamWriter(System.IO.Path.Combine(repoPath_, logName));
        sr.WriteLine(logName);
        foreach (ITestResult test in testResults.testResults)
        {
          sr.WriteLine("-----------------------------");
          sr.WriteLine(test.testName);
          sr.WriteLine(test.testResult);
          sr.WriteLine(test.testLog);
        }
        sr.WriteLine("-----------------------------");
      }
      catch
      {
        sr.Close();
        return false;
      }
      sr.Close();
      return true;
    }
    //----< run tests >----------------------------------------------
    /*
     * In Project #4 this function becomes the thread proc for
     * each child AppDomain thread.
     */
    ITestResults runTests(Message testRequest)
    {
      RequestInfo rqi = processRequestAndLoadFiles(testRequest);

      AppDomain ad = createChildAppDomain();
      ILoadAndTest ldandtst = installLoader(ad);

      ITestResults tr = null;
      if (ldandtst != null)
      {
        //ldandtst.setCallback(cb_);
        tr = ldandtst.test(rqi);
        tr.testKey = localDir_;

        DLog.flush();
        //DLog.pause(true);
        RLog.putLine();
        RLog.write("\n  test results are:");
        RLog.write("\n  - test Identifier: " + tr.testKey);
        RLog.write("\n  - test DateTime:   " + tr.dateTime);
        foreach (ITestResult test in tr.testResults)
        {
          RLog.write("\n  --------------------------------------");
          RLog.write("\n    test name:   " + test.testName);
          RLog.write("\n    test result: " + test.testResult);
          RLog.write("\n    test log:    " + test.testLog);
        }
        RLog.write("\n  --------------------------------------");
      }
      RLog.putLine();
      RLog.flush();
      //DLog.pause(false);
      if (saveResultsAndLogs(tr))
      {
        RLog.write("\n  saved test results and logs in Repository - Req #6, Req #7\n");
      }
      else
      {
        RLog.write("\n  failed to save test results and logs in Repository\n");
      }
      DLog.putLine();
      DLog.write("\n  removing test directory \"" + localDir_ + "\"");
      try
      {
        System.IO.Directory.Delete(localDir_, true);
      }
      catch (Exception ex)
      {
        DLog.write("\n  could not remove directory");
        DLog.write("\n  " + ex.Message);
      }
      // unloading ChildDomain, and so unloading the library

      DLog.write("\n  unloading: \"" + ad.FriendlyName + "\"\n");
      AppDomain.Unload(ad);
      DLog.stop();
      return tr;
    }
    //----< make TestResults Message >-------------------------------

    Message makeTestResultsMessage(ITestResults tr)
    {
      Message trMsg = new Message();
      trMsg.author = "TestHarness";
      trMsg.to = "CL";
      trMsg.from = "TH";
      XDocument doc = new XDocument();
      XElement root = new XElement("testResultsMsg");
      doc.Add(root);
      XElement testKey = new XElement("testKey");
      testKey.Value = tr.testKey;
      root.Add(testKey);
      XElement timeStamp = new XElement("timeStamp");
      timeStamp.Value = tr.dateTime.ToString();
      root.Add(timeStamp);
      XElement testResults = new XElement("testResults");
      root.Add(testResults);
      foreach(ITestResult test in tr.testResults)
      {
        XElement testResult = new XElement("testResult");
        testResults.Add(testResult);
        XElement testName = new XElement("testName");
        testName.Value = test.testName;
        testResult.Add(testName);
        XElement result = new XElement("result");
        result.Value = test.testResult;
        testResult.Add(result);
        XElement log = new XElement("log");
        log.Value = test.testLog;
        testResult.Add(log);
      }
      trMsg.body = doc.ToString();
      return trMsg;
    }
    //----< main activity of TestHarness >---------------------------

    public void processMessages()
    {
      AppDomain main = AppDomain.CurrentDomain;
      DLog.write("\n  Starting in AppDomain " + main.FriendlyName + "\n");

      while (true)
      {
        if (inQ_.size() == 0)  // won't use this in Project #4
          break;
        Message testRequest = inQ_.deQ();

        RLog.write("\n  dequeuing testRequest: - Req #3");
        RLog.write("\n  processing TestRequest: - Req #2");
        DLog.write("\n" + testRequest.body.formatXml(4));

        ITestResults tr = runTests(testRequest);
        client_.sendResults(makeTestResultsMessage(tr));
      }
    }
    //----< was used for debugging >---------------------------------

    void showAssemblies(AppDomain ad)
    {
      Assembly[] arrayOfAssems = ad.GetAssemblies();
      foreach (Assembly assem in arrayOfAssems)
        DLog.write("\n  " + assem.ToString());
    }
    //----< create child AppDomain >---------------------------------

    public AppDomain createChildAppDomain()
    {
      try
      {
        DLog.flush();
        RLog.write("\n  creating child AppDomain - Req #5");

        AppDomainSetup domaininfo = new AppDomainSetup();
        domaininfo.ApplicationBase
          = "file:///" + System.Environment.CurrentDirectory;  // defines search path for LoadAndTest library

        //Create evidence for the new AppDomain from evidence of current

        Evidence adevidence = AppDomain.CurrentDomain.Evidence;

        // Create Child AppDomain

        AppDomain ad
          = AppDomain.CreateDomain("ChildDomain", adevidence, domaininfo);

        DLog.write("\n  created AppDomain \"" + ad.FriendlyName + "\"");
        return ad;
      }
      catch (Exception except)
      {
        RLog.write("\n  " + except.Message + "\n\n");
      }
      return null;
    }
    //----< Load and Test is responsible for testing >---------------

    ILoadAndTest installLoader(AppDomain ad)
    {
      ad.Load("LoadAndTest");
      //showAssemblies(ad);
      //Console.WriteLine();

      // create proxy for LoadAndTest object in child AppDomain

      ObjectHandle oh
        = ad.CreateInstance("LoadAndTest", "TestHarness.LoadAndTest");
      object ob = oh.Unwrap();    // unwrap creates proxy to ChildDomain
                                  // Console.Write("\n  {0}", ob);

      // set reference to LoadAndTest object in child

      ILoadAndTest landt = (ILoadAndTest)ob;

      // create Callback object in parent domain and pass reference
      // to LoadAndTest object in child

      landt.setCallback(cb_);
      landt.loadPath(filePath_);  // send file path to LoadAndTest
      return landt;
    }
#if (TEST_TESTHARNESS)
    static void Main(string[] args)
    {
    }
#endif
  }
}
