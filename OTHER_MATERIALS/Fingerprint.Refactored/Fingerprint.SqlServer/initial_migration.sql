DECLARE @createDbQuery NVARCHAR(MAX) = 'CREATE DATABASE Fingerprint'
DECLARE @createTestRunsTable NVARCHAR(MAX) = 'CREATE TABLE [Fingerprint].[dbo].[TestRuns] (
	Id INT NOT NULL PRIMARY KEY IDENTITY,
	StartDate DATETIME2(3) NOT NULL,
	DatasetPath NVARCHAR(MAX) NOT NULL
)'
DECLARE @createImagesTable NVARCHAR(MAX) = 'CREATE TABLE [Fingerprint].[dbo].[Images] (
	Id INT NOT NULL PRIMARY KEY IDENTITY,
	FileName NVARCHAR(MAX) NOT NULL,
	WidthShift INT,
	HeightShift INT,
	WidthOffset INT,
	HeightOffset INT,
	ProcessedCorrectly BIT,
	TestRunId INT NOT NULL FOREIGN KEY REFERENCES [Fingerprint].[dbo].[TestRuns](Id)
)'
DECLARE @createMinutiaeTable NVARCHAR(MAX) = 'CREATE TABLE [Fingerprint].[dbo].[Minutiae] (
	Id INT NOT NULL PRIMARY KEY IDENTITY,
	X INT NOT NULL,
	Y INT NOT NULL,
	IsTermination BIT NOT NULL,
	Theta FLOAT NOT NULL,
	ImageId INT NOT NULL FOREIGN KEY REFERENCES [Fingerprint].[dbo].[Images](Id)
)'
DECLARE @createClustersTable NVARCHAR(MAX) = 'CREATE TABLE [Fingerprint].[dbo].[Clusters] (
	Id INT NOT NULL PRIMARY KEY IDENTITY,
	ImageId INT NOT NULL FOREIGN KEY REFERENCES [Fingerprint].[dbo].[Images](Id)
)'
DECLARE @createClusterMinutiaeTable NVARCHAR(MAX) = 'CREATE TABLE [Fingerprint].[dbo].[ClusterMinutiae] (
	Id INT NOT NULL PRIMARY KEY IDENTITY,
	ClusterId INT NOT NULL FOREIGN KEY REFERENCES [Fingerprint].[dbo].[Clusters](Id),
	MinutiaId INT NOT NULL FOREIGN KEY REFERENCES [Fingerprint].[dbo].[Minutiae](Id),
	IsCentroid BIT NOT NULL
)'
DECLARE @createMetricsTable NVARCHAR(MAX) = 'CREATE TABLE [Fingerprint].[dbo].[Metrics] (
	Id INT NOT NULL PRIMARY KEY IDENTITY,
	Name NVARCHAR(900) NOT NULL UNIQUE,
	AcceptableThreshold FLOAT NOT NULL
)'
DECLARE @createMinutiaeMetricsTable NVARCHAR(MAX) = 'CREATE TABLE [Fingerprint].[dbo].[MinutiaeMetrics] (
	Id INT NOT NULL PRIMARY KEY IDENTITY,
	ClusterId INT NOT NULL FOREIGN KEY REFERENCES [Fingerprint].[dbo].[Clusters](Id),
	MinutiaId INT NOT NULL FOREIGN KEY REFERENCES [Fingerprint].[dbo].[Minutiae](Id),
	OtherMinutiaId INT NOT NULL FOREIGN KEY REFERENCES [Fingerprint].[dbo].[Minutiae](Id),
	MetricId INT NOT NULL FOREIGN KEY REFERENCES [Fingerprint].[dbo].[Metrics](Id),
	Value FLOAT NOT NULL
)'
DECLARE @createClusterComparisonsTable NVARCHAR(MAX) = 'CREATE TABLE [Fingerprint].[dbo].[ClusterComparisons] (
	Id INT NOT NULL PRIMARY KEY IDENTITY,
	FirstMinutiaId INT NOT NULL FOREIGN KEY REFERENCES [Fingerprint].[dbo].[ClusterMinutiae](Id),
	SecondMinutiaId INT NOT NULL FOREIGN KEY REFERENCES [Fingerprint].[dbo].[ClusterMinutiae](Id),
	LeadingFirstMinutiaId INT NOT NULL FOREIGN KEY REFERENCES [Fingerprint].[dbo].[ClusterMinutiae](Id),
	LeadingSecondMinutiaId INT NOT NULL FOREIGN KEY REFERENCES [Fingerprint].[dbo].[ClusterMinutiae](Id),
	Matches BIT NOT NULL,
	DistanceDifference FLOAT NOT NULL,
	AngleDifference FLOAT NULL
)'
BEGIN TRY
	EXEC(@createDbQuery)
	BEGIN TRANSACTION DATABASE_CREATION
		EXEC(@createTestRunsTable)
		EXEC(@createImagesTable)
		EXEC(@createMinutiaeTable)
		EXEC(@createClustersTable)
		EXEC(@createClusterMinutiaeTable)
		EXEC(@createMetricsTable)
		EXEC(@createMinutiaeMetricsTable)
		EXEC(@createClusterComparisonsTable)
		EXEC('INSERT INTO [Fingerprint].[dbo].[Metrics] (Name, AcceptableThreshold) VALUES (''Distance'', 10)')
		EXEC('INSERT INTO [Fingerprint].[dbo].[Metrics] (Name, AcceptableThreshold) VALUES (''Angle'', PI() / 4)')
	COMMIT TRANSACTION DATABASE_CREATION
	PRINT 'Database created'
	SELECT name FROM [Fingerprint].[sys].[Tables]
END TRY

BEGIN CATCH
	IF (@@TRANCOUNT > 0)
	BEGIN
		ROLLBACK TRANSACTION DATABASE_CREATION
		DROP DATABASE Fingerprint
		PRINT 'Found an error. Rollbacking transaction'
		PRINT ERROR_MESSAGE()
	END
END CATCH