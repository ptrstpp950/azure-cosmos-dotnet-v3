//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace CosmosBenchmark
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Bogus;
    using Bogus.DataSets;
    using Microsoft.Azure.Cosmos;

    internal class InsertV3BenchmarkOperation : IBenchmarkOperation
    {
        private readonly Container container;
        private readonly string partitionKeyPath;
        private readonly Dictionary<string, object> sampleJObject;

        private readonly string databaseName;
        private readonly string containerName;

        public InsertV3BenchmarkOperation(
            CosmosClient cosmosClient,
            string dbName,
            string containerName,
            string partitionKeyPath,
            string sampleJson)
        {
            this.databaseName = dbName;
            this.containerName = containerName;

            this.container = cosmosClient.GetContainer(this.databaseName, this.containerName);
            this.partitionKeyPath = partitionKeyPath.Replace("/", "");

            this.sampleJObject = new Dictionary<string, object>();
        }

        public async Task<OperationResult> ExecuteOnceAsync()
        {
            using (MemoryStream input = JsonHelper.ToStream(this.sampleJObject))
            {
                ResponseMessage itemResponse = await this.container.CreateItemStreamAsync(
                        input,
                        new PartitionKey(this.sampleJObject[this.partitionKeyPath].ToString()));

                double ruCharges = itemResponse.Headers.RequestCharge;

                global::System.Buffers.ArrayPool<byte>.Shared.Return(input.GetBuffer());

                return new OperationResult()
                {
                    DatabseName = this.databaseName,
                    ContainerName = this.containerName,
                    RuCharges = ruCharges,
                    CosmosDiagnostics = itemResponse.Diagnostics,
                    LazyDiagnostics = () => itemResponse.Diagnostics.ToString(),
                };
            }
        }

        public Task PrepareAsync()
        {
            string[] fruit = new[] { "apple", "banana", "orange", "strawberry", "kiwi" };

            Faker<Order> testOrders = new Faker<Order>()
                //Ensure all properties have rules. By default, StrictMode is false
                //Set a global policy by using Faker.DefaultStrictMode
                .StrictMode(true)
                //OrderId is deterministic
                .RuleFor(o => o.OrderId, f => f.IndexGlobal)
                //Pick some fruit from a basket
                .RuleFor(o => o.Item, f => f.PickRandom(fruit))
                //A random quantity from 1 to 10
                .RuleFor(o => o.Quantity, f => f.Random.Number(1, 10));

            Faker<User> testUsers = new Faker<User>()
                //Optional: Call for objects that have complex initialization
                .RuleFor(u => u.Id, f => f.IndexGlobal)
                //Use an enum outside scope.
                .RuleFor(u => u.Gender, f => f.PickRandom<Name.Gender>())
                //Basic rules using built-in generators
                .RuleFor(u => u.FirstName, (f, u) => f.Name.FirstName(u.Gender))
                .RuleFor(u => u.LastName, (f, u) => f.Name.LastName(u.Gender))
                .RuleFor(u => u.Avatar, f => f.Internet.Avatar())
                .RuleFor(u => u.UserName, (f, u) => f.Internet.UserName(u.FirstName, u.LastName))
                .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
                //Use a method outside scope.
                .RuleFor(u => u.CartId, f => Guid.NewGuid())
                //Compound property with context, use the first/last name properties
                .RuleFor(u => u.FullName, (f, u) => u.FirstName + " " + u.LastName)
                //And composability of a complex collection.
                .RuleFor(u => u.Orders, f => testOrders.Generate(3).ToList());

            string newPartitionKey = Guid.NewGuid().ToString();
            this.sampleJObject["id"] = Guid.NewGuid().ToString();
            this.sampleJObject[this.partitionKeyPath] = newPartitionKey;
            this.sampleJObject["user"] = testUsers.Generate();
            return Task.CompletedTask;
        }

        public class Order    {
            public int OrderId { get; set; }
            public string Item { get; set; }
            public int Quantity { get; set; }
        }


        public class User {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string FullName { get; set; }
            public string UserName { get; set; }
            public string UserNameLower => this.UserName.ToLower();
            public string Email { get; set; }
            public string Avatar { get; set; }
            public Guid CartId { get; set; }
            public string SSN { get; set; }
            public Name.Gender Gender { get; set; }
            public List<Order> Orders { get; set; }
        }
    }
}
