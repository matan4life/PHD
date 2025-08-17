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

        // DynamoDB Table for Dataset Minutiae
        var datasetTable = DynamoDb.CreateDatasetMinutiaeTable(this, props);

        // Extract Lambda
        var extractLambda = new DockerImageFunction(this, "DatasetExtract", new DockerImageFunctionProps
        {
            Code = DockerImageCode.FromImageAsset("../python-dataset-extract", new AssetImageCodeProps { File = "Dockerfile" }),
            MemorySize = 1024,
            Timeout = Duration.Minutes(5),
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

        // S3 Event Trigger
        datasetBucket.AddEventNotification(EventType.OBJECT_CREATED, new LambdaDestination(extractLambda));

        // Permissions
        datasetTable.GrantReadWriteData(extractLambda);
        datasetBucket.GrantRead(extractLambda);
    }
}
