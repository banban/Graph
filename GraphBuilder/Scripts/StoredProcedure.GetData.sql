USE [AdventureWorks2014]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*=============================================
Author		: Andrew Butenko
Create date	: 03/08/2016
Description	: retrieves data to build YED graph
    exec [dbo].[GetGraphData] @EntityType='FKDependencies'
    exec [dbo].[GetGraphData] @EntityType='ReportDependencies', @RootMask ='/SampleReports/%'
	exec [dbo].[GetGraphData] @EntityType='JobDependencies', @Url = 'http://localhost/{0}'
=============================================*/
CREATE PROCEDURE [dbo].[GetGraphData]
(
    @EntityType varchar(100)='ReportDependencies'
	,@RootMask varchar(100)='%'
    ,@Url varchar(255) = NULL
    --,@RootName varchar(100)='All'
)
AS
BEGIN
    -- SET NOCOUNT ON added to prevent extra result sets from
    -- interfering with SELECT statements.
    SET NOCOUNT ON;
    IF @EntityType ='FKDependencies'
    BEGIN
        SELECT fk.[parent_object_id] as IdFrom, OBJECT_NAME(fk.[parent_object_id]) as NameFrom, 'Table' as TypeFrom
                ,CONVERT(VARCHAR(255),Null) UrlFrom
                ,'delete: '+fk.delete_referential_action_desc +' update: '+ fk.update_referential_action_desc as DescriptionFrom
            ,fk.[referenced_object_id] as IdTo, OBJECT_NAME(fk.[referenced_object_id]) as NameTo, 'Table' as TypeTo
                ,CONVERT(VARCHAR(255),Null) as UrlTo
                ,'delete: '+fk.delete_referential_action_desc +' update: '+ fk.update_referential_action_desc as DescriptionTo
            ,fk.name as Actions
--select OBJECT_NAME([parent_object_id]),OBJECT_NAME([referenced_object_id]),* from sys.foreign_keys
			FROM sys.foreign_keys fk
		ORDER BY 2, 7 
	END
    ELSE IF @EntityType ='ReportDependencies'
    BEGIN
        ;WITH LinkedReports as (
			SELECT CONVERT(varchar(64),ctl.ItemID) as ItemID, ctl.[Name] as Name, ctl.[Path], 'Linked Report' as Type
                ,REPLACE('@Url','{temp}', 'dbo.Catalog&IDName=ItemID&IDValue='+convert(varchar(64),ctl.ItemID)) as Url
                ,CONVERT(varchar(255),ctl.Description) as Description
				,CONVERT(xml,ctl.Parameter) as ParametersXml
				,[LinkSourceID]
				,[ParentID]
			FROM [ReportServer].[dbo].[Catalog] ctl
			WHERE ctl.Type=4 AND ctl.[Path] like @RootMask
        )
        ,MasterReports as (
			SELECT CONVERT(varchar(64),ctl.ItemID) as ItemID, ctl.[Name] as Name, ctl.[Path], 'Master Report' as Type
                ,REPLACE('@Url','{temp}', 'dbo.Catalog&IDName=ItemID&IDValue='+convert(varchar(64),ctl.ItemID)) as Url
                ,CONVERT(varchar(255),ctl.Description) as Description
				,[ParentID]
			FROM [ReportServer].[dbo].[Catalog] ctl
			WHERE ctl.Type=2 AND ctl.[Path] like @RootMask
        )
        ,DataSets as (
			SELECT CONVERT(varchar(64),ctl.ItemID) as ItemID, ctl.[Name] as Name, ctl.[Path], 'Data Set' as Type
                ,REPLACE('@Url','{temp}', 'dbo.Catalog&IDName=ItemID&IDValue='+convert(varchar(64),ctl.ItemID)) as Url
                ,CONVERT(varchar(255),ctl.Description) as Description
				,[ParentID]
			FROM [ReportServer].[dbo].[Catalog] ctl
			WHERE ctl.Type=8 AND ctl.[Path] like @RootMask
        )
        ,Folders as (
			SELECT CONVERT(varchar(64),ctl.ItemID) as ItemID, ctl.[Name] as Name, ctl.[Path], 'Folder' as Type
                ,REPLACE('@Url','{temp}', 'dbo.Catalog&IDName=ItemID&IDValue='+convert(varchar(64),ctl.ItemID)) as Url
                ,CONVERT(varchar(255),ctl.Description) as Description
				,[ParentID]
			FROM [ReportServer].[dbo].[Catalog] ctl
			WHERE ctl.Type=1 AND ctl.[Path] like @RootMask
        )
		--datasets to master reports
        SELECT ctl.ItemID as IdFrom, ctl.[Name] as NameFrom, ctl.Type as TypeFrom
                ,ctl.Url UrlFrom
                ,ctl.Description as DescriptionFrom
            ,ctl2.ItemID as IdTo, ctl2.[Name] as NameTo, ctl2.Type as TypeTo
                ,ctl2.Url as UrlTo
                ,ctl2.Description as DescriptionTo
            ,'Connects' as Actions
		FROM DataSets ctl
		LEFT OUTER JOIN [ReportServer].[dbo].[DataSets] dts 
			INNER JOIN MasterReports ctl2 ON ctl2.ItemID = dts.ItemID
		ON ctl.ItemID = dts.LinkID
		--master to linked reports
		UNION ALL
        SELECT ctl.ItemID as IdFrom, ctl.[Name] as NameFrom, ctl.Type as TypeFrom
                ,ctl.Url UrlFrom
                ,ctl.Description as DescriptionFrom
            ,ctl2.ItemID as IdTo, ctl2.[Name] as NameTo, ctl2.Type as TypeTo
                ,ctl2.Url as UrlTo
                ,ctl2.Description as DescriptionTo
            ,'Links' as Actions
		FROM MasterReports ctl
		LEFT OUTER JOIN LinkedReports ctl2 ON  ctl.ItemID = ctl2.[LinkSourceID]
		--folders to master reports
		UNION ALL
        SELECT ctl.ItemID as IdFrom, ctl.[Name] as NameFrom, ctl.Type as TypeFrom
                ,ctl.Url UrlFrom
                ,ctl.Description as DescriptionFrom
            ,ctl2.ItemID as IdTo, ctl2.[Name] as NameTo, ctl2.Type as TypeTo
                ,ctl2.Url as UrlTo
                ,ctl2.Description as DescriptionTo
            ,'Contains' as Actions
		FROM Folders ctl
		INNER JOIN MasterReports ctl2 ON ctl.ItemID = ctl2.ParentID
		--folders to linked reports
		UNION ALL
        SELECT ctl.ItemID as IdFrom, ctl.[Name] as NameFrom, ctl.Type as TypeFrom
                ,ctl.Url UrlFrom
                ,ctl.Description as DescriptionFrom
            ,ctl2.ItemID as IdTo, ctl2.[Name] as NameTo, ctl2.Type as TypeTo
                ,ctl2.Url as UrlTo
                ,ctl2.Description as DescriptionTo
            ,'Contains' as Actions
		FROM Folders ctl
		INNER JOIN LinkedReports ctl2 ON ctl.ItemID = ctl2.ParentID

		--linked reports to parameters
		UNION ALL
		select ctl.ItemID as IdFrom, ctl.Name as NameFrom, ctl.[Type] as TypeFrom, ctl.Url as UrlFrom, ctl.[Description] as DescriptionFrom
            ,prm.[Parameter].value('(Name)[1]', 'varchar(100)') 
				+ prm.[Parameter].value('(Type)[1]', 'varchar(50)') as IdTo
            , prm.[Parameter].value('(Name)[1]', 'varchar(100)') as NameTo, 'Parameter' as TypeTo
                ,REPLACE('@Url','{temp}', 'dbo.Catalog&IDName=ItemID&IDValue='+convert(varchar(64),ctl.ItemID)) as UrlTo
            ,prm.[Parameter].value('(Type)[1]', 'varchar(50)') as DescriptionTo
            ,'Has value='+prm.[Parameter].value('(DefaultValues/Value)[1]', 'varchar(255)') Actions
			--,prm.[Parameter].value('(Name)[1]', 'varchar(100)') as [ParameterName]
			--,prm.[Parameter].value('(Type)[1]', 'varchar(50)') as [ParameterType]
			--,prm.[Parameter].value('(DefaultValues/Value)[1]', 'varchar(255)') as [ParameterValue]
			--,prm.[Parameter].value('(Nullable)[1]', 'varchar(10)') as [ParameterNullable]
			--,prm.[Parameter].value('(AllowBlank)[1]', 'varchar(10)') as [ParameterAllowBlank]
			--,prm.[Parameter].value('(MultiValue)[1]', 'varchar(10)') as [ParameterMultiValue]
			--,prm.[Parameter].value('(UsedInQuery)[1]', 'varchar(10)') as [UsedInQuery]
			--,prm.[Parameter].value('(State)[1]', 'varchar(50)') as [ParameterState]
			--,prm.[Parameter].value('(Prompt)[1]', 'varchar(10)') as [ParameterPrompt]
			--,prm.[Parameter].value('(DynamicPrompt)[1]', 'varchar(10)') as [ParameterDynamicPrompt]
			--,prm.[Parameter].value('(PromptUser)[1]', 'varchar(10)') as [ParameterPromptUser]
		FROM LinkedReports ctl
		CROSS APPLY ParametersXml.nodes('//Parameter') AS prm (Parameter)

        RETURN;
    END

    ELSE IF @EntityType ='JobDependencies'
    BEGIN
		--DECLARE @Url VARCHAR(255) = 'http://localhost/{0}'
		;WITH L1 AS (
			SELECT ROW_NUMBER() OVER (ORDER BY sjh.[LastRunDate],sj.[name]) AS [RowNo]
				,sj.[job_id]
				,sj.[name] as [job_name]
				,sj.[Description]
				,sjh.[MaxDuration]
				,CONVERT(DATETIME,(CONVERT(VARCHAR(16),sjh.[LastRunDate],121))) AS [LastRunDate]
				,sja.[NextRunDate] 
				,@@SERVERNAME AS [ServerName]
			FROM [msdb].[dbo].[sysjobs_view] sj
			LEFT OUTER JOIN (
				SELECT [job_id], MAX(convert(datetime, convert(varchar(8),h.run_date) +' '+ 
						isnull(substring (right (stuff (' ', 1, 1, '000000') + convert(varchar(6),h.run_time), 6), 1, 2)
							+ ':'
							+ substring (right (stuff (' ', 1, 1, '000000') + convert(varchar(6), h.run_time), 6) ,3 ,2)
							+ ':'
							+ substring (right (stuff (' ', 1, 1, '000000') + convert(varchar(6),h.run_time), 6) ,5 ,2),'')
					,112)) as [LastRunDate]
					,MAX(substring (right (stuff (' ', 1, 1, '000000') + convert(varchar(6),h.run_duration), 6), 1, 2)
							+ ':'
							+ substring (right (stuff (' ', 1, 1, '000000') + convert(varchar(6), h.run_duration), 6) ,3 ,2)
							+ ':'
							+ substring (right (stuff (' ', 1, 1, '000000') + convert(varchar(6),h.run_duration), 6) ,5 ,2)) AS [MaxDuration]
				FROM [msdb].[dbo].[sysjobhistory] h
				WHERE h.step_id = 0
					AND LEN(h.run_duration)<=6 --exclude jobs running longer then 1 day
				GROUP BY [job_id]
				) sjh ON sj.job_id = sjh.job_id
			LEFT OUTER JOIN (
				SELECT [job_id],MAX(start_execution_date) AS [start_execution_date], MAX(next_scheduled_run_date) AS [NextRunDate] 
				FROM [msdb].[dbo].[sysjobactivity] 
				GROUP BY [job_id]
				) sja ON sj.job_id = sja.job_id
			WHERE sj.[enabled] = 1
				AND sj.[Description] NOT LIKE '%This job is owned by a report server process.%'
				AND sj.[name] NOT LIKE '%[_]Purge%'
		)
		--select * from L1 order by [RowNo]
		,L2 AS (
			SELECT 
				ROW_NUMBER() OVER (ORDER BY [LastRunDate]) AS [GroupNo]
				--,MAX([RowNo]) AS [LastRowNo]
				,[LastRunDate]
				,[ServerName]
			FROM L1
			GROUP BY [LastRunDate]
				,[ServerName]
		)
		--select * from L2 order by [LastRowNo]
		,L3 AS (SELECT L1.[RowNo]
				--, L2.[LastRowNo]
				, ISNULL(L2.[GroupNo],1) AS [GroupNo] 
				,L1.[job_id]
				,L1.[job_name]
				,L1.[Description]
				,L1.[MaxDuration]
				,L1.[LastRunDate]
				,L1.[NextRunDate] 
				,L1.[ServerName]
			FROM L1
			LEFT OUTER JOIN L2 ON L1.[LastRunDate] = L2.[LastRunDate]
		)
		--SELECT * FROM L3 ORDER BY [RowNo]
        SELECT l.[job_id] as IdFrom, l.[job_name] as [NameFrom], l.[ServerName] as [TypeFrom]
                ,REPLACE(@Url,'{0}', l.[job_id]) AS [UrlFrom]
                ,l.[Description] as [DescriptionFrom]
            ,r.[job_id] as IdTo, r.[job_name] as [NameTo], r.[ServerName] as TypeTo
                ,REPLACE(@Url,'{0}', r.[job_id]) as UrlTo
                ,r.[Description] as [DescriptionTo]
            ,'start at:'+ CONVERT(VARCHAR(5),l.[LastRunDate],114) + ', duration:'+l.[MaxDuration] as Actions
		FROM L3 l
		LEFT OUTER JOIN L3 r ON l.[GroupNo] + 1 = r.[GroupNo]

        RETURN;
    END
END


