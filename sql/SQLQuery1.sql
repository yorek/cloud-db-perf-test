--create sequence dbo.TestSequence
--as bigint
--start with 1 
--increment by 1
--go

drop table if exists dbo.TestTable ;
go

--create table dbo.TestTable 
--(
--	Id bigint not null primary key default (next value for dbo.TestSequence),
--	InsertTime datetime2 not null default(sysdatetime()),
--	Payload nvarchar(max) check (isjson(Payload) = 1)
--)
--go
create table dbo.TestTable 
(
	Id bigint not null primary key nonclustered,
	InsertTime datetime2 not null default(sysdatetime()),
	Payload nvarchar(max) check (isjson(Payload) = 1)
)	
with 
(
	memory_optimized = on, 
	durability = schema_and_data
)
go
