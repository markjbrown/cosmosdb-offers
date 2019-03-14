using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace ChangeOfferNetCore
{
    class Program
    {

        static void Main(string[] args)
        {
            MyOffer offer = new MyOffer();

            offer.CreateDatabaseWithOffer().GetAwaiter().GetResult();
            offer.CreateCollectionWithDbSharedOffer().GetAwaiter().GetResult();
            offer.UpdateCollectionOffer().GetAwaiter().GetResult();

            offer.UpdateDatabaseOffer().GetAwaiter().GetResult();

        }
    }

    class MyOffer
    {
        private string endpoint;
        private string key;
        private string databaseName;
        private string collectionName;
        private Uri databaseUri;
        private Uri collectionUri;
        private DocumentClient client;

        public MyOffer()
        {
            endpoint = "myendpoint";
            key = "mykey";
            databaseName = "dbthroughput";
            collectionName = "collection1";
            databaseUri = UriFactory.CreateDatabaseUri(databaseName);
            collectionUri = UriFactory.CreateDocumentCollectionUri(databaseName, collectionName);

            ConnectionPolicy policy = new ConnectionPolicy
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp
            };

            client = new DocumentClient(new Uri(endpoint), key, policy);
        }

        public async Task CreateDatabaseWithOffer()
        {
            Database myDatabase = new Database
            {
                Id = this.databaseName
            };

            await client.CreateDatabaseIfNotExistsAsync(
                myDatabase,
                new RequestOptions { OfferThroughput = 1000 });
        }

        public async Task UpdateDatabaseOffer()
        {

            Database database = await client.ReadDatabaseAsync(this.databaseUri);

            //Get the current offer
            Offer offer = client.CreateOfferQuery()
                            .Where(r => r.ResourceLink == database.SelfLink)
                            .AsEnumerable()
                            .SingleOrDefault();
              

            // Set the throughput to 5000 request units per second
            offer = new OfferV2(offer, 5000);

            // Now persist these changes to the database by replacing the original resource
            await client.ReplaceOfferAsync(offer);
        }

        public async Task CreateCollectionWithDbSharedOffer()
        {
            DocumentCollection myCollection = new DocumentCollection
            {
                Id = this.collectionName
            };
            myCollection.PartitionKey.Paths.Add("/deviceId");

            await client.CreateDocumentCollectionAsync(
                this.databaseUri,
                myCollection);
        }

        public async Task UpdateCollectionOffer()
        {
            DocumentCollection collection = await client.ReadDocumentCollectionAsync(this.collectionUri);
            
            // Fetch the resource to be updated
            // For a updating throughput for a set of containers, replace the collection's self link with the database's self link
            Offer offer = client.CreateOfferQuery()
                            .Where(r => r.ResourceLink == collection.SelfLink)
                            .AsEnumerable()
                            .SingleOrDefault();

            // Set the throughput to 5000 request units per second
            offer = new OfferV2(offer, 5000);

            // Now persist these changes to the database by replacing the original resource
            await client.ReplaceOfferAsync(offer);
        }
    }

}
