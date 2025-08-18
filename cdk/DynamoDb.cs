using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Constructs;
using Attribute = Amazon.CDK.AWS.DynamoDB.Attribute;

namespace Cdk;

class DynamoDb
{
    internal static Table CreateDatasetMinutiaeTable(Construct scope, Props props)
    {
        var table = new Table(scope, props.DatasetMinutiaeDynamoTableName, new TableProps
        {
            PartitionKey = new Attribute { Name = "ImageId", Type = AttributeType.STRING },
            RemovalPolicy = RemovalPolicy.DESTROY,
            TableName = props.DatasetMinutiaeDynamoTableName,
            BillingMode = BillingMode.PAY_PER_REQUEST
        });

        table.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
        {
            IndexName = "GroupId-GSI",
            PartitionKey = new Attribute
            {
                Name = "GroupId",
                Type = AttributeType.STRING
            },
            ProjectionType = ProjectionType.ALL
        });

        return table;
    }

    internal static Table CreateInputMinutiaeTable(Construct scope, Props props)
    {
        return new Table(scope, props.InputMinutiaeDynamoTableName, new TableProps
        {
            PartitionKey = new Attribute { Name = "ImageId", Type = AttributeType.STRING },
            RemovalPolicy = RemovalPolicy.DESTROY,
            TableName = props.InputMinutiaeDynamoTableName,
            BillingMode = BillingMode.PAY_PER_REQUEST
        });
    }

    internal static Table CreateDatasetGroupsTable(Construct scope, Props props)
    {
        return new Table(scope, props.DatasetGroupsTableName, new TableProps
        {
            PartitionKey = new Attribute { Name = "GroupId", Type = AttributeType.STRING },
            RemovalPolicy = RemovalPolicy.DESTROY,
            TableName = props.DatasetGroupsTableName,
            BillingMode = BillingMode.PAY_PER_REQUEST
        });
    }
}
