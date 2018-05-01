using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft;
using Newtonsoft.Json;

namespace DBLoadTest.Workers
{
    public class CosmosWorker : IWorker
    {
        private const string _endpointUrl = "https://dmcosmos.documents.azure.com:443/";
        private const string _primaryKey = "YAopQ0edHWK9v8yV7IpCU1WzvFQkPvpHWDGmjhpXC0swlmibZgHkgqVDiTRG3abFM2PfYoWKPOVFjL7OTJOPsA==";

        private const string _databaseName = "testdb1";
        private const string _collectionName = "testcollection1";

        private int _documentsToInsert;
        private string _documentPayload;

        public void SetUp(int documentsToInsert, string documentPayload)
        {
            _documentsToInsert = documentsToInsert;
            _documentPayload = documentPayload;

            //var client = new DocumentClient(new Uri(_endpointUrl), _primaryKey);
            //try
            //{
            //    var options = new RequestOptions
            //    {
            //        OfferThroughput = 15000
            //    };
            //    var result = client.DeleteDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(_databaseName, _collectionName), options).Result;
            //    Console.WriteLine(result.StatusCode);
            //} catch {}

            //client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(_databaseName), new DocumentCollection() { Id = _collectionName });
        }

        public WorkerResults Run(int workerId)
        {
            int insertedDocuments = 0;
            List<double> recordedPerformances = new List<double>(_documentsToInsert);
            Stopwatch stopWatch = new Stopwatch();

            dynamic d = JsonConvert.DeserializeObject<dynamic>(_documentPayload);
            
            stopWatch.Start();
            var policy = new ConnectionPolicy()
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp
            };
            using (var client = new DocumentClient(new Uri(_endpointUrl), _primaryKey, policy))
            {
                client.OpenAsync().Wait();

                while (insertedDocuments < _documentsToInsert)
                {
                    d["id"] = Guid.NewGuid();

                    stopWatch.Restart();
                    var result = client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri("testdb1", "testcollection1"), d).GetAwaiter().GetResult();
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