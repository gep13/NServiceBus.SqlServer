namespace NServiceBus.Transports.SQLServer
{
    using System.Data;

    class Sql
    {
        internal const string PurgeText = @"DELETE FROM [{0}].[{1}]";

        internal const string SendText =
            @"INSERT INTO [{0}].[{1}] ([Id],[CorrelationId],[ReplyToAddress],[Recoverable],[Expires],[Headers],[Body]) 
                                    VALUES (@Id,@CorrelationId,@ReplyToAddress,@Recoverable,CASE WHEN @TimeToBeReceivedMs IS NOT NULL THEN DATEADD(ms, @TimeToBeReceivedMs, GETUTCDATE()) END,@Headers,@Body)";

        internal const string ReceiveText =
            @"WITH message AS (SELECT TOP(1) * FROM [{0}].[{1}] WITH (UPDLOCK, READPAST, ROWLOCK) ORDER BY [RowVersion]) 
			DELETE FROM message 
			OUTPUT deleted.Id, deleted.CorrelationId, deleted.ReplyToAddress, 
			deleted.Recoverable, CASE WHEN deleted.Expires IS NOT NULL THEN DATEDIFF(ms, GETUTCDATE(), deleted.Expires) END, deleted.Headers, deleted.Body;";
        
        internal const string PeekText = @"SELECT count(*) Id FROM [{0}].[{1}];";

        internal const string CreateQueueText = @"IF NOT  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[{1}]') AND type in (N'U'))
                  BEGIN
                    EXEC sp_getapplock @Resource = '{0}_{1}_lock', @LockMode = 'Exclusive'

                    IF NOT  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[{1}]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [{0}].[{1}](
	                        [Id] [uniqueidentifier] NOT NULL,
	                        [CorrelationId] [varchar](255) NULL,
	                        [ReplyToAddress] [varchar](255) NULL,
	                        [Recoverable] [bit] NOT NULL,
	                        [Expires] [datetime] NULL,
	                        [Headers] [varchar](max) NOT NULL,
	                        [Body] [varbinary](max) NULL,
	                        [RowVersion] [bigint] IDENTITY(1,1) NOT NULL
                        ) ON [PRIMARY];                    

                        CREATE CLUSTERED INDEX [Index_RowVersion] ON [{0}].[{1}] 
                        (
	                        [RowVersion] ASC
                        )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

                        CREATE NONCLUSTERED INDEX [Index_Expires] ON [{0}].[{1}]
                        (
	                        [Expires] ASC
                        )
                        INCLUDE
                        (
                            [Id],
                            [RowVersion]
                        )
                        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
                    END
    
                    EXEC sp_releaseapplock @Resource = '{0}_{1}_lock'
                  END";

        internal const string PurgeBatchOfExpiredMessagesText = @"DELETE FROM [{1}].[{2}] WHERE [Id] IN (SELECT TOP ({0}) [Id] FROM [{1}].[{2}] WITH (UPDLOCK, READPAST, ROWLOCK) WHERE [Expires] < GETUTCDATE() ORDER BY [RowVersion])";

        internal const string CheckIfExpiresIndexIsPresent = @"SELECT COUNT(*) FROM [sys].[indexes] WHERE [name] = '{0}' AND [object_id] = OBJECT_ID('[{1}].[{2}]')";

        internal const string ExpiresIndexName = "Index_Expires";

        internal class Columns
        {
            internal static ColumnInfo Id = new ColumnInfo
            {
                Index = 0,
                Name = "Id",
                Type = SqlDbType.UniqueIdentifier
            };

            internal static ColumnInfo CorrelationId = new ColumnInfo
            {
                Index = 1,
                Name = "CorrelationId",
                Type = SqlDbType.VarChar
            };

            internal static ColumnInfo ReplyToAddress = new ColumnInfo
            {
                Index = 2,
                Name = "ReplyToAddress",
                Type = SqlDbType.VarChar
            };

            internal static ColumnInfo Recoverable = new ColumnInfo
            {
                Index = 3,
                Name = "Recoverable",
                Type = SqlDbType.Bit
            };

            internal static ColumnInfo TimeToBeReceived = new ColumnInfo
            {
                Index = 4,
                Name = "TimeToBeReceivedMs",
                Type = SqlDbType.Int
            };

            internal static ColumnInfo Headers = new ColumnInfo
            {
                Index = 5,
                Name = "Headers",
                Type = SqlDbType.VarChar
            };

            internal static ColumnInfo Body = new ColumnInfo
            {
                Index = 6,
                Name = "Body",
                Type = SqlDbType.VarBinary
            };

            public static ColumnInfo[] All =
            {
                Id,
                CorrelationId,
                ReplyToAddress,
                Recoverable,
                TimeToBeReceived,
                Headers,
                Body
            };
        }

        internal class ColumnInfo
        {
            public int Index { get; set; }
            public string Name { get; set; }
            public SqlDbType Type { get; set; }
        }
    }
}