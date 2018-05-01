using System.Collections.Generic;

namespace DBLoadTest.Workers
{
    public class WorkerResults
    {
        public int InsertedDocuments { get; }

        public List<double> InsertPerformance { get; }

        public WorkerResults(int insertedDocuments, List<double> insertPerformance)
        {
            InsertedDocuments = insertedDocuments;
            InsertPerformance = insertPerformance;
        }
    }

    public interface IWorker
    {    
        void SetUp(int documentsToInsert, string documentPayload);

        WorkerResults Run(int workerId);

        void TearDown();
    }
}