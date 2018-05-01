using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using Dapper;

namespace DBLoadTest.Workers
{
    public class AzureSQLWorker: IWorker
    {
        private string _connectionString;
        private string _tableName = "dbo.TestTable";
        private int _documentsToInsert;
        private string _documentPayload;
        
        public void SetUp(int documentsToInsert, string documentPayload)
        {
            _connectionString = "Server=tcp:dmmssqlsrv.database.windows.net,1433;Initial Catalog=perftest;Persist Security Info=False;User ID=damauri;Password=Passw0rd!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30";
            _documentsToInsert = documentsToInsert;
            _documentPayload = documentPayload;

            Console.WriteLine("Cleaning test table...");
            using (SqlConnection conn = new SqlConnection($"{_connectionString}"))
            {
                int isMemoryOptimized = conn.ExecuteScalar<int>($"SELECT is_memory_optimized FROM sys.tables WHERE [object_id] = object_id('{_tableName}')");

                if (isMemoryOptimized == 1)
                {
                    conn.Execute($"DELETE FROM {_tableName}");
                } else
                {
                    conn.Execute($"TRUNCATE TABLE {_tableName}");
                }                
            }
        }

        public WorkerResults Run(int workerId) 
        {
            string workerConnectionString = String.Format($"{_connectionString};Application Name=DB-Load-Worker-{workerId:000}");

            int insertedDocuments = 0;
            List<double> recordedPerformances = new List<double>(_documentsToInsert);
            Stopwatch stopWatch = new Stopwatch();    
            
            stopWatch.Start();
            using (SqlConnection conn = new SqlConnection(workerConnectionString))
            {
                while (insertedDocuments < _documentsToInsert)
                {
                    stopWatch.Restart();
                    conn.ExecuteScalar($"INSERT INTO {_tableName} (Payload) VALUES(@payload)", new { payload = _documentPayload });
                    stopWatch.Stop();
                    recordedPerformances.Add(stopWatch.ElapsedMilliseconds);
                    insertedDocuments += 1;
                }
            }        

            return new WorkerResults(insertedDocuments, recordedPerformances);
        }

        public void TearDown()
        {
        }
    }
}