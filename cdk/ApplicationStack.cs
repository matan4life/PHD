using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Notifications;
using Constructs;

namespace Cdk;

class ApplicationStack : Stack
{
    internal ApplicationStack(Construct scope, Props props) : base(scope, props.ApplicationStackName, default)
    {
        var datasetBucket = S3.CreateDatasetBucket(this, props);
        var inputBucket = S3.CreateInputBucket(this, props);

        // DynamoDB Table for Dataset Minutiae
        var datasetTable = DynamoDb.CreateDatasetMinutiaeTable(this, props);
        var inputTable = DynamoDb.CreateInputMinutiaeTable(this, props);

        var groupsTable = DynamoDb.CreateDatasetGroupsTable(this, props);
        var resultTable = DynamoDb.CreateResultTable(this, props);

        var role = Roles.CreateRole(this, props);
        var asset = DockerImageCode.FromImageAsset("../python-extract", new AssetImageCodeProps { File = "Dockerfile" });
        // Extract Lambda
        var extractLambda = new DockerImageFunction(this, "DatasetExtract", new DockerImageFunctionProps
        {
            Code = asset,
            MemorySize = 1024,
            Timeout = Duration.Minutes(15),
            Role = role,
            LogGroup = new LogGroup(this, "DatasetExtractLogGroup", new LogGroupProps
            {
                LogGroupName = "/aws/lambda/dataset-extract",
                RemovalPolicy = RemovalPolicy.DESTROY,
                Retention = RetentionDays.ONE_WEEK
            }),
            Environment = new Dictionary<string, string>
            {
                { "TABLE_NAME", datasetTable.TableName },
                { "SERVICE", "dataset-extract" },
                { "GROUPS_TABLE_NAME", groupsTable.TableName }
            }
        });
        var inputLambda = new DockerImageFunction(this, "InputExtract", new DockerImageFunctionProps
        {
            Code = asset,
            MemorySize = 1024,
            Timeout = Duration.Minutes(15),
            Role = role,
            LogGroup = new LogGroup(this, "InputExtractLogGroup", new LogGroupProps
            {
                LogGroupName = "/aws/lambda/input-extract",
                RemovalPolicy = RemovalPolicy.DESTROY,
                Retention = RetentionDays.ONE_WEEK
            }),
            Environment = new Dictionary<string, string>
            {
                { "TABLE_NAME", inputTable.TableName },
                { "SERVICE", "input-extract" },
                { "GROUPS_TABLE_NAME", groupsTable.TableName }
            }
        });
        var comparatorLambda = new DockerImageFunction(this, "Compare", new DockerImageFunctionProps
        {
            Code = DockerImageCode.FromImageAsset("../python-compare", new AssetImageCodeProps { File = "Dockerfile" }),
            MemorySize = 1024,
            Timeout = Duration.Minutes(15),
            Role = role,
            LogGroup = new LogGroup(this, "CompareLogGroup", new LogGroupProps
            {
                LogGroupName = "/aws/lambda/compare",
                RemovalPolicy = RemovalPolicy.DESTROY,
                Retention = RetentionDays.ONE_WEEK
            }),
            Environment = new Dictionary<string, string>
            {
                { "INPUT_TABLE_NAME", inputTable.TableName },
                { "DATASET_TABLE_NAME", datasetTable.TableName },
                { "RESULT_TABLE_NAME", resultTable.TableName },
                { "GROUP_TABLE_NAME", groupsTable.TableName },
                { "SERVICE", "compare" }
            }
        });

        comparatorLambda.AddEventSource(new DynamoEventSource(inputTable, new DynamoEventSourceProps
        {
            StartingPosition = StartingPosition.LATEST,
            BatchSize = 10,
            Enabled = true,
            RetryAttempts = 3,
            MaxBatchingWindow = Duration.Seconds(5)
        }));

        // S3 Event Trigger
        datasetBucket.AddEventNotification(EventType.OBJECT_CREATED, new LambdaDestination(extractLambda));
        inputBucket.AddEventNotification(EventType.OBJECT_CREATED, new LambdaDestination(inputLambda));

        // Permissions
        datasetTable.GrantReadWriteData(extractLambda);
        groupsTable.GrantReadWriteData(extractLambda);
        inputTable.GrantReadWriteData(inputLambda);
        datasetBucket.GrantRead(extractLambda);
        inputBucket.GrantRead(inputLambda);
        datasetBucket.GrantDelete(extractLambda);
        inputBucket.GrantDelete(inputLambda);

        datasetTable.GrantReadWriteData(comparatorLambda);
        inputTable.GrantReadWriteData(comparatorLambda);
        inputTable.GrantStreamRead(comparatorLambda);
        resultTable.GrantReadWriteData(comparatorLambda);
        groupsTable.GrantReadWriteData(comparatorLambda);
    }
}
