using Amazon.CDK.AWS.EC2;
using Cdk.Configuration;
using Constructs;

namespace Cdk.Resources
{
    public static class VpcResources
    {
        public static VpcInfrastructure CreateVpcInfrastructure(Construct scope, InfrastructureConfig config)
        {
            var vpc = new Vpc(scope, "SharedVpc", new VpcProps
            {
                MaxAzs = 2,
                NatGateways = 0,
                SubnetConfiguration =
                [
                    new SubnetConfiguration
                    {
                        Name = "Private",
                        SubnetType = SubnetType.PRIVATE_ISOLATED,
                        CidrMask = 24
                    }
                ],
                VpcName = $"{config.ProjectName}-vpc",
                EnableDnsHostnames = true,
                EnableDnsSupport = true
            });

            var cacheSecurityGroup = new SecurityGroup(scope, "CacheSecurityGroup", new SecurityGroupProps
            {
                Vpc = vpc,
                Description = "Security group for Redis metrics cache",
                SecurityGroupName = $"{config.ProjectName}-cache-sg",
                AllowAllOutbound = false
            });

            cacheSecurityGroup.AddIngressRule(
                Peer.Ipv4(vpc.VpcCidrBlock),
                Port.Tcp(6379),
                "Redis access from Lambda comparator"
            );

            return new VpcInfrastructure
            {
                Vpc = vpc,
                CacheSecurityGroup = cacheSecurityGroup,
                PrivateSubnets = vpc.IsolatedSubnets
            };
        }
    }

    public class VpcInfrastructure
    {
        public IVpc Vpc { get; set; } = null!;
        public SecurityGroup CacheSecurityGroup { get; set; } = null!;
        public ISubnet[] PrivateSubnets { get; set; } = null!;
    }
}
