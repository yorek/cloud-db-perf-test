drop table if exists dbo.TestTable ;
go

alter database perftest modify (service_objective = 'S0')
go

select * from sys.database_service_objectives
go

