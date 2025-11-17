using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Notifications;
using Cdk.Configuration;
using Constructs;

namespace Cdk.Resources
{
    public static class S3Resources
    {
        public static S3Infrastructure CreateS3Infrastructure(Construct scope, InfrastructureConfig config)
        {
            // S3 bucket для dataset изображений
            var datasetBucket = new Bucket(scope, "DatasetBucket", new BucketProps
            {
                BucketName = $"{config.ProjectName}-dataset-{config.Environment}",
                RemovalPolicy = RemovalPolicy.DESTROY,
                AutoDeleteObjects = true,
            });

            // S3 bucket для input изображений
            var inputBucket = new Bucket(scope, "InputBucket", new BucketProps
            {
                BucketName = $"{config.ProjectName}-input-{config.Environment}",
                RemovalPolicy = RemovalPolicy.DESTROY,
                AutoDeleteObjects = true,
            });

            return new S3Infrastructure
            {
                DatasetBucket = datasetBucket,
                InputBucket = inputBucket
            };
        }

        public static void SetupS3EventTriggers(S3Infrastructure s3, LambdaInfrastructure lambdas)
        {
            s3.DatasetBucket.AddEventNotification(
                EventType.OBJECT_CREATED,
                new LambdaDestination(lambdas.DatasetExtractor),
                new NotificationKeyFilter { Suffix = ".tif" }
            );

            s3.InputBucket.AddEventNotification(
                EventType.OBJECT_CREATED,
                new LambdaDestination(lambdas.InputExtractor),
                new NotificationKeyFilter { Suffix = ".tif" }
            );
        }
    }

    public class S3Infrastructure
    {
        public Bucket DatasetBucket { get; set; } = null!;
        public Bucket InputBucket { get; set; } = null!;
    }
}
