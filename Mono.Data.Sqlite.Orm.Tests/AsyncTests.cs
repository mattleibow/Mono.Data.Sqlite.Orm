using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Mono.Data.Sqlite.Orm.ComponentModel;
#if SILVERLIGHT || MS_TEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestFixtureAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
using TestAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
#elif NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using TestFixtureAttribute = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using TestAttribute = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;

#endif

#if NETFX_CORE
using Windows.System.Threading;
#endif

namespace Mono.Data.Sqlite.Orm.Tests
{
    // @mbrit - 2012-05-14 - NOTE - the lack of async use in this class is because the VS11 test runner falsely
    // reports any failing test as succeeding if marked as async. Should be fixed in the "June 2012" drop...

    public class Customer
    {
        [AutoIncrement]
        [PrimaryKey]
        public int Id { get; set; }

        [MaxLength(64)]
        public string FirstName { get; set; }

        [MaxLength(64)]
        public string LastName { get; set; }

        [MaxLength(64)]
        public string Email { get; set; }
    }

    /// <summary>
    /// Defines tests that exercise async behaviour.
    /// </summary>
    [TestFixture]
    public class AsyncTests
    {
        private Customer CreateCustomer()
        {
            var customer = new Customer { FirstName = "foo", LastName = "bar", Email = Guid.NewGuid().ToString() };
            return customer;
        }

        [Test]
        public void StressAsync()
        {
            var globalConn = OrmAsyncTestSession.GetConnection();

            globalConn.CreateTableAsync<Customer>().Wait();

            int threadCount = 0;
            var doneEvent = new AutoResetEvent(false);
            const int n = 500;
            var errors = new List<string>();
            for (int i = 0; i < n; i++)
            {
#if NETFX_CORE
                ThreadPool.RunAsync(
#else
                ThreadPool.QueueUserWorkItem(
#endif
                    delegate
                        {
                            try
                            {
                                var conn = OrmAsyncTestSession.GetConnection(globalConn.ConnectionString);
                                var obj = new Customer { FirstName = i.ToString() };

                                conn.InsertAsync(obj).Wait();
                                if (obj.Id == 0)
                                {
                                    lock (errors)
                                    {
                                        errors.Add("Bad Id");
                                    }
                                }

                                var query = from c in conn.Table<Customer>() where c.Id == obj.Id select c;
                                if (query.ToListAsync().Result.FirstOrDefault() == null)
                                {
                                    lock (errors)
                                    {
                                        errors.Add("Failed query");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                lock (errors)
                                {
                                    errors.Add(ex.Message);
                                }
                            }

                            threadCount++;
                            if (threadCount == n)
                            {
                                doneEvent.Set();
                            }
                        });
            }
            doneEvent.WaitOne();

            int count = globalConn.Table<Customer>().CountAsync().Result;

            Assert.AreEqual(0, errors.Count);
            Assert.AreEqual(n, count);
        }

        [Test]
        public void TestAsyncTableElementAtAsync()
        {
            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();
            conn.ExecuteAsync("delete from customer").Wait();

            // create...
            for (int index = 0; index < 10; index++)
            {
                Customer customer = this.CreateCustomer();
                customer.FirstName = index.ToString();
                conn.InsertAsync(customer).Wait();
            }

            // query...
            TableQuery<Customer> query = conn.Table<Customer>().OrderBy(v => v.FirstName);
            Task<Customer> task = query.ElementAtAsync(7);
            task.Wait();
            Customer loaded = task.Result;

            // check...
            Assert.AreEqual("7", loaded.FirstName);
        }

        [Test]
        public void TestAsyncTableOrderBy()
        {
            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();
            conn.ExecuteAsync("delete from customer").Wait();

            // create...
            for (int index = 0; index < 10; index++)
            {
                conn.InsertAsync(this.CreateCustomer()).Wait();
            }

            // query...
            TableQuery<Customer> query = conn.Table<Customer>().OrderBy(v => v.Email);
            Task<List<Customer>> task = query.ToListAsync();
            task.Wait();
            List<Customer> items = task.Result;

            // check...
            Assert.AreEqual(-1, string.Compare(items[0].Email, items[9].Email));
        }

        [Test]
        public void TestAsyncTableOrderByDescending()
        {
            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();
            conn.ExecuteAsync("delete from customer").Wait();

            // create...
            for (int index = 0; index < 10; index++)
            {
                conn.InsertAsync(this.CreateCustomer()).Wait();
            }

            // query...
            TableQuery<Customer> query = conn.Table<Customer>().OrderByDescending(v => v.Email);
            Task<List<Customer>> task = query.ToListAsync();
            task.Wait();
            List<Customer> items = task.Result;

            // check...
            Assert.AreEqual(1, string.Compare(items[0].Email, items[9].Email));
        }

        [Test]
        public void TestAsyncTableQueryCountAsync()
        {
            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();
            conn.ExecuteAsync("delete from customer").Wait();

            // create...
            for (int index = 0; index < 10; index++)
            {
                conn.InsertAsync(this.CreateCustomer()).Wait();
            }

            // load...
            TableQuery<Customer> query = conn.Table<Customer>();
            Task<int> task = query.CountAsync();
            task.Wait();

            // check...
            Assert.AreEqual(10, task.Result);
        }

        [Test]
        public void TestAsyncTableQuerySkip()
        {
            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();
            conn.ExecuteAsync("delete from customer").Wait();

            // create...
            for (int index = 0; index < 10; index++)
            {
                Customer customer = this.CreateCustomer();
                customer.FirstName = index.ToString();
                conn.InsertAsync(customer).Wait();
            }

            // query...
            TableQuery<Customer> query = conn.Table<Customer>().OrderBy(v => v.FirstName).Skip(5);
            Task<List<Customer>> task = query.ToListAsync();
            task.Wait();
            List<Customer> items = task.Result;

            // check...
            Assert.AreEqual(5, items.Count);
            Assert.AreEqual("5", items[0].FirstName);
        }

        [Test]
        public void TestAsyncTableQueryTake()
        {
            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();
            conn.ExecuteAsync("delete from customer").Wait();

            // create...
            for (int index = 0; index < 10; index++)
            {
                Customer customer = this.CreateCustomer();
                customer.FirstName = index.ToString();
                conn.InsertAsync(customer).Wait();
            }

            // query...
            TableQuery<Customer> query = conn.Table<Customer>().OrderBy(v => v.FirstName).Take(1);
            Task<List<Customer>> task = query.ToListAsync();
            task.Wait();
            List<Customer> items = task.Result;

            // check...
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("0", items[0].FirstName);
        }

        [Test]
        public void TestAsyncTableQueryToListAsync()
        {
            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // create...
            Customer customer = this.CreateCustomer();
            conn.InsertAsync(customer).Wait();

            // query...
            TableQuery<Customer> query = conn.Table<Customer>();
            Task<List<Customer>> task = query.ToListAsync();
            task.Wait();
            List<Customer> items = task.Result;

            // check...
            Customer loaded = items.First(v => v.Id == customer.Id);
            Assert.AreEqual(customer.Email, loaded.Email);
        }

        [Test]
        public void TestAsyncTableQueryWhereOperation()
        {
            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // create...
            Customer customer = this.CreateCustomer();
            conn.InsertAsync(customer).Wait();

            // query...
            TableQuery<Customer> query = conn.Table<Customer>();
            Task<List<Customer>> task = query.ToListAsync();
            task.Wait();
            List<Customer> items = task.Result;

            // check...
            Customer loaded = items.First(v => v.Id == customer.Id);
            Assert.AreEqual(customer.Email, loaded.Email);
        }

        [Test]
        public void TestCreateTableAsync()
        {
            var conn = OrmAsyncTestSession.GetConnection();

            // drop the customer table...
            conn.ExecuteAsync("drop table if exists Customer").Wait();

            // run...
            conn.CreateTableAsync<Customer>().Wait();

            // check...
            using (var check = OrmAsyncTestSession.GetConnection(conn.ConnectionString))
            {
                // run it - if it's missing we'll get a failure...
                check.Execute("select * from Customer");
            }
        }

        [Test]
        public void TestDeleteAsync()
        {
            // create...
            Customer customer = this.CreateCustomer();

            // connect...
            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // run...
            conn.InsertAsync(customer).Wait();

            // delete it...
            conn.DeleteAsync(customer).Wait();

            // check...
            using (var check = OrmAsyncTestSession.GetConnection(conn.ConnectionString))
            {
                // load it back - should be null...
                List<Customer> loaded = check.Table<Customer>().Where(v => v.Id == customer.Id).ToList();
                Assert.AreEqual(0, loaded.Count);
            }
        }

        [Test]
        public void TestDropTableAsync()
        {
            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // drop it...
            conn.DropTableAsync<Customer>().Wait();

            // check...
            using (var check = OrmAsyncTestSession.GetConnection(conn.ConnectionString))
            {
                // load it back and check - should be missing
                Assert.IsFalse(check.TableExists<Customer>());
            }
        }

        [Test]
        public void TestExecuteAsync()
        {
            // connect...

            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // do a manual insert...
            string email = Guid.NewGuid().ToString();
            conn.ExecuteAsync("insert into customer (firstname, lastname, email) values (?, ?, ?)", "foo", "bar", email)
                .Wait();

            // check...
            using (var check = OrmAsyncTestSession.GetConnection(conn.ConnectionString))
            {
                // load it back - should be null...
                TableQuery<Customer> result = check.Table<Customer>().Where(v => v.Email == email);
                Assert.IsNotNull(result);
            }
        }

        [Test]
        public void TestExecuteScalar()
        {
            // connect...
            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            conn.Insert(new Customer { FirstName = "Hello" });

            // check...
            var task = conn.ExecuteScalarAsync<int>("SELECT Id FROM Customer WHERE FirstName = ?", "Hello");
            task.Wait();
            Assert.AreEqual(1, task.Result);
        }

        [Test]
        public void TestFindAsyncItemMissing()
        {
            // connect and insert...
            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // now get one that doesn't exist...
            Task<Customer> task = conn.FindAsync<Customer>(-1);
            task.Wait();

            // check...
            Assert.IsNull(task.Result);
        }

        [Test]
        public void TestFindAsyncItemPresent()
        {
            // create...
            Customer customer = this.CreateCustomer();

            // connect and insert...
            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();
            conn.InsertAsync(customer).Wait();

            // check...
            Assert.AreNotEqual(0, customer.Id);

            // get it back...
            Task<Customer> task = conn.GetAsync<Customer>(customer.Id);
            task.Wait();
            Customer loaded = task.Result;

            // check...
            Assert.AreEqual(customer.Id, loaded.Id);
        }

        [Test]
        public void TestGetAsync()
        {
            // create...
            var customer = this.CreateCustomer();

            // connect and insert...
            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();
            conn.InsertAsync(customer).Wait();

            // check...
            Assert.AreNotEqual(0, customer.Id);

            // get it back...
            Task<Customer> task = conn.GetAsync<Customer>(customer.Id);
            task.Wait();
            Customer loaded = task.Result;

            // check...
            Assert.AreEqual(customer.Id, loaded.Id);
        }

        [Test]
        public void TestInsertAllAsync()
        {
            // create a bunch of customers...
            var customers = new List<Customer>();
            for (int index = 0; index < 100; index++)
            {
                var customer = this.CreateCustomer();
                customers.Add(customer);
            }

            // connect...
            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // insert them all...
            conn.InsertAllAsync(customers).Wait();

            // check...
            using (var check = OrmAsyncTestSession.GetConnection(conn.ConnectionString))
            {
                foreach (Customer t in customers)
                {
                    // load it back and check...
                    var loaded = check.Get<Customer>(t.Id);
                    Assert.AreEqual(loaded.Email, t.Email);
                }
            }
        }

        [Test]
        public void TestInsertAsync()
        {
            // create...
            Customer customer = this.CreateCustomer();

            // connect...

            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // run...
            conn.InsertAsync(customer).Wait();

            // check that we got an id...
            Assert.AreNotEqual(0, customer.Id);

            // check...
            using (var check = OrmAsyncTestSession.GetConnection(conn.ConnectionString))
            {
                // load it back...
                var loaded = check.Get<Customer>(customer.Id);
                Assert.AreEqual(loaded.Id, customer.Id);
            }
        }

        [Test]
        public void TestQueryAsync()
        {
            // connect...
            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // insert some...
            var customers = new List<Customer>();
            for (int index = 0; index < 5; index++)
            {
                Customer customer = this.CreateCustomer();

                // insert...
                conn.InsertAsync(customer).Wait();

                // add...
                customers.Add(customer);
            }

            // return the third one...
            Task<List<Customer>> task = conn.QueryAsync<Customer>("select * from customer where id=?", customers[2].Id);
            task.Wait();
            List<Customer> loaded = task.Result;

            // check...
            Assert.AreEqual(1, loaded.Count);
            Assert.AreEqual(customers[2].Email, loaded[0].Email);
        }

        [Test]
        public void TestRunInTransactionAsync()
        {
            // connect...
            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // run...
            var customer = this.CreateCustomer();
            using (var trans = conn.BeginTransaction())
            {
                // insert...
                conn.InsertAsync(customer).Wait();

                // delete it again...
                conn.ExecuteAsync("delete from customer where id=?", customer.Id).Wait();

                trans.Commit();
            }

            // check...
            using (var check = OrmAsyncTestSession.GetConnection(conn.ConnectionString))
            {
                // load it back and check - should be deleted...
                List<Customer> loaded = check.Table<Customer>().Where(v => v.Id == customer.Id).ToList();
                Assert.AreEqual(0, loaded.Count);
            }
        }

        [Test]
        public void TestTableAsync()
        {
            // connect...
            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();
            conn.ExecuteAsync("delete from customer").Wait();

            // insert some...
            var customers = new List<Customer>();
            for (int index = 0; index < 5; index++)
            {
                var customer = this.CreateCustomer();

                // insert...
                conn.InsertAsync(customer).Wait();

                // add...
                customers.Add(customer);
            }

            // run the table operation...
            TableQuery<Customer> query = conn.Table<Customer>();
            List<Customer> loaded = query.ToListAsync().Result;

            // check that we got them all back...
            Assert.AreEqual(5, loaded.Count);
            Assert.IsNotNull(loaded.Where(v => v.Id == customers[0].Id));
            Assert.IsNotNull(loaded.Where(v => v.Id == customers[1].Id));
            Assert.IsNotNull(loaded.Where(v => v.Id == customers[2].Id));
            Assert.IsNotNull(loaded.Where(v => v.Id == customers[3].Id));
            Assert.IsNotNull(loaded.Where(v => v.Id == customers[4].Id));
        }

        [Test]
        public void TestUpdateAsync()
        {
            // create...
            Customer customer = this.CreateCustomer();

            // connect...

            var conn = OrmAsyncTestSession.GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // run...
            conn.InsertAsync(customer).Wait();

            // change it...
            string newEmail = Guid.NewGuid().ToString();
            customer.Email = newEmail;

            // save it...
            conn.UpdateAsync(customer).Wait();

            // check...
            using (var check = OrmAsyncTestSession.GetConnection(conn.ConnectionString))
            {
                // load it back - should be changed...
                var loaded = check.Get<Customer>(customer.Id);
                Assert.AreEqual(newEmail, loaded.Email);
            }
        }

        [Test]
        public void FirstAsyncTest()
        {
            var db = OrmAsyncTestSession.GetConnection();
            db.CreateTableAsync<Customer>().Wait();

            db.InsertAsync(new Customer { FirstName = "First" }).Wait();

            var firstAsync = db.Table<Customer>().FirstAsync();
            firstAsync.Wait();
            Assert.AreEqual("First", firstAsync.Result.FirstName);

            db.ClearTableAsync<Customer>().Wait();

            try
            {
                var task = db.Table<Customer>().FirstAsync();
                task.Wait();

                Assert.Fail();
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException as InvalidOperationException == null)
                {
                    throw;
                }
            }
        }

        [Test]
        public void FirstOrDefaultAsyncTest()
        {
            var db = OrmAsyncTestSession.GetConnection();
            db.CreateTableAsync<Customer>().Wait();

            db.InsertAsync(new Customer { FirstName = "First" }).Wait();

            var firstAsync = db.Table<Customer>().FirstOrDefaultAsync();
            firstAsync.Wait();
            Assert.AreEqual("First", firstAsync.Result.FirstName);

            db.ClearTableAsync<Customer>().Wait();

            var task = db.Table<Customer>().FirstOrDefaultAsync();
            task.Wait();
            Assert.IsNull(task.Result);
        }

        [Test]
        public void GetWithExpressionAsyncTest()
        {
            var db = OrmAsyncTestSession.GetConnection();

            db.CreateTableAsync<Customer>().Wait();

            db.InsertAsync(new Customer { FirstName = "A", Email = "a@a.a" }).Wait();

            var task = db.GetAsync<Customer>(x => x.FirstName == "A");
            task.Wait();

            Assert.AreEqual("a@a.a", task.Result.Email);
        }

    }
}