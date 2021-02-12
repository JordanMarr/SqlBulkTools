<img src="http://gregnz.com/images/SqlBulkTools/icon-large.png" alt="SqlBulkTools">  [![Build Status](https://travis-ci.org/tiagorosendo/SqlBulkTools.svg?branch=master)](https://travis-ci.org/tiagorosendo/SqlBulkTools)

SqlBulkTools features an easy to use fluent interface for performing SQL operations in C# or F#. Supports Bulk Insert, Update, Delete and Merge. Includes advanced features such as output identity, delete entities conditionally (for merging), exclude column(s) from update (for merging), single entity operations and plenty more. 

Please leave a Github star if you find this project useful.

#### Getting started

```dotnet add package SqlBulkTools.FSharp --version 0.2.0```

## F# Computation Expressions!

```open SqlBulkTools.FSharp``` 

### Bulk Insert
```fsharp
use t = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
let count =
    bulkInsert conn {
        for user in users do
        table "Users"
        column user.Id
        column row.FirstName
        column row.LastName
        column row.SSN
    } 
if count > 0 then t.Complete()
```

### Bulk Update
```fsharp
bulkUpdate conn {
    for row in rows do
    table (nameof ctx.Dbo.Orders)
    column row.Id
    column row.OrderDate
    column row.CustomerEmail
    column row.CustomerAddress
    column row.CustomerCity
    column row.CustomerState
    column row.CustomerZip
    matchTargetOn row.Id
}
```

### Bulk Upsert
```fsharp
bulkUpsert conn {
    for row in rows do
    table (nameof ctx.Dbo.Orders)
    column row.Id
    column row.OrderDate
    column row.CustomerEmail
    column row.CustomerAddress
    column row.CustomerCity
    column row.CustomerState
    column row.CustomerZip
    matchTargetOn row.Id
}
```

### Bulk Delete
```fsharp
bulkDelete conn {
    for sheet in deletedSheets do
    table "Sheets"
    column sheet.Id
    matchTargetOn sheet.Id
}
```

## Standard Library
You can use the underlying C# fluent API if the F# computation expression builders don't meet your needs:

```open SqlBulkTools```

```c#
using SqlBulkTools;

  // Mockable IBulkOperations and IDataTableOperations Interface.
  public class BookClub(IBulkOperations bulk, IDataTableOperations dtOps) {

  private IBulkOperations _bulk; // Use this for bulk and single entity operations 
  (e.g. Bulk Insert, Update, Merge, Delete)
  
  private IDataTableOperations _dtOps; // Use this for Data Table helper 
  
  public BookClub(IBulkOperations bulk) {
    _bulk = bulk;
    _dtOps = dtOps;
  }
    // .....
}

// Or simply new up an instance.
var bulk = new BulkOperations(); // for Bulk Tools. 
var dtOps = new DataTableOperations() // for Data Table Tools.

// ..... 

// The following examples are based on a cut down Book model

public class Book {
    public int Id { get; set; }
    public string Title { get; set; }
    public string ISBN { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; }
    public int WarehouseId { get; set; }
}

```

#### Note
---------------
You may need to update your references to include System.Transactions for TransactionScope as 
it's is not included by default.


### BulkInsert
---------------
```c#
var bulk = new BulkOperations();
books = GetBooks();

using (TransactionScope trans = new TransactionScope())
{
    using (SqlConnection conn = new SqlConnection(ConfigurationManager
    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
    {
        bulk.Setup<Book>()
            .ForCollection(books)
            .WithTable("Books")
            .AddAllColumns()
            .BulkInsert()
            .SetIdentityColumn(x => x.Id, ColumnDirection.InputOutput)
            .Commit(conn); 
    }

    trans.Complete();
}

/* With the above example, the value of the Id property in 'books' collection will be updated 
to reflect the value added in database. */

/* 
Notes: 

(1) It's also possible to add each column manually via the AddColumn method. Bear in mind that 
columns that are not added will be assigned their default value according to the property type. 
(2) It's possible to disable non-clustered indexes during the transaction. See advanced section 
for more info. 
*/

```

### BulkInsertOrUpdate (aka Merge)
---------------
```c#
var bulk = new BulkOperations();
books = GetBooks();

using (TransactionScope trans = new TransactionScope())
{
	using (SqlConnection conn = new SqlConnection(ConfigurationManager
	.ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
	{
        bulk.Setup<Book>()
            .ForCollection(books)
            .WithTable("Books")
            .AddColumn(x => x.ISBN)
            .AddColumn(x => x.Title)
            .AddColumn(x => x.Description)
            .BulkInsertOrUpdate()
            .MatchTargetOn(x => x.ISBN)
            .Commit(conn);
	}

	trans.Complete();
}

// Another example matching an identity column

using (TransactionScope trans = new TransactionScope())
{
	using (SqlConnection conn = new SqlConnection(ConfigurationManager
	.ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
	{
        bulk.Setup<Book>()
            .ForCollection(books)
            .WithTable("Books")
            .AddAllColumns()
            .BulkInsertOrUpdate()
            .SetIdentityColumn(x => x.Id)
            .MatchTargetOn(x => x.Id)
            .Commit(conn);
	}

	trans.Complete();
}

/* 
Notes: 

(1) It's possible to use AddAllColumns for operations BulkInsert/BulkInsertOrUpdate/BulkUpdate. 
(2) MatchTargetOn is mandatory for BulkUpdate, BulkInsertOrUpdate and BulkDelete... unless you want 
to eat an SqlBulkToolsException. 
(3) If model property name does not match the actual SQL column name, you can set up a custom 
mapping. An example of this is shown in a dedicated section somewhere in this documentation...
(4) BulkInsertOrUpdate also supports DeleteWhenNotMatched which is false by default. With power 
comes responsibility. You can instead use DeleteWhen to filter specific records. 
(5) If your model contains an identity column and it's included (via AddAllColumns, AddColumn or 
MatchTargetOn) in your setup, you must use SetIdentityColumn to mark it as your identity column. 
This is because identity columns are immutable and SQL will have a whinge when you try to update it. 
You can of course update based on an identity column (using MatchTargetOn) but just make sure to use 
SetIdentityColumn to mark it as an identity column so we can sort it out. A user friendly exception will 
be thrown if you forget. 
*/
```

### BulkUpdate
---------------
```c#
var bulk = new BulkOperations();
books = GetBooksToUpdate();

using (TransactionScope trans = new TransactionScope())
{
	using (SqlConnection conn = new SqlConnection(ConfigurationManager
	.ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
	{
        bulk.Setup<Book>()
            .ForCollection(books)
            .WithTable("Books")
            .AddColumn(x => x.ISBN)
            .AddColumn(x => x.Title)
            .AddColumn(x => x.Description)
            .BulkUpdate()
            .MatchTargetOn(x => x.ISBN) 
            .Commit(conn);
	}

	trans.Complete();
}

/* Notes: 

(1) Whilst it's possible to use AddAllColumns for BulkUpdate, using AddColumn for only the columns 
that need to be updated leads to performance gains. 
(2) MatchTargetOn is mandatory for BulkUpdate, BulkInsertOrUpdate and BulkDelete... unless you want to eat 
an SqlBulkToolsException. 
(3) MatchTargetOn can be called multiple times for more than one column to match on. 
(4) If your model contains an identity column and it's included (via AddAllColumns, AddColumn or 
MatchTargetOn) in your setup, you must use SetIdentityColumn to mark it as your identity column. 
Identity columns are immutable and auto incremented. You can of course update based on an identity 
column (using MatchTargetOn) but just make sure to use SetIdentityColumn to mark it as an 
identity column.  
*/

```
### BulkDelete
---------------
```c#

var bulk = new BulkOperations();
books = GetBooksIDontLike();

using (TransactionScope trans = new TransactionScope())
{
	using (SqlConnection conn = new SqlConnection(ConfigurationManager
	.ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
	{
        bulk.Setup<BookDto>()
            .ForCollection(books)
            .WithTable("Books")
            .AddColumn(x => x.ISBN)
            .BulkDelete()
            .MatchTargetOn(x => x.ISBN)
            .Commit(conn);
	}

	trans.Complete();
}

/* 
Notes: 

(1) Avoid using AddAllColumns for BulkDelete. 
(2) MatchTargetOn is mandatory for BulkUpdate, BulkInsertOrUpdate and BulkDelete... unless you want to eat 
an SqlBulkToolsException.
*/

```

### UpdateWhen & DeleteWhen
---------------
```c#
/* Only update or delete records when the target satisfies a speicific requirement. This is used alongside
MatchTargetOn and is available to BulkUpdate, BulkInsertOrUpdate and BulkDelete methods. Internally, 
SqlBulkTools will use a parameterized query for each (potentially unsafe) input and respect any custom 
column mappings that are applied.
*/

var bulk = new BulkOperations();
books = GetBooks();

using (TransactionScope trans = new TransactionScope())
{
	using (SqlConnection conn = new SqlConnection(ConfigurationManager
	.ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
	{
        /* BulkUpdate example */

        bulk.Setup<Book>()
            .ForCollection(books)
            .WithTable("Books")
            .AddColumn(x => x.Price)
            .BulkUpdate()
            .MatchTargetOn(x => x.ISBN)
            .UpdateWhen(x => x.Price <= 20)
            .Commit(conn); 

        /* BulkInsertOrUpdate example */

        bulk.Setup<Book>()
        .ForCollection(books)
        .WithTable("Books")
        .AddAllColumns()
        .BulkInsertOrUpdate()
        .MatchTargetOn(x => x.ISBN)
        .SetIdentityColumn(x => x.Id)
        .DeleteWhen(x => x.WarehouseId == 1)
        .Commit(conn); 

        /* BulkInsertOrUpdate also supports UpdateWhen which applies to the records that are being updated. */
	}

	trans.Complete();
}

```

### Upsert a single record
---------------
```c#

var bulk = new BulkOperations();

var book = new Book(){
    Title = "Programming your life away?"
    ISBN = "1234567",
    Price = 29.95,
    Description = "Nice book bro",
    WarehouseId = 1
};

using (TransactionScope trans = new TransactionScope())
{
	using (SqlConnection conn = new SqlConnection(ConfigurationManager
	.ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
	{   
        bulk.Setup<Book>()
        .ForObject(book)
        .WithTable("Books")
        .AddAllColumns()
        .Upsert()
        .SetIdentityColumn(x => x.Id, ColumnDirection.InputOutput)
        .MatchTargetOn(x => x.Id) // you can call MatchTargetOn for multiple columns if needed
        .Commit(con);
	}

	trans.Complete();
}
```

The above fluent expression translates to:

```sql
UPDATE [SqlBulkTools].[dbo].[Books] 
SET [WarehouseId] = @WarehouseId, 
[ISBN] = @ISBN, 
[Title] = @Title, 
[Description] = @Description, 
[Price] = @Price, 
WHERE [Id] = @Id 

IF (@@ROWCOUNT = 0) 
    BEGIN 
        INSERT INTO [SqlBulkTools].[dbo].[Books] ([WarehouseId], [ISBN], [Title], [Description], [Price])  
        VALUES (@WarehouseId, @ISBN, @Title, @Description, @Price) 
    END 
    
SET @Id=SCOPE_IDENTITY()
```

### Insert a single record
---------------
```c#

var bulk = new BulkOperations();

var book = new Book(){
    Title = "Programming your life away?"
    ISBN = "1234567",
    Price = 29.95,
    Description = "Nice book bro",
    WarehouseId = 1
};

using (TransactionScope trans = new TransactionScope())
{
	using (SqlConnection conn = new SqlConnection(ConfigurationManager
	.ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
	{   
        bulk.Setup<Book>()
        .ForObject(book)
        .WithTable("Books")
        .AddAllColumns()
        .Insert()
        .SetIdentityColumn(x => x.Id)
        .MatchTargetOn(x => x.Id)
        .Commit(con);
	}

	trans.Complete();
}
```

The above fluent expression translates to:

```sql
INSERT INTO [SqlBulkTools].[dbo].[Books] 
([WarehouseId], [ISBN], [Title], [Description], [Price])  
VALUES (@WarehouseId, @ISBN, @Title, @Description, @Price) 
```

### Update One or Many entities based on condition
---------------
```c#

var bulk = new BulkOperations();

Book bookToUpdate = new Book()
{
    ISBN = "123456789ABCD",
    Description = "I'm a bit dusty, update me!"
    Price = 49.99
};

using (TransactionScope trans = new TransactionScope())
{
	using (SqlConnection conn = new SqlConnection(ConfigurationManager
	.ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
	{
        int updatedRecords = bulk.Setup<Book>()
            .ForObject(bookToUpdate)
            .WithTable("Books")
            .AddColumn(x => x.Price)
            .AddColumn(x => x.Description)
            .Update()
            .Where(x => x.ISBN == book.ISBN)
            .Commit(conn);
        
        /* updatedRecords will be 1 if a record with the above ISBN exists 
        and the transaction is successful. */
	}

	trans.Complete();
}

```

The above fluent expression translates to:

```sql
UPDATE [SqlBulkTools].[dbo].[Books] 
SET [SqlBulkTools].[dbo].[Books].[Price] = @Price, 
[SqlBulkTools].[dbo].[Books].[Description] = @Description 
WHERE [ISBN] = @ISBNCondition1
```

### Delete One or Many entities based on condition
---------------
```c#
/* Easily delete one or more records in a single roundtrip. */

using (TransactionScope trans = new TransactionScope())
{
	using (SqlConnection conn = new SqlConnection(ConfigurationManager
	.ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
	{
        int affectedRecords = bulk.Setup<Book>()
        .ForDeleteQuery()
        .WithTable("Books")
        .Delete()
        .Where(x => x.Warehouse == 1)
        .And(x => x.Price >= 100)
        .And(x => x.Description == null)
        .Commit(conn);
	}

	trans.Complete();
}

```

The above fluent expression translates to:

```sql
DELETE FROM [SqlBulkTools].[dbo].[Books]  
WHERE [WarehouseId] = @WarehouseIdCondition1 
AND [Price] >= @PriceCondition2 
AND [Description] IS NULL
```

### Async Transactions (CommitAsync)
---------------

All setups include support for asynchronous transactions. Please note that you must supply
the argument 'TransactionScopeAsyncFlowOption.Enabled' to TransactionScope and you must be using at least .NET 4.5.1 
```c#
using (TransactionScope trans = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
{
	using (SqlConnection conn = new SqlConnection(ConfigurationManager
	.ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
	{
        await bulk.Setup<Book>()
            .ForDeleteQuery()
            .WithTable("Books")
            .Delete()
            .Where(x => x.WarehouseId == 1)
            .CommitAsync(conn);
	}

	trans.Complete();
}
```

### Custom Mappings
---------------
```c#
/* If the property names in your model don't match the column names in the corresponding table, you 
can use a custom column mapping. For the below example, assume that there is a 'BookTitle' column 
name in database which is defined in the C# model as 'Title' */

var bulk = new BulkOperations();
books = GetBooks();

using (TransactionScope trans = new TransactionScope())
{
	using (SqlConnection conn = new SqlConnection(ConfigurationManager
	.ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
	{
        bulk.Setup<Book>()
            .ForCollection(books)
            .WithTable("Books")
            .AddAllColumns()
            .CustomColumnMapping(x => x.Title, "BookTitle") 
            .BulkInsert()
            .Commit(conn);
	}
	trans.Complete();
}

// or if adding each column one by one...
using (TransactionScope trans = new TransactionScope())
{
  using (SqlConnection conn = new SqlConnection(ConfigurationManager
  .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
  {
        bulk.Setup<Book>()
            .ForCollection(books)
            .WithTable("Books")
            .AddColumn(x => x.Title, "BookTitle") // Title property corrosponds to BookTitle in table
            .AddColumn(x => x.ISBN)
            .AddColumn(x => x.Description)
            .BulkInsert()
            .Commit(conn);
  }
  trans.Complete();
}

```
### Collation conflicts
---------------
If you attempt to use MatchTargetOn against a string and you have mixed collations, you will receive a collation
SQL Exception. To overcome this error, you can set a collation in the MatchTargetOn overload.

```c#
var bulk = new BulkOperations();
books = GetBooks();

using (TransactionScope trans = new TransactionScope())
{
  using (SqlConnection conn = new SqlConnection(ConfigurationManager
  .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
  {
        bulk.Setup<Book>()
            .ForCollection(books)
            .WithTable("Books")
            .AddColumn(x => x.ISBN)
            .AddColumn(x => x.Title)
            .AddColumn(x => x.Description)
            .BulkInsertOrUpdate()
            .MatchTargetOn(x => x.ISBN, "DATABASE_DEFAULT")
            .Commit(conn);
  }

  trans.Complete();
}
```

### BuildPreparedDataDable
---------------
Easily create data tables for table variables or temp tables and benefit from the following features:
- Strongly typed column names. 
- Resolve data types automatically. 
- Populate list. 

Once data table is prepared, any additional configuration can be applied. 

```c#
DataTableOperations dtOps = new DataTableOperations();
books = GetBooks();

var dt = dtOps.SetupDataTable<Book>()
    .ForCollection(books)
    .AddColumn(x => x.Id)
    .AddColumn(x => x.ISBN)
    .AddColumn(x => x.Description)
    .CustomColumnMapping(x => x.Description, "BookDescription")
    .PrepareDataTable();

/* 
PrepareDataTable() returns a DataTable. Here you can apply additional configuration.
You can use GetColumn<T> to get the name of your property as a string. Any custom column 
mappings previously configured are applied 
*/

dt.Columns[dtOps.GetColumn<Book>(x => x.Id)].AutoIncrement = true;
dt.Columns[dtOps.GetColumn<Book>(x => x.ISBN)].AllowDBNull = false;

// .....

dt = dtOps.BuildPreparedDataTable(); 

// Another example...

// An example with AddAllColumns... easy.

var dt = dtOps.SetupDataTable<Book>()
.ForCollection(books)
.AddAllColumns()
.RemoveColumn(x => x.Description) // Use RemoveColumn to exclude a column. 
.PrepareDataTable();

// .....

dt = dtOps.BuildPreparedDataTable(); // Returns a populated DataTable

```

### Advanced
---------------
```c#
var bulk = new BulkOperations();
books = GetBooks();

bulk.Setup<Book>()
    .ForCollection(books)
    .WithTable("Books")
    .WithSchema("Api") // Specify a schema 
    .WithBulkCopySettings(new BulkCopySettings()
    {
      BatchSize = 5000, // Default is 0 - single batch
      BulkCopyTimeout = 720, // Default is 600 seconds
      EnableStreaming = true,
      SqlBulkCopyOptions = SqlBulkCopyOptions.TableLock
    })
    .AddColumn(x =>  // ........

/* SqlBulkTools gives you the ability to disable all non-clustered indexes during 
the transaction. Indexes are rebuilt once the transaction is completed. If at any time during 
the transaction an exception arises, the transaction is safely rolled back and indexes revert 
to their initial state. */

// Example

bulk.Setup<Book>()
    .ForCollection(books)
    .WithTable("Books")
    .AddAllColumns() 
    .BulkInsert()
    .TmpDisableAllNonClusteredIndexes()
    .Commit(conn);


/* When user that run operations on server side does not have permissions to ALTER table or 
when you wont to fire triggers and check constraint then proper SqlBulkCopyOptions should be applied. */

// Example
    .WithBulkCopySettings(new BulkCopySettings()
    {
      SqlBulkCopyOptions = SqlBulkCopyOptions.FireTriggers | SqlBulkCopyOptions.CheckConstraints
    })

```

### How does SqlBulkTools compare to others? 
<img src="http://gregnz.com/images/SqlBulkTools/performance_comparison.png" alt="Performance Comparison">

<b>Test notes:</b>
- Table had 6 columns including an identity column. <br/> 
- There were 3 non-clustered indexes on the table. <br/>
- SqlBulkTools used the following setup options: AddAllColumns, TmpDisableAllNonClusteredIndexes. <br/>
