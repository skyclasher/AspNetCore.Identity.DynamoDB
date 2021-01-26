using AspNetCore.Identity.DynamoDB;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace IdentitySample
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateWebHostBuilder(args).Build().Run();
		}



		public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
			WebHost.CreateDefaultBuilder(args)
				.UseStartup<Startup>();

	}


	public static class Utils
	{
		public static IServiceCollection AddDynamoDbIdentity(this IServiceCollection services)
		{
			services.AddDefaultIdentity<DynamoIdentityUser>()
							.AddRoles<DynamoIdentityRole>()
							.AddDefaultTokenProviders();


			services.AddSingleton<DynamoRoleUsersStore<DynamoIdentityRole, DynamoIdentityUser>, DynamoRoleUsersStore<DynamoIdentityRole, DynamoIdentityUser>>();
			services.AddSingleton<IUserStore<DynamoIdentityUser>, DynamoUserStore<DynamoIdentityUser, DynamoIdentityRole>>();
			services.AddSingleton<IRoleStore<DynamoIdentityRole>, DynamoRoleStore<DynamoIdentityRole>>();
			return services;
		}
	}
}