using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
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

        // Extract Lambda
        var extractLambda = new DockerImageFunction(this, "DatasetExtract", new DockerImageFunctionProps
        {
            Code = DockerImageCode.FromImageAsset("../python-extract", new AssetImageCodeProps { File = "Dockerfile" }),
            MemorySize = 1024,
            Timeout = Duration.Minutes(15),
            Role = Roles.CreateRole(this, props),
            LogGroup = new LogGroup(this, "DatasetExtractLogGroup", new LogGroupProps
            {
                LogGroupName = "/aws/lambda/dataset-extract",
                RemovalPolicy = RemovalPolicy.DESTROY,
                Retention = RetentionDays.ONE_WEEK
            }),
            Environment = new Dictionary<string, string>
            {
                { "TABLE_NAME", datasetTable.TableName }
            }
        });
        var inputLambda = new DockerImageFunction(this, "InputExtract", new DockerImageFunctionProps
        {
            Code = DockerImageCode.FromImageAsset("../python-extract", new AssetImageCodeProps { File = "Dockerfile" }),
            MemorySize = 1024,
            Timeout = Duration.Minutes(15),
            Role = Roles.CreateRole(this, props),
            LogGroup = new LogGroup(this, "InputExtractLogGroup", new LogGroupProps
            {
                LogGroupName = "/aws/lambda/input-extract",
                RemovalPolicy = RemovalPolicy.DESTROY,
                Retention = RetentionDays.ONE_WEEK
            }),
            Environment = new Dictionary<string, string>
            {
                { "TABLE_NAME", inputTable.TableName }
            }
        });

        // S3 Event Trigger
        datasetBucket.AddEventNotification(EventType.OBJECT_CREATED, new LambdaDestination(extractLambda));
        inputBucket.AddEventNotification(EventType.OBJECT_CREATED, new LambdaDestination(inputLambda));

        // Permissions
        datasetTable.GrantReadWriteData(extractLambda);
        inputTable.GrantReadWriteData(inputLambda);
        datasetBucket.GrantRead(extractLambda);
        inputBucket.GrantRead(inputLambda);
    }
}
