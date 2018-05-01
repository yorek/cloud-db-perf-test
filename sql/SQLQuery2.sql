select count(*) from dbo.TestTable --with (readpast)

select top 10 * from dbo.TestTable

delete from dbo.TestTable
--truncate table dbo.TestTable

select * from sys.dm_exec_connections where net_transport = 'TCP'
select * from sys.dm_exec_sessions where is_user_process = 1 and program_name like  'Test%'

select 
	cast(InsertTime as time(0)),
	count(*)
from
	dbo.TestTable
group by
	cast(InsertTime as time(0))
order by 
	1


SELECT avg_cpu_percent, avg_data_io_percent, avg_log_write_percent, avg_memory_usage_percent, xtp_storage_percent,
       max_worker_percent, max_session_percent, dtu_limit, avg_login_rate_percent, end_time 
FROM sys.dm_db_resource_stats WITH (NOLOCK) 
ORDER BY end_time DESC OPTION (RECOMPILE);