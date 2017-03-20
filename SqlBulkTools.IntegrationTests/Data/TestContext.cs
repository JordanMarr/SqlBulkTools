﻿using System.Data.Entity;
using System.Data.SqlClient;
using SqlBulkTools.TestCommon.Model;

namespace SqlBulkTools.IntegrationTests2.Data
{
    public class TestContext : DbContext 
    {
        public TestContext()
            : base("SqlBulkToolsTest")
        {
            this.Database.CommandTimeout = 150;
        }

        public TestContext(SqlConnection conn, bool contextOwnsConnection) : base(conn, contextOwnsConnection)
        {
            this.Database.CommandTimeout = 150;
        }

        public virtual DbSet<Book> Books { get; set; }
        public virtual DbSet<SchemaTest1> SchemaTest1 { get; set; }
        public virtual DbSet<SchemaTest2> SchemaTest2 { get; set; }
        public virtual DbSet<CustomColumnMappingTest> CustomColumnMappingTest { get; set; }
        public virtual DbSet<ReservedColumnNameTest> ReservedColumnNameTest { get; set; }
    }
}
