using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Constructs;

namespace Cdk;

class S3
{
    internal static Bucket CreateDatasetBucket(Construct scope, Props props)
    {
        return new Bucket(scope, props.DatasetBucketName, new BucketProps
        {
            RemovalPolicy = RemovalPolicy.DESTROY,
            BucketName = props.DatasetBucketName
        });
    }
}
