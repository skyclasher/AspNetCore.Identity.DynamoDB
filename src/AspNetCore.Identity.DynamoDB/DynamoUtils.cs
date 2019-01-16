using System;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AspNetCore.Identity.DynamoDB
{
    public static class DynamoUtils
    {
        public static async Task WaitForActiveTableAsync(IAmazonDynamoDB client, string userTableName)
        {
            bool active;
            do
            {
                active = true;
                var response = await client.DescribeTableAsync(new DescribeTableRequest { TableName = userTableName });
                if (!Equals(response.Table.TableStatus, TableStatus.ACTIVE) ||
                    !response.Table.GlobalSecondaryIndexes.TrueForAll(g => Equals(g.IndexStatus, IndexStatus.ACTIVE)))
                {
                    active = false;
                }
                Console.WriteLine($"Waiting for table {userTableName} to become active...");
                await Task.Delay(TimeSpan.FromSeconds(5));
            } while (!active);
        }

        public static IdentityBuilder AddDynamoDbIdentity(this IdentityBuilder builder)
        {
            builder.AddRoles<DynamoIdentityRole>()
                    .AddDefaultTokenProviders();


            builder.Services.AddSingleton<DynamoRoleUsersStore<DynamoIdentityRole, DynamoIdentityUser>, DynamoRoleUsersStore<DynamoIdentityRole, DynamoIdentityUser>>();
            builder.Services.AddSingleton<IUserStore<DynamoIdentityUser>, DynamoUserStore<DynamoIdentityUser, DynamoIdentityRole>>();
            builder.Services.AddSingleton<IRoleStore<DynamoIdentityRole>, DynamoRoleStore<DynamoIdentityRole>>();
            return builder;
        }

        public static IApplicationBuilder UseDynamoDBIdentity(this IApplicationBuilder app)
        {
            var options = app.ApplicationServices.GetService<IOptions<DynamoDbSettings>>();
            var env = app.ApplicationServices.GetService<IHostingEnvironment>();
            var client = app.ApplicationServices.GetService<IAmazonDynamoDB>();
            var context = new DynamoDBContext(client);

            var userStore = app.ApplicationServices
                    .GetService<IUserStore<DynamoIdentityUser>>()
                as DynamoUserStore<DynamoIdentityUser, DynamoIdentityRole>;
            var roleStore = app.ApplicationServices
                    .GetService<IRoleStore<DynamoIdentityRole>>()
                as DynamoRoleStore<DynamoIdentityRole>;
            var roleUsersStore = app.ApplicationServices
                .GetService<DynamoRoleUsersStore<DynamoIdentityRole, DynamoIdentityUser>>();

            userStore.EnsureInitializedAsync(client, context, options.Value.UsersTableName).Wait();
            roleStore.EnsureInitializedAsync(client, context, options.Value.RolesTableName).Wait();
            roleUsersStore.EnsureInitializedAsync(client, context, options.Value.RoleUsersTableName).Wait();

            return app;
        }

        public class DynamoDbSettings
        {
            public string ServiceUrl { get; set; }
            public string UsersTableName { get; set; }
            public string RolesTableName { get; set; }
            public string RoleUsersTableName { get; set; }
        }

    }
}