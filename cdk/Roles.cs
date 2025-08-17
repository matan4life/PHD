using Amazon.CDK.AWS.IAM;
using Constructs;

namespace Cdk;

class Roles
{
    public static Role? ExtractRole { get; private set; }

    internal static Role CreateRole(Construct scope, Props props)
    {
        ExtractRole = new Role(scope, props.ExtractLambdaRoleName, new RoleProps
        {
            AssumedBy = new ServicePrincipal("lambda.amazonaws.com")
        });
        ExtractRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole"));

        // Custom policy for S3 read, DynamoDB write
        ExtractRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["s3:GetObject", "dynamodb:PutItem", "dynamodb:GetItem"],
            Resources = ["*"]  // TODO: Replace with specific ARNs for production
        }));

        return ExtractRole;
    }
}
