using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Constructs;
using Attribute = Amazon.CDK.AWS.DynamoDB.Attribute;

namespace Cdk;

class DynamoDb
{
    internal static Table CreateDatasetMinutiaeTable(Construct scope, Props props)
    {
        return new Table(scope, props.DatasetMinutiaeDynamoTableName, new TableProps
        {
            PartitionKey = new Attribute { Name = "ImageId", Type = AttributeType.STRING },
            RemovalPolicy = RemovalPolicy.DESTROY,
            TableName = props.DatasetMinutiaeDynamoTableName,
            BillingMode = BillingMode.PAY_PER_REQUEST
        });
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
}
