using Amazon.CDK;
using Cdk.Configuration;
using Cdk.Resources;
using Constructs;

namespace Cdk;

class ApplicationStack : Stack
{
    public ApplicationStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
    {
        var config = new InfrastructureConfig();

        var vpcInfra = VpcResources.CreateVpcInfrastructure(this, config);
        var s3Infra = S3Resources.CreateS3Infrastructure(this, config);
        var dynamoInfra = DynamoDBResources.CreateDynamoDBInfrastructure(this, config);
        var cacheInfra = CacheResources.CreateCacheInfrastructure(this, config, vpcInfra);
        var lambdaInfra = LambdaResources.CreateLambdaInfrastructure(this, config, s3Infra, dynamoInfra);
        S3Resources.SetupS3EventTriggers(s3Infra, lambdaInfra);
        CreateStackOutputs(config, s3Infra, dynamoInfra, vpcInfra, cacheInfra, lambdaInfra);
    }

    private void CreateStackOutputs(InfrastructureConfig config, S3Infrastructure s3Infra,
        DynamoDBInfrastructure dynamoInfra, VpcInfrastructure vpcInfra, CacheInfrastructure cacheInfra,
        LambdaInfrastructure lambdaInfra)
    {
        new CfnOutput(this, "DatasetBucketName", new CfnOutputProps
        {
            Value = s3Infra.DatasetBucket.BucketName,
            Description = "S3 bucket for dataset fingerprint images"
        });

        new CfnOutput(this, "InputBucketName", new CfnOutputProps
        {
            Value = s3Infra.InputBucket.BucketName,
            Description = "S3 bucket for input fingerprint images"
        });

        new CfnOutput(this, "SharedVpcId", new CfnOutputProps
        {
            Value = vpcInfra.Vpc.VpcId,
            Description = "VPC ID for comparator Lambda (future stage)"
        });

        new CfnOutput(this, "VpcCidr", new CfnOutputProps
        {
            Value = vpcInfra.Vpc.VpcCidrBlock,
            Description = "VPC CIDR block"
        });

        new CfnOutput(this, "PrivateSubnetIds", new CfnOutputProps
        {
            Value = string.Join(",", vpcInfra.PrivateSubnets.Select(s => s.SubnetId)),
            Description = "Private subnet IDs for comparator Lambda"
        });

        new CfnOutput(this, "CacheSecurityGroupId", new CfnOutputProps
        {
            Value = vpcInfra.CacheSecurityGroup.SecurityGroupId,
            Description = "Security Group ID for Redis access"
        });

        new CfnOutput(this, "RedisEndpoint", new CfnOutputProps
        {
            Value = cacheInfra.RedisCluster.AttrRedisEndpointAddress,
            Description = "Redis cache endpoint for metrics caching in comparator"
        });

        new CfnOutput(this, "RedisPort", new CfnOutputProps
        {
            Value = "6379",
            Description = "Redis port"
        });

        new CfnOutput(this, "DatasetTableName", new CfnOutputProps
        {
            Value = dynamoInfra.DatasetMinutiaeTable.TableName,
            Description = "DynamoDB table for dataset minutiae"
        });

        new CfnOutput(this, "InputTableName", new CfnOutputProps
        {
            Value = dynamoInfra.InputMinutiaeTable.TableName,
            Description = "DynamoDB table for input minutiae (with stream for comparator)"
        });

        new CfnOutput(this, "InputTableStreamArn", new CfnOutputProps
        {
            Value = dynamoInfra.InputMinutiaeTable.TableStreamArn ?? "No stream configured",
            Description = "DynamoDB stream ARN for comparator trigger"
        });

        new CfnOutput(this, "ResultsTableName", new CfnOutputProps
        {
            Value = dynamoInfra.ResultsTable.TableName,
            Description = "DynamoDB table for comparison results"
        });

        new CfnOutput(this, "DatasetExtractorName", new CfnOutputProps
        {
            Value = lambdaInfra.DatasetExtractor.FunctionName,
            Description = "Dataset extractor Lambda function name"
        });

        new CfnOutput(this, "InputExtractorName", new CfnOutputProps
        {
            Value = lambdaInfra.InputExtractor.FunctionName,
            Description = "Input extractor Lambda function name"
        });

        new CfnOutput(this, "CloudWatchLogsDatasetExtract", new CfnOutputProps
        {
            Value = $"https://console.aws.amazon.com/cloudwatch/home?region={this.Region}#logsV2:log-groups/log-group/%2Faws%2Flambda%2Fdataset-extract",
            Description = "CloudWatch Logs for Dataset Extractor"
        });

        new CfnOutput(this, "CloudWatchLogsInputExtract", new CfnOutputProps
        {
            Value = $"https://console.aws.amazon.com/cloudwatch/home?region={this.Region}#logsV2:log-groups/log-group/%2Faws%2Flambda%2Finput-extract",
            Description = "CloudWatch Logs for Input Extractor"
        });

        new CfnOutput(this, "ReadyForComparator", new CfnOutputProps
        {
            Value = "VPC, ElastiCache, and DynamoDB stream ready for comparator Lambda deployment",
            Description = "Infrastructure ready for stage 2 (comparator)"
        });
    }
}
