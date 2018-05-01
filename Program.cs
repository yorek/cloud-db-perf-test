using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MathNet.Numerics.Statistics;
using DBLoadTest.Workers;

namespace DBLoadTest
{
    class Program
    {
        static void Main(string[] args)
        {                 
            Console.WriteLine("Loading payload...");
            var json = JObject.Parse(File.ReadAllText(@"payload.json"));
            string payload = json.ToString(Formatting.None, null);
            
            var startAt = DateTime.Now;
            startAt = startAt.AddMilliseconds(-startAt.Millisecond);
            startAt = startAt.AddSeconds(10);
            Console.WriteLine($"Load will start at: {startAt.ToUniversalTime()}");

            Console.WriteLine("Setting up environment...");
            int workerCount = 1;
            var threads = new List<Thread>(workerCount);
            CountdownEvent startEvent = new CountdownEvent(workerCount);
            CountdownEvent runEvent = new CountdownEvent(workerCount);
            CountdownEvent finishEvent = new CountdownEvent(workerCount);
            List<SyncRunResult> allResults = new List<SyncRunResult>();
            object lockHolder = new Object();

            //var w = new AzureSQLWorker();            
            var w = new CosmosWorker();
            w.SetUp(1000, payload);

            Console.WriteLine("Creating threads...");
            for (int workerIndex = 0; workerIndex < workerCount; workerIndex++)
            {
                SyncRunner sr = new SyncRunner(workerIndex, w, startAt, payload, startEvent, runEvent, finishEvent);
                
                Thread t = new Thread(() => { 
                    var srr = sr.RunTest();
                    lock (lockHolder) {
                        allResults.Add(srr);
                        }
                    });
                
                threads.Add(t);
            }

            Console.WriteLine($"Starting tasks ({workerCount})...");
            threads.ForEach(t => t.Start());
            startEvent.Wait();
            Console.WriteLine("Tasks started and put to sleep.");

            Console.WriteLine($"Waiting tasks to enter run state (@ {startAt.ToUniversalTime()})...");
            runEvent.Wait();
            
            Console.WriteLine("Tasks now running.");            
            Console.WriteLine("Waiting for tasks to finish...");
            finishEvent.Wait();

            Console.WriteLine("Done.");

            int totalDocs = 0;
            double totalMilliseconds = 0;
            allResults.ForEach( r => { totalDocs += r.Documents; totalMilliseconds += r.ElapsedMilliseconds; } );
            double avgMilliseconds = totalMilliseconds / workerCount;

            Console.WriteLine($"Total Documents: {totalDocs}");
            Console.WriteLine($"Average Elapsed Seconds: {(avgMilliseconds/1000D):#.00} sec");
            Console.WriteLine($"Overall Performances: {totalDocs/(avgMilliseconds/1000D):#.00} msg/sec");

            Console.WriteLine("Done.");
        }
    }

    public class SyncRunResult
    {
        public int Documents;
        public double ElapsedMilliseconds;
    }

    public class SyncRunner {
        private int _workerId;
        private IWorker _worker;
        private DateTime _startAt;
        private CountdownEvent _startEvent;
        private CountdownEvent _runEvent;
        private CountdownEvent _finishEvent;
        
        public SyncRunner(int workerId, IWorker worker, DateTime startAt, string payload, CountdownEvent startEvent, CountdownEvent runEvent, CountdownEvent finishEvent)
        {
            _workerId = workerId;

            _worker = worker;

            _startAt = startAt;
            _startEvent = startEvent;
            _runEvent = runEvent;
            _finishEvent = finishEvent;
        }

        public SyncRunResult RunTest() 
        {
            var waitTime = _startAt - DateTime.Now;
            _startEvent.Signal();

            Thread.Sleep((int)(waitTime.TotalMilliseconds));
            _runEvent.Signal();

            var wr = _worker.Run(_workerId);            

            double totalElapsed = 0;
            wr.InsertPerformance.ForEach(i => { totalElapsed += i; });
            var sample = new DescriptiveStatistics(wr.InsertPerformance.ToArray());

            Console.WriteLine($"{_workerId:000}: MIN/AVG/MAX: {sample.Minimum:#.00}, {sample.Mean:#.00}, {sample.Maximum:#.00} - STDEV/VAR: {sample.StandardDeviation:#.00}, {sample.Variance:#.00} - SUM: {totalElapsed}");
        
            _finishEvent.Signal();

            return new SyncRunResult() { Documents = wr.InsertedDocuments, ElapsedMilliseconds = totalElapsed };    
        }       
    }    
}
