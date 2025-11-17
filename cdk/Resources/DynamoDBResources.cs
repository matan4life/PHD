using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK;
using Constructs;
using Cdk.Configuration;
using Attribute = Amazon.CDK.AWS.DynamoDB.Attribute;

namespace Cdk.Resources
{
    public static class DynamoDBResources
    {
        public static DynamoDBInfrastructure CreateDynamoDBInfrastructure(Construct scope, InfrastructureConfig config)
        {
            var datasetMinutiaeTable = new Table(scope, "DatasetMinutiaeTable", new TableProps
            {
                TableName = $"{config.ProjectName}-dataset-minutiae",
                PartitionKey = new Attribute { Name = "ImageId", Type = AttributeType.STRING },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                RemovalPolicy = RemovalPolicy.DESTROY,
                DeletionProtection = false
            });

            var inputMinutiaeTable = new Table(scope, "InputMinutiaeTable", new TableProps
            {
                TableName = $"{config.ProjectName}-input-minutiae",
                PartitionKey = new Attribute { Name = "ImageId", Type = AttributeType.STRING },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                RemovalPolicy = RemovalPolicy.DESTROY,
                Stream = StreamViewType.NEW_AND_OLD_IMAGES,
                DeletionProtection = false
            });

            var groupsTable = new Table(scope, "GroupsTable", new TableProps
            {
                TableName = $"{config.ProjectName}-groups",
                PartitionKey = new Attribute { Name = "GroupId", Type = AttributeType.STRING },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                RemovalPolicy = RemovalPolicy.DESTROY,
                DeletionProtection = false
            });

            var resultsTable = new Table(scope, "ResultsTable", new TableProps
            {
                TableName = $"{config.ProjectName}-results",
                PartitionKey = new Attribute { Name = "ProbeImageId", Type = AttributeType.STRING },
                SortKey = new Attribute { Name = "GroupId", Type = AttributeType.STRING },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                RemovalPolicy = RemovalPolicy.DESTROY,
                DeletionProtection = false
            });

            return new DynamoDBInfrastructure
            {
                DatasetMinutiaeTable = datasetMinutiaeTable,
                InputMinutiaeTable = inputMinutiaeTable,
                GroupsTable = groupsTable,
                ResultsTable = resultsTable
            };
        }
    }

    public class DynamoDBInfrastructure
    {
        public Table DatasetMinutiaeTable { get; set; } = null!;
        public Table InputMinutiaeTable { get; set; } = null!;
        public Table GroupsTable { get; set; } = null!;
        public Table ResultsTable { get; set; } = null!;
    }
}
