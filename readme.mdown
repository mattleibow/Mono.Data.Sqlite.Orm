
Overview
===

Mono.Data.Sqlite.Orm is an open source library to allow .NET and Mono applications to store data in [SQLite 3 databases](http://www.sqlite.org). It should work in any CLI environment.

Mono.Data.Sqlite.Orm was designed as a quick and convenient database layer. Its design follows from these *goals*:

* It should be very easy to integrate with existing projects.
  
* It is a thin wrapper over SQLite and should be fast and efficient. (The library should not be the performance bottleneck of your queries.)
  
* It provides very simple methods for executing queries safely (using parameters) and for retrieving the results of those query in a strongly typed fashion.
  
* It works with your data model without forcing you to change your classes. (Contains a small reflection-driven ORM layer.)
  
* It dependends only on Mono.Data.Sqlite on all platforms except Windows Phone which also requires [C# SQLite](http://code.google.com/p/csharp-sqlite)

* Support for creating entire databases using only compact `Attributes`

To do:

* Support `IQueryable` for constructing queries. You can of course use LINQ on the results of queries, but you cannot use it to construct the queries yet.


Documentation
===

The library contains simple attributes that you can use to control the construction of tables. In a simple stock program, you might use:

    public class Stock
    {
    	[PrimaryKey, AutoIncrement]
    	public int Id { get; set; }
    	[MaxLength(8)]
    	public string Symbol { get; set; }
    }

    public class Valuation
    {
    	[PrimaryKey, AutoIncrement]
    	public int Id { get; set; }
    	[Indexed("IX_StockId")]
    	public int StockId { get; set; }
    	public DateTime Time { get; set; }
    	public decimal Price { get; set; }
    }

With these, you can automatically generate tables (and update) in your database by calling `CreateTable`:

    var db = new SqliteSession("database.sqlite");
    db.CreateTable<Stock>();
    db.CreateTable<Valuation>();

You can insert rows in the database using `Insert`. If the table contains an auto-incremented primary key, then the value for that key will be available to you after the insert:

    public static void AddStock(SqliteSession db, string symbol) 
	{
    	var s = db.Insert(new Stock() 
		{
			Symbol = symbol
    	});
    	Console.WriteLine("{0} == {1}", s.Symbol, s.Id);
    }

You can query the database using the `Query` method of `SqliteSession`:

    public static IEnumerable<Valuation> QueryValuations (SqliteSession db, Stock stock)
    {
    	return db.Query<Valuation> ("select * from Valuation where StockId = ?", stock.Id);
    }

The generic parameter to the `Query` method specifies the type of object to create for each row. It can be one of your table classes, or any other class whose public properties match the column returned by the query. For instance, we could rewrite the above query as:

    public class Val 
	{
    	public decimal Money { get; set; }
    	public DateTime Date { get; set; }
    }
    public static IEnumerable<Val> QueryVals (SqliteSession db, Stock stock)
    {
    	return db.Query<Val> ("select 'Price' as 'Money', 'Time' as 'Date' from Valuation where StockId = ?", stock.Id);
    }

Updates must be performed manually using the `Execute` method of `SqliteSession`.

Meta
===

This library is based on [Frank A. Krueger's](mailto:fak@praeclarum.org) [sqlite-net](https://github.com/praeclarum/sqlite-net) library.

This is an open source project that welcomes contributions/suggestions/bug reports from those who use it. If you have any ideas on how to improve the library, please contact [Matthew Leibowitz](mailto:mattleibowmail@gmail.com).

 
# Failing Unit Tests

Zero length arrays read as null
Fails on Windows Phone & Silverlight
http://code.google.com/p/csharp-sqlite/issues/detail?id=149
ByteArrayTest.EmptyByteArraySavedAndRetrievedCorrectlyTest

Excetions ar silently swallowed
Fails on Windows Phone & Silverlight
http://code.google.com/p/csharp-sqlite/issues/detail?id=130
InsertTest.InsertWithExtra

Inserting a UInt32.MaxValue into a BigInt column fails
Fails on NetFx, Metro, Mono for Android and MonoTouch
https://bugzilla.xamarin.com/show_bug.cgi?id=4289
https://github.com/mono/mono/pull/438
ColumnTypesTest.ColumnsSaveLoadMaxCorrectly
ColumnTypesTest.ColumnsSaveLoadRandomValuesCorrectly

Creating a Unique Index within a Transaction throw Exception
Fails on Windows Phone & Silverlight
http://code.google.com/p/csharp-sqlite/issues/detail?id=150
CreateTableTest.CreateIndexedTable

No virtual table support 
Fails on Windows Phone & Silverlight
https://code.google.com/p/csharp-sqlite/issues/detail?id=171
CreateVirtualTableTest.*
