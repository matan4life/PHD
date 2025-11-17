using Amazon.CDK.AWS.ElastiCache;
using Cdk.Configuration;
using Constructs;

namespace Cdk.Resources
{
    public static class CacheResources
    {
        public static CacheInfrastructure CreateCacheInfrastructure(Construct scope, InfrastructureConfig config, VpcInfrastructure vpcInfra)
        {
            var subnetGroup = new CfnSubnetGroup(scope, "CacheSubnetGroup", new CfnSubnetGroupProps
            {
                Description = "Subnet group for fingerprint metrics cache",
                SubnetIds = vpcInfra.PrivateSubnets.Select(s => s.SubnetId).ToArray(),
                CacheSubnetGroupName = $"{config.ProjectName}-cache-subnets"
            });

            var redisCluster = new CfnCacheCluster(scope, "RedisCluster", new CfnCacheClusterProps
            {
                CacheNodeType = "cache.t3.micro",
                Engine = "redis",
                EngineVersion = "7.0",
                NumCacheNodes = 1,
                CacheSubnetGroupName = subnetGroup.CacheSubnetGroupName,
                VpcSecurityGroupIds = [vpcInfra.CacheSecurityGroup.SecurityGroupId],
                ClusterName = $"{config.ProjectName}-metrics-cache",
                Port = 6379,
                PreferredMaintenanceWindow = "sun:05:00-sun:06:00",
                SnapshotRetentionLimit = 1,
                SnapshotWindow = "04:00-05:00"
            });

            redisCluster.AddDependency(subnetGroup);

            return new CacheInfrastructure
            {
                RedisCluster = redisCluster,
                SubnetGroup = subnetGroup,
                SecurityGroup = vpcInfra.CacheSecurityGroup
            };
        }
    }

    public class CacheInfrastructure
    {
        public CfnCacheCluster RedisCluster { get; set; } = null!;
        public CfnSubnetGroup SubnetGroup { get; set; } = null!;
        public Amazon.CDK.AWS.EC2.SecurityGroup SecurityGroup { get; set; } = null!;
    }
}
