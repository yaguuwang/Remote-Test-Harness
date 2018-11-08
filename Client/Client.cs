/////////////////////////////////////////////////////////////////////
// Client.cs - sends TestRequests, displays results                //
//                                                                 //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * Almost no functionality now.  Will be expanded to make 
 * Queries into Repository for Logs and Libraries.
 * 
 * Required Files:
 * - Client.cs, ITest.cs, Logger.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 20 Oct 2016
 * - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestHarness
{
  public class Client : IClient
  {
    public SWTools.BlockingQueue<string> inQ_ { get; set; }
    private ITestHarness th_ = null;
    private IRepository repo_ = null;

    public Client(ITestHarness th)
    {
      DLog.write("\n  Creating instance of Client");
      th_ = th;
    }
    public void setRepository(IRepository repo)
    {
      repo_ = repo;
    }

    public void sendTestRequest(Message testRequest)
    {
      th_.sendTestRequest(testRequest);
    }
    public void sendResults(Message results)
    {
      RLog.write("\n  Client received results message:");
      RLog.write("\n  " + results.ToString());
      RLog.putLine();
    }
    public void makeQuery(string queryText)
    {
      RLog.write("\n  Results of client query for \"" + queryText + "\"");
      if (repo_ == null)
        return;
      List<string> files = repo_.queryLogs(queryText);
      RLog.write("\n  first 10 reponses to query \"" + queryText + "\"");
      for (int i = 0; i < 10; ++i)
      {
        if (i == files.Count())
          break;
        RLog.write("\n  " + files[i]);
      }
    }
#if (TEST_CLIENT)
    static void Main(string[] args)
    {
      /*
       * ToDo: add code to test 
       * - Add code in Client class to make queries into Repository for
       *   information about libraries and logs.
       * - Add code in Client class to sent files to Repository.
       * - Add ClientTest class that implements ITest so Client
       *   functionality can be tested in TestHarness.
       */
    }
#endif
  }
}
