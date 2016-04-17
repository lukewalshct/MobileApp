using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using Microsoft.Azure.Mobile.Server;
using Microsoft.Azure.Mobile.Server.Tables;
using LWalshFinalAzure.DataObjects;

namespace LWalshFinalAzure.Models
{
    public class MobileServiceContext : DbContext
    {
        // You can add custom code to this file. Changes will not be overwritten.
        // 
        // If you want Entity Framework to alter your database
        // automatically whenever you change your model schema, please use data migrations.
        // For more information refer to the documentation:
        // http://msdn.microsoft.com/en-us/data/jj591621.aspx
        //
        // To enable Entity Framework migrations in the cloud, please ensure that the 
        // service name, set by the 'MS_MobileServiceName' AppSettings in the local 
        // Web.config, is the same as the service name when hosted in Azure.

        private const string connectionStringName = "Name=MS_TableConnectionString";

        public MobileServiceContext() : base(connectionStringName)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Add(
                new AttributeToColumnAnnotationConvention<TableColumnAttribute, string>(
                    "ServiceTableColumn", (property, attributes) => attributes.Single().ColumnType.ToString()));
            //modelBuilder.Entity<User>()
            //    .HasMany<Household>(u => u.households)
            //    .WithMany(h => h.users)
            //    .Map(hu =>
            //    {
            //        hu.MapLeftKey("UserRefId");
            //        hu.MapRightKey("HouseholdRefId");
            //        hu.ToTable("HouseholdMember");
            //    }
            //    );
        }

        public System.Data.Entity.DbSet<LWalshFinalAzure.DataObjects.User> Users { get; set; }
        public System.Data.Entity.DbSet<LWalshFinalAzure.DataObjects.Household> Households { get; set; }
        public System.Data.Entity.DbSet<LWalshFinalAzure.DataObjects.HouseholdMember> HouseholdMembers { get; set; }
    }
}
