using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK;
using Constructs;
using System.Collections.Generic;
using Cdk.Configuration;

namespace Cdk.Resources
{
    public static class LambdaResources
    {
        public static LambdaInfrastructure CreateLambdaInfrastructure(Construct scope, InfrastructureConfig config,
            S3Infrastructure s3Infra, DynamoDBInfrastructure dynamoInfra)
        {
            var extractorRole = CreateExtractorRole(scope, config, s3Infra, dynamoInfra);

            var extractorCode = DockerImageCode.FromImageAsset("../python-extract", new AssetImageCodeProps
            {
                File = "Dockerfile"
            });

            // Dataset extractor Lambda
            var datasetExtractor = new DockerImageFunction(scope, "DatasetExtractor", new DockerImageFunctionProps
            {
                Code = extractorCode,
                MemorySize = 2048,
                Timeout = Duration.Minutes(15),
                Role = extractorRole,
                LogGroup = new LogGroup(scope, "DatasetExtractorLogs", new LogGroupProps
                {
                    LogGroupName = "/aws/lambda/dataset-extract",
                    RemovalPolicy = RemovalPolicy.DESTROY,
                    Retention = RetentionDays.ONE_DAY
                }),
                Environment = new Dictionary<string, string>
                {
                    ["MINUTIAE_TABLE"] = dynamoInfra.DatasetMinutiaeTable.TableName,
                    ["GROUPS_TABLE"] = dynamoInfra.GroupsTable.TableName,
                    ["SERVICE_TYPE"] = "dataset",
                    ["LOG_LEVEL"] = "INFO"
                },
                FunctionName = $"{config.ProjectName}-dataset-extractor"
            });

            // Input extractor Lambda
            var inputExtractor = new DockerImageFunction(scope, "InputExtractor", new DockerImageFunctionProps
            {
                Code = extractorCode,
                MemorySize = 2048,
                Timeout = Duration.Minutes(15),
                Role = extractorRole,
                LogGroup = new LogGroup(scope, "InputExtractorLogs", new LogGroupProps
                {
                    LogGroupName = "/aws/lambda/input-extract",
                    RemovalPolicy = RemovalPolicy.DESTROY,
                    Retention = RetentionDays.TWO_WEEKS
                }),
                Environment = new Dictionary<string, string>
                {
                    ["MINUTIAE_TABLE"] = dynamoInfra.InputMinutiaeTable.TableName,
                    ["GROUPS_TABLE"] = dynamoInfra.GroupsTable.TableName,
                    ["SERVICE_TYPE"] = "input",
                    ["TTL_HOURS"] = "24",
                    ["LOG_LEVEL"] = "INFO"
                },
                FunctionName = $"{config.ProjectName}-input-extractor"
            });

            return new LambdaInfrastructure
            {
                DatasetExtractor = datasetExtractor,
                InputExtractor = inputExtractor,
                ExtractorRole = extractorRole
            };
        }

        private static Role CreateExtractorRole(Construct scope, InfrastructureConfig config,
            S3Infrastructure s3Infra, DynamoDBInfrastructure dynamoInfra)
        {
            var role = new Role(scope, "ExtractorLambdaRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
                RoleName = $"{config.ProjectName}-extractor-role",
                Description = "IAM role for fingerprint extractor Lambda functions",
                ManagedPolicies =
                [
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole")
                ]
            });

            // S3 разрешения для чтения и удаления изображений
            role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = ["s3:GetObject", "s3:DeleteObject"],
                Resources = [
                    s3Infra.DatasetBucket.BucketArn + "/*",
                    s3Infra.InputBucket.BucketArn + "/*"
                ]
            }));

            // DynamoDB разрешения для записи минуций и групп
            role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = ["dynamodb:PutItem", "dynamodb:UpdateItem"],
                Resources = [
                    dynamoInfra.DatasetMinutiaeTable.TableArn,
                    dynamoInfra.InputMinutiaeTable.TableArn,
                    dynamoInfra.GroupsTable.TableArn
                ]
            }));

            return role;
        }
    }

    public class LambdaInfrastructure
    {
        public DockerImageFunction DatasetExtractor { get; set; } = null!;
        public DockerImageFunction InputExtractor { get; set; } = null!;
        public Role ExtractorRole { get; set; } = null!;
    }
}
