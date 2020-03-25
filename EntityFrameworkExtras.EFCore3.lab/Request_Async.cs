﻿using System;
using System.Collections.Generic;
using System.Data; 
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkExtras.EFCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Z.EntityFrameworkExtras.Lab.EFCore30
{
	class Request_Async
	{
		public static void Execute()
		{
			// Create BD 
			using (var context = new EntityContext())
			{
				My.CreateBD(context);
			}

			// CLEAN  
			using (var context = new EntityContext())
			{
				context.EntitySimples.RemoveRange(context.EntitySimples);

				context.SaveChanges();
			}

			// SEED  
			using (var context = new EntityContext())
			{
				for (int i = 0; i < 3; i++)
				{
					context.EntitySimples.Add(new EntitySimple { ColumnInt = i });
				}

				context.SaveChanges();
			}



			// TEST  
			using (var context = new EntityContext())
			{
				var connection = context.Database.GetDbConnection();
				connection.Open();
				using (var commande = connection.CreateCommand())
				{
					commande.CommandText = @"   
if exists (select 1 from sys.procedures where name = 'PROC_Get_EntitySimple')
BEGIN
DROP PROCEDURE [dbo].[PROC_Get_EntitySimple]
END 
						";
					commande.ExecuteNonQuery();
				}

				using (var commande = connection.CreateCommand())
				{
					commande.CommandText = @"    
CREATE PROCEDURE [dbo].[PROC_Get_EntitySimple]

	@ParameterID INT 	,
	@ParameterInt INT = NULL OUTPUT 


AS
BEGIN 

WAITFOR DELAY '00:00:05'
Select * from EntitySimples
Where ColumnInt = @ParameterID
Set @ParameterInt = @ParameterID +1 
END
						";
					commande.ExecuteNonQuery();
				}

			}

			// TEST  
			using (var context = new EntityContext())
			{
				var cts = new CancellationTokenSource();
			
				var proc_Get_EntitySimple = new Proc_Get_EntitySimple() { ParameterID = 2 };
				var entity = context.Database.ExecuteStoredProcedureAsync<EntitySimple>(proc_Get_EntitySimple, cts.Token) ;
				// cts.Cancel();
				entity.Wait();
			}


			using (var context = new EntityContext())
			{
				var cts = new CancellationTokenSource();
				var proc_Get_EntitySimple = new Proc_Get_EntitySimple() { ParameterID = 2 };
				var task = context.Database.ExecuteStoredProcedureAsync(proc_Get_EntitySimple, cts.Token);
		 	//  cts.Cancel();
				task.Wait(); 
			}
		}

		public class EntityContext : DbContext
		{
			public EntityContext() : base( )
			{
			}

			public DbSet<EntitySimple> EntitySimples { get; set; }

			protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
			{
				optionsBuilder.UseSqlServer(new SqlConnection(My.ConnectionString));

				base.OnConfiguring(optionsBuilder);
			}
		}

		[StoredProcedure("PROC_Get_EntitySimple")]
		public class Proc_Get_EntitySimple
		{
			[StoredProcedureParameter(SqlDbType.Int, Direction = ParameterDirection.Output)]
			public int ParameterInt { get; set; }

			[StoredProcedureParameter(SqlDbType.Int, Direction = ParameterDirection.Input)]
			public int ParameterID { get; set; }
		}

		public class EntitySimple
		{
			public int ID { get; set; }
			public int ColumnInt { get; set; }
			public String ColumnString { get; set; }
		}
	}
}